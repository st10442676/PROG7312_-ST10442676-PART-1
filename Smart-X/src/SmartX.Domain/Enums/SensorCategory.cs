namespace SmartX.Domain.Enums;

/// <summary>
/// Identifies the operational purpose of a registered Smart-X sensor.
/// </summary>
public enum SensorCategory
{
    /// <summary>
    /// Measures environmental conditions such as temperature,
    /// humidity, soil moisture or air quality.
    /// </summary>
    Environmental = 1,

    /// <summary>
    /// Measures electrical usage, voltage, current or wattage.
    /// </summary>
    PowerConsumption = 2,

    /// <summary>
    /// Represents a controllable device such as a valve,
    /// relay, pump or smart switch.
    /// </summary>
    Actuator = 3
}