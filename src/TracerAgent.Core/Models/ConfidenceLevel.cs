namespace TracerAgent.Core.Models

/// <summary>
/// Data quality signal — NOT a gate.
/// All accounts continue the full pipeline regardless of confidence.
/// Tells the IAM engineer how much to trust the activity verification data.
/// </summary>


public enum ConfidenceLevel
{
    /// <summary> SIEM (Splunk) corroborated. Strongest verification. </summary>
    High,

    /// <summary> OpenLDAP verified ( SIEM had no data). Secondary source of information </summary>
    Medium,

    /// <summary> No verification possible from any source. Flag for IAM team. </summary>
    Low
}


