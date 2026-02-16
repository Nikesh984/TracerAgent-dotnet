namespace TracerAgent.Core.Models;

/// <summary>
/// Dep 2 output — from AppCatalog/CMDB (NOT from IGA).
/// Answers: What app? Still active or decommissioned? Who owns the app?
/// </summary>
public sealed record AppContext
{
    public required string ApplicationId { get; init; }
    public required string ApplicationName { get; init; }
    public required string Platform { get; init; }

    public required AppStatus Status { get; init; }
    public string? DecommissionDate { get; init; }

    public string? AppOwnerId { get; init; }
    public string? AppOwnerName { get; init; }
    public string? AppOwnerEmail { get; init; }
    public string? TeamName { get; init; }
    public string? TeamDistributionList { get; init; }

    public string? Notes { get; init; }
}

public enum AppStatus
{
    Active,
    Decommissioned,
    Deprecated,
    Unknown
}