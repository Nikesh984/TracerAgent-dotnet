namespace TracerAgent.Core.Models;

public sealed record ActivityRecord
{
    public required string AccountId { get; init; }

    public required string Source { get; init; }  // Splunk or OpenLDAP

    public required DateTime TimeStamp { get; init; }

    public required string EventType { get; init; } // "login", "APICall", "LDAPBind"

    public string? TargetResource { get; init; }

    public string? SourceIp { get; init; }


    public Dictionary<string, string> MetaData { get; init; } = new();
}