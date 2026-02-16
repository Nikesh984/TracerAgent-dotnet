namespace TracerAgent.Core.Models;

/// <summary>
/// Full case file for a single account.
/// Sent to Agent B (risk) + Agent C (outreach) for ALL confirmed Stale/Orphaned.
/// Confidence is a label, not a filter.
/// </summary>
public sealed record InvestigationResult
{
    public required string AccountId { get; init; }
    public required string RequestId { get; init; }
    public required NhiAccount AccountData { get; init; }

    // ── Verification outcome ────────────────────────────────────
    // Agent A does NOT classify. It verifies the upstream classification.
    // Only override: reclassify → Active if recent activity found (IGA data gap).
    public required Classification FinalClassification { get; init; }
    public required bool WasReclassified { get; init; }
    public string? ReclassificationReason { get; init; }

    // ── Dep 1: Activity Verification ────────────────────────────
    public required ActivityVerificationResult ActivityVerification { get; init; }

    // ── Dep 2: Context Resolution (from AppCatalog) ─────────────
    public required AppContext ApplicationContext { get; init; }

    // ── Routing ─────────────────────────────────────────────────
    public required DownstreamRouting Routing { get; init; }

    public required DateTime InvestigatedAt { get; init; }
}

/// <summary>
/// ALL Stale/Orphaned → BOTH Agent B AND Agent C.
/// Outreach goal differs by classification.
/// </summary>
public sealed record DownstreamRouting
{
    public required bool SendToAgentB { get; init; }
    public required bool SendToAgentC { get; init; }
    public required string OutreachGoal { get; init; }
}