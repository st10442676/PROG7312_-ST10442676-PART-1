using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SmartX.Client.Models;

/// <summary>
/// Sensor category values matching the Smart-X API.
/// </summary>
public enum SensorCategoryOption
{
    Environmental = 1,
    PowerConsumption = 2,
    Actuator = 3
}

/// <summary>
/// Supported telemetry value types.
/// </summary>
public enum TelemetryDataTypeOption
{
    FloatingPoint = 1,
    Integer = 2,
    Boolean = 3
}

/// <summary>
/// Contains and validates values entered into the
/// sensor-registration form.
/// </summary>
public sealed class RegisterSensorFormModel : IValidatableObject
{
    [Required(ErrorMessage = "A device identifier is required.")]
    [StringLength(
        64,
        MinimumLength = 6,
        ErrorMessage =
            "The device identifier must contain between 6 and 64 characters.")]
    [RegularExpression(
        @"^[A-Za-z0-9][A-Za-z0-9:_\-.]{5,63}$",
        ErrorMessage =
            "Use letters, numbers, colons, hyphens, periods or underscores.")]
    public string DeviceIdentifier { get; set; } = string.Empty;

    [Required(ErrorMessage = "A sensor name is required.")]
    [StringLength(
        80,
        MinimumLength = 3,
        ErrorMessage =
            "The sensor name must contain between 3 and 80 characters.")]
    public string DisplayName { get; set; } = string.Empty;

    [Required(ErrorMessage = "A facility is required.")]
    [StringLength(
        100,
        MinimumLength = 2,
        ErrorMessage =
            "The facility must contain between 2 and 100 characters.")]
    public string Facility { get; set; } = string.Empty;

    [Required(ErrorMessage = "A deployment node identifier is required.")]
    [StringLength(
        80,
        MinimumLength = 2,
        ErrorMessage =
            "The node identifier must contain between 2 and 80 characters.")]
    [RegularExpression(
        @"^[A-Za-z0-9][A-Za-z0-9:_\-. ]{1,79}$",
        ErrorMessage =
            "The node identifier contains unsupported characters.")]
    public string NodeId { get; set; } = string.Empty;

    [Required(ErrorMessage = "A deployment zone is required.")]
    [StringLength(
        80,
        MinimumLength = 2,
        ErrorMessage =
            "The zone must contain between 2 and 80 characters.")]
    public string Zone { get; set; } = string.Empty;

    [Required(ErrorMessage = "A deployment sub-zone is required.")]
    [StringLength(
        80,
        MinimumLength = 2,
        ErrorMessage =
            "The sub-zone must contain between 2 and 80 characters.")]
    public string SubZone { get; set; } = string.Empty;

    [Range(
        1,
        3,
        ErrorMessage = "Select a sensor category.")]
    public int Category { get; set; }

    [Range(
        1,
        3,
        ErrorMessage = "Select a telemetry data type.")]
    public int DataType { get; set; } =
        (int)TelemetryDataTypeOption.FloatingPoint;

    [Required(ErrorMessage = "A measurement unit is required.")]
    [StringLength(
        40,
        MinimumLength = 1,
        ErrorMessage =
            "The measurement unit cannot exceed 40 characters.")]
    public string Unit { get; set; } = string.Empty;

    [Range(
        1,
        86400,
        ErrorMessage =
            "The publishing interval must be between 1 and 86,400 seconds.")]
    public int PublishingIntervalSeconds { get; set; } = 30;

    public double? ExpectedMinimum { get; set; } = 0;

    public double? ExpectedMaximum { get; set; } = 100;

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        bool isNumeric =
            DataType ==
                (int)TelemetryDataTypeOption.FloatingPoint ||
            DataType ==
                (int)TelemetryDataTypeOption.Integer;

        if (!isNumeric)
        {
            yield break;
        }

        if (!ExpectedMinimum.HasValue)
        {
            yield return new ValidationResult(
                "An expected minimum is required for numeric sensors.",
                new[]
                {
                    nameof(ExpectedMinimum)
                });
        }

        if (!ExpectedMaximum.HasValue)
        {
            yield return new ValidationResult(
                "An expected maximum is required for numeric sensors.",
                new[]
                {
                    nameof(ExpectedMaximum)
                });
        }

        if (ExpectedMinimum.HasValue &&
            ExpectedMaximum.HasValue &&
            ExpectedMinimum.Value >= ExpectedMaximum.Value)
        {
            yield return new ValidationResult(
                "The expected maximum must be greater than the expected minimum.",
                new[]
                {
                    nameof(ExpectedMinimum),
                    nameof(ExpectedMaximum)
                });
        }
    }
}

/// <summary>
/// Request sent to POST api/sensors.
/// Property names match RegisterSensorRequest in SmartX.Api.
/// </summary>
public sealed record RegisterSensorRequest(
    [property: JsonPropertyName("deviceIdentifier")]
    string DeviceIdentifier,

    [property: JsonPropertyName("displayName")]
    string DisplayName,

    [property: JsonPropertyName("facility")]
    string Facility,

    [property: JsonPropertyName("zone")]
    string Zone,

    [property: JsonPropertyName("subZone")]
    string SubZone,

    [property: JsonPropertyName("nodeId")]
    string NodeId,

    [property: JsonPropertyName("category")]
    SensorCategoryOption Category,

    [property: JsonPropertyName("dataType")]
    TelemetryDataTypeOption DataType,

    [property: JsonPropertyName("unit")]
    string Unit,

    [property: JsonPropertyName("expectedMinimum")]
    double? ExpectedMinimum,

    [property: JsonPropertyName("expectedMaximum")]
    double? ExpectedMaximum,

    [property: JsonPropertyName("publishingIntervalSeconds")]
    int PublishingIntervalSeconds);

/// <summary>
/// Minimum response information required after registration.
/// Additional API properties are ignored automatically.
/// </summary>
public sealed class RegisteredSensorResponse
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("deviceIdentifier")]
    public string DeviceIdentifier { get; init; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; init; } = string.Empty;
}

/// <summary>
/// Represents RFC 7807 validation or API problem information.
/// </summary>
public sealed class ApiProblemResponse
{
    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("detail")]
    public string? Detail { get; init; }

    [JsonPropertyName("status")]
    public int? Status { get; init; }

    [JsonPropertyName("errors")]
    public Dictionary<string, string[]>? Errors { get; init; }
}