namespace SmartX.Domain.Enums;

/// <summary>
/// Describes the validation and health state of a telemetry reading.
/// </summary>
public enum TelemetryHealthState
{
    /// <summary>
    /// The reading is present and within its expected operating range.
    /// </summary>
    Normal = 1,

    /// <summary>
    /// The reading exhibits unusual behaviour such as a sudden spike.
    /// </summary>
    Anomaly = 2,

    /// <summary>
    /// The reading falls outside the configured safe range.
    /// </summary>
    OutOfRange = 3,

    /// <summary>
    /// An expected telemetry reading was not received.
    /// </summary>
    Missing = 4,

    /// <summary>
    /// The reading could not be parsed or safely validated.
    /// </summary>
    Invalid = 5
}