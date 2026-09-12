namespace SmartX.Domain.Enums;

/// <summary>
/// Defines the strongly typed telemetry formats supported by Smart-X.
/// </summary>
public enum TelemetryDataType
{
    /// <summary>
    /// A decimal reading such as temperature, humidity or soil moisture.
    /// </summary>
    FloatingPoint = 1,

    /// <summary>
    /// A whole-number reading such as wattage or an event count.
    /// </summary>
    Integer = 2,

    /// <summary>
    /// A logical reading such as a valve, relay or switch state.
    /// </summary>
    Boolean = 3
}