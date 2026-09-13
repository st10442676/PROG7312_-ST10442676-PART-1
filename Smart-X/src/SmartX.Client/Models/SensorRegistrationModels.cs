using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SmartX.Client.Models;

/// <summary>
/// Sensor categories supported by the Smart-X API.
/// Numeric values match the domain enum.
/// </summary>
public enum SensorCategoryOption
{
    Environmental = 1,
    PowerConsumption = 2,
    Actuator = 3
}

/// <summary>
/// Telemetry value types supported by the Smart-X API.
/// Numeric values match the domain enum.
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
public sealed class RegisterSensorFormModel
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

    [Required(ErrorMessage = "A building or site is required.")]
    [StringLength(
        80,
        MinimumLength = 2,
        ErrorMessage =
            "The building or site must contain between 2 and 80 characters.")]
    public string Building { get; set; } = string.Empty;

    [Required(ErrorMessage = "A floor or level is required.")]
    [StringLength(
        40,
        MinimumLength = 1,
        ErrorMessage =
            "The floor or level cannot exceed 40 characters.")]
    public string Floor { get; set; } = string.Empty;

    [Required(ErrorMessage = "A deployment zone is required.")]
    [StringLength(
        80,
        MinimumLength = 2,
        ErrorMessage =
            "The deployment zone must contain between 2 and 80 characters.")]
    public string Zone { get; set; } = string.Empty;

    [Range(
        1,
        3,
        ErrorMessage = "Select a sensor category.")]
    public int Category { get; set; }

    [Range(
        1,
        86400,
        ErrorMessage =
            "The publishing interval must be between 1 and 86,400 seconds.")]
    public int PublishingIntervalSeconds { get; set; } = 30;
}

/// <summary>
/// Request sent to the Smart-X sensor-registration API.
///
/// This constructor accepts the existing registration-page values
/// and expands them into the complete API contract.
/// </summary>
public sealed class RegisterSensorRequest
{
    public RegisterSensorRequest(
        string deviceIdentifier,
        string displayName,
        string building,
        string floor,
        string zone,
        SensorCategoryOption category,
        int publishingIntervalSeconds)
    {
        DeviceIdentifier = deviceIdentifier;
        DisplayName = displayName;

        Facility = building;
        Zone = zone;
        SubZone = floor;
        NodeId = deviceIdentifier;

        Category = category;

        DataType =
            category == SensorCategoryOption.Actuator
                ? TelemetryDataTypeOption.Boolean
                : TelemetryDataTypeOption.FloatingPoint;

        Unit =
            category switch
            {
                SensorCategoryOption.Environmental => "°C",
                SensorCategoryOption.PowerConsumption => "kWh",
                SensorCategoryOption.Actuator => "state",
                _ => "unit"
            };

        if (DataType == TelemetryDataTypeOption.Boolean)
        {
            ExpectedMinimum = null;
            ExpectedMaximum = null;
        }
        else if (category ==
                 SensorCategoryOption.PowerConsumption)
        {
            ExpectedMinimum = 0;
            ExpectedMaximum = 100_000;
        }
        else
        {
            ExpectedMinimum = -50;
            ExpectedMaximum = 100;
        }

        PublishingIntervalSeconds =
            publishingIntervalSeconds;
    }

    [JsonPropertyName("deviceIdentifier")]
    public string DeviceIdentifier { get; }

    [JsonPropertyName("displayName")]
    public string DisplayName { get; }

    [JsonPropertyName("facility")]
    public string Facility { get; }

    [JsonPropertyName("zone")]
    public string Zone { get; }

    [JsonPropertyName("subZone")]
    public string SubZone { get; }

    [JsonPropertyName("nodeId")]
    public string NodeId { get; }

    [JsonPropertyName("category")]
    public SensorCategoryOption Category { get; }

    [JsonPropertyName("dataType")]
    public TelemetryDataTypeOption DataType { get; }

    [JsonPropertyName("unit")]
    public string Unit { get; }

    [JsonPropertyName("expectedMinimum")]
    public double? ExpectedMinimum { get; }

    [JsonPropertyName("expectedMaximum")]
    public double? ExpectedMaximum { get; }

    [JsonPropertyName("publishingIntervalSeconds")]
    public int PublishingIntervalSeconds { get; }
}

/// <summary>
/// Minimum response required after successful registration.
/// Additional API response properties are ignored.
/// </summary>
public sealed class RegisteredSensorResponse
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("deviceIdentifier")]
    public string DeviceIdentifier { get; init; } =
        string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; init; } =
        string.Empty;
}

/// <summary>
/// Represents an API validation or problem-details response.
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