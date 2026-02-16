namespace TracerAgent.Core.Models;

/// <summary>
/// Pre-classified account from IGA, selected by IAM engineer for investigation.
/// Already classified as Stale or Orphaned by the upstream Classification Engine.
/// Agent A does NOT classify — it verifies and enriches.
/// </summary>
/// 

public sealed record NHIAccount
{
    public required string AccountId { get; init; }
    public required string AccountName { get; init; }

    public required string ApplicationId { get; init; }

    public required string Platform { get; init; } // Endpoint, Application

    public required Classification Classification { get; init; } //Pre-set : Stale or Orphaned

    public string? OwnerId { get; init; }                        // null for Orphaned Accounts

    public string? OwnerDisplayName { get; init; }

    public string? OwnerEmail { get; init; }

    public DateTime? LastKnownActivityPerIga { get; init; }     // What IGA has (maybe wrong)

    public int InActivityThresholdDays { get; init; }           // App-specific, set by IAM config

    public DateTime CreatedDate { get; init; }

    public Dictionary<string, string> Attributes { get; init; } = new();
}