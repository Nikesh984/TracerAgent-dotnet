namespace TracerAgent.Core.Models;

/// <summary>
/// Set UPSTREAM by the Classification Engine (IGA layer).
/// Agent A receives Stale/Orphaned only.
/// Agent A can only change this to Active (reclassification).
/// </summary>

public enum Classification
{
    Active,
    Stale,
    Orphaned
}