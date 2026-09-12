namespace SmartX.Domain.Enums;

/// <summary>
/// Describes the current connectivity state of a Smart-X sensor.
/// </summary>
public enum SensorConnectionStatus
{
    /// <summary>
    /// The sensor is registered but has not submitted telemetry.
    /// </summary>
    Pending = 1,

    /// <summary>
    /// The sensor is connected and submitting telemetry normally.
    /// </summary>
    Online = 2,

    /// <summary>
    /// The sensor has missed its expected publishing interval.
    /// </summary>
    Stale = 3,

    /// <summary>
    /// The sensor has missed multiple publishing intervals.
    /// </summary>
    Disconnected = 4,

    /// <summary>
    /// The sensor has intentionally been placed into maintenance.
    /// </summary>
    Maintenance = 5
}