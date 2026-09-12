namespace SmartX.Domain.Enums;

/// <summary>
/// Describes how the Smart-X gateway handled a telemetry item.
/// </summary>
public enum TelemetryIngestionState
{
    /// <summary>
    /// The item passed validation and entered the ingestion pipeline.
    /// </summary>
    Accepted = 1,

    /// <summary>
    /// The item was stored but requires investigation.
    /// </summary>
    Flagged = 2,

    /// <summary>
    /// The item failed validation and was not accepted for processing.
    /// </summary>
    Rejected = 3
}