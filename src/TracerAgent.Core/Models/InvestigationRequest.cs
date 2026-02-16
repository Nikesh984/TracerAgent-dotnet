namespace TracerAgent.Core.Models;

/// <summary>
/// IAM engineer selects Stale/Orphaned accounts from the dashboard
/// and triggers Agent A investigation. Accounts arrive pre-classified
/// with IGA baseline data.
/// </summary>
public sealed record InvestigationRequest
{
    public string RequestId { get; init; } = Guid.NewGuid().ToString();
    public required List<NhiAccount> Accounts { get; init; }
    public string? RequestedBy { get; init; }
    public DateTime RequestedAt { get; init; } = DateTime.UtcNow;
}