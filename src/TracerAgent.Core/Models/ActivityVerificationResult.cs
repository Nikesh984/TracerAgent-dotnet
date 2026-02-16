namespace TracerAgent.Core.Models;

/// <summary>
/// Dep 1 output — result of the SIEM → LDAP fallback chain.
/// Confidence level is a data quality signal attached to the case file.
/// </summary>
/// 

public sealed record ActivityVerificationResult
{
    public required string AccountId { get; init; }

    public required ConfidenceLevel Confidence { get; init; }

    public required bool ActivityFound { get; init; }

    public DateTime? LastConfirmedActivity { get; init; }

    public string? VerifiedBy { get; init; }  // "Splunk", "OpenLDAP", or null

    public IReadOnlyList<ActivityRecord> Evidence { get; init; } = [];

    public required string Summary { get; init; }
}