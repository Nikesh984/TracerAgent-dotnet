namespace TracerAgent.Core.Services;

using Microsoft.Extensions.Logging;
using TracerAgent.Core.Interfaces;
using TracerAgent.Core.Models;

/// <summary>
/// Full Agent A pipeline:
///
///   1. Dep 1 — Activity Verification (SIEM → LDAP fallback → confidence)
///   2. Reclassification check:
///      - Recent activity within threshold? → Reclassify Active, flag IGA gap, DROP
///      - Otherwise → confirmed Stale/Orphaned, CONTINUE
///   3. ALL confirmed Stale/Orphaned continue — confidence is NOT a gate
///   4. Dep 2 — Context Resolution (AppCatalog: app status, owner, team)
///   5. Build case file
///   6. Route to BOTH Agent B (risk) AND Agent C (outreach)
///
/// Key principle: A low-confidence case file with Agent C's owner response
/// saying "yes, we still use this daily" is more valuable than a high-confidence
/// SIEM-corroborated one — because it uncovered a visibility gap.
/// </summary>
public sealed class EnrichmentOrchestrator : IEnrichmentOrchestrator
{
    private readonly IActivityVerifier _activityVerifier;
    private readonly IContextResolver _contextResolver;
    private readonly ILogger<EnrichmentOrchestrator> _log;

    public EnrichmentOrchestrator(
        IActivityVerifier activityVerifier,
        IContextResolver contextResolver,
        ILogger<EnrichmentOrchestrator> log)
    {
        _activityVerifier = activityVerifier;
        _contextResolver = contextResolver;
        _log = log;
    }

    public async Task<InvestigationResult> InvestigateAccountAsync(
        NhiAccount account, string requestId, CancellationToken ct = default)
    {
        _log.LogInformation(
            "═══ Investigation start: {Id} (upstream classification: {Class}) ═══",
            account.AccountId, account.Classification);

        // ── Dep 1: Activity Verification ─────────────────────────
        var verification = await _activityVerifier.VerifyAsync(account, ct);

        // ── Reclassification check ───────────────────────────────
        // Only override: if SIEM/LDAP found recent activity within the
        // app-specific threshold → IGA was wrong → reclassify Active + drop
        var (reclassified, reclassReason) = CheckReclassification(account, verification);

        if (reclassified)
        {
            _log.LogWarning(
                "⚡ {Id}: RECLASSIFIED → Active. IGA data gap. Dropping from pipeline.",
                account.AccountId);

            return new InvestigationResult
            {
                AccountId = account.AccountId,
                RequestId = requestId,
                AccountData = account,
                FinalClassification = Classification.Active,
                WasReclassified = true,
                ReclassificationReason = reclassReason,
                ActivityVerification = verification,
                ApplicationContext = new AppContext
                {
                    ApplicationId = account.ApplicationId,
                    ApplicationName = account.ApplicationName,
                    Platform = account.Platform,
                    Status = AppStatus.Active
                },
                Routing = new DownstreamRouting
                {
                    SendToAgentB = false,
                    SendToAgentC = false,
                    OutreachGoal = "N/A — reclassified Active. IGA data gap flagged for reconciliation."
                },
                InvestigatedAt = DateTime.UtcNow
            };
        }

        // ── Confirmed Stale/Orphaned — full pipeline continues ───
        _log.LogInformation(
            "{Id}: Confirmed {Class} | {Confidence} confidence | continuing pipeline",
            account.AccountId, account.Classification, verification.Confidence);

        // ── Dep 2: Context Resolution (AppCatalog) ───────────────
        var appContext = await _contextResolver.ResolveAsync(account, ct);

        // ── Build case file + routing ────────────────────────────
        var routing = BuildRouting(account.Classification);

        var result = new InvestigationResult
        {
            AccountId = account.AccountId,
            RequestId = requestId,
            AccountData = account,
            FinalClassification = account.Classification,
            WasReclassified = false,
            ActivityVerification = verification,
            ApplicationContext = appContext,
            Routing = routing,
            InvestigatedAt = DateTime.UtcNow
        };

        _log.LogInformation(
            "═══ Investigation complete: {Id} → {Class} | {Confidence} | → B:{B} C:{C} | Goal: {Goal} ═══",
            result.AccountId, result.FinalClassification, verification.Confidence,
            routing.SendToAgentB, routing.SendToAgentC, routing.OutreachGoal);

        return result;
    }

    public async Task<IReadOnlyList<InvestigationResult>> InvestigateBatchAsync(
        InvestigationRequest request, CancellationToken ct = default)
    {
        _log.LogInformation(
            "Batch: {Count} accounts (request {Req})",
            request.Accounts.Count, request.RequestId);

        var semaphore = new SemaphoreSlim(5);
        var tasks = request.Accounts.Select(async account =>
        {
            await semaphore.WaitAsync(ct);
            try { return await InvestigateAccountAsync(account, request.RequestId, ct); }
            finally { semaphore.Release(); }
        });

        var results = await Task.WhenAll(tasks);

        var reclass = results.Count(r => r.WasReclassified);
        var stale = results.Count(r => r.FinalClassification == Classification.Stale);
        var orphaned = results.Count(r => r.FinalClassification == Classification.Orphaned);

        _log.LogInformation(
            "Batch complete: {Reclass} reclassified→Active, {Stale} stale, {Orphaned} orphaned → Agent B+C",
            reclass, stale, orphaned);

        return results;
    }

    // ─────────────────────────────────────────────────────────────
    // Reclassification: only if recent activity found within threshold
    // ─────────────────────────────────────────────────────────────
    private static (bool Reclassified, string? Reason) CheckReclassification(
        NhiAccount account, ActivityVerificationResult verification)
    {
        if (!verification.ActivityFound || !verification.LastConfirmedActivity.HasValue)
            return (false, null);

        var daysSince = (DateTime.UtcNow - verification.LastConfirmedActivity.Value).TotalDays;

        if (daysSince <= account.InactivityThresholdDays)
        {
            return (true,
                $"Reclassified → Active. {verification.VerifiedBy} shows activity " +
                $"{daysSince:F0}d ago (threshold: {account.InactivityThresholdDays}d). " +
                $"IGA incorrectly classified as {account.Classification} — data gap flagged.");
        }

        return (false, null);
    }

    // ─────────────────────────────────────────────────────────────
    // Both Stale AND Orphaned → BOTH Agent B + Agent C
    // ─────────────────────────────────────────────────────────────
    private static DownstreamRouting BuildRouting(Classification classification) => classification switch
    {
        Classification.Stale => new DownstreamRouting
        {
            SendToAgentB = true,
            SendToAgentC = true,
            OutreachGoal = "Get approval to disable account. Confirm usage details with owner."
        },
        Classification.Orphaned => new DownstreamRouting
        {
            SendToAgentB = true,
            SendToAgentC = true,
            OutreachGoal = "Transfer ownership. Confirm application status and usage with app team."
        },
        _ => new DownstreamRouting
        {
            SendToAgentB = false,
            SendToAgentC = false,
            OutreachGoal = "N/A"
        }
    };
}