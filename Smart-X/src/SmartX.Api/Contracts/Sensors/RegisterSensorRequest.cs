using System.ComponentModel.DataAnnotations;
using SmartX.Domain.Enums;

namespace SmartX.Api.Contracts.Sensors;

/// <summary>
/// Contains the client-supplied values required to register a sensor.
/// </summary>
public sealed class RegisterSensorRequest
{
    [Required]
    [StringLength(64, MinimumLength = 3)]
    public string DeviceIdentifier { get; init; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 3)]
    public string DisplayName { get; init; } = string.Empty;

    [Required]
    [StringLength(80)]
    public string Facility { get; init; } = string.Empty;

    [Required]
    [StringLength(80)]
    public string Zone { get; init; } = string.Empty;

    [StringLength(80)]
    public string? SubZone { get; init; }

    [Required]
    [StringLength(80)]
    public string NodeId { get; init; } = string.Empty;

    [EnumDataType(typeof(SensorCategory))]
    public SensorCategory Category { get; init; }

    [EnumDataType(typeof(TelemetryDataType))]
    public TelemetryDataType DataType { get; init; }

    [Required]
    [StringLength(20)]
    public string Unit { get; init; } = string.Empty;

    public double? ExpectedMinimum { get; init; }

    public double? ExpectedMaximum { get; init; }

    [Range(
        1,
        86_400,
        ErrorMessage =
            "Publishing interval must be between 1 and 86400 seconds.")]
    public int PublishingIntervalSeconds { get; init; } = 30;
}