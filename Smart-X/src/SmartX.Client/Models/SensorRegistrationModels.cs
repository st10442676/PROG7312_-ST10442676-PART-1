using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SmartX.Client.Models;

/// <summary>
/// Sensor category values matching the Smart-X domain categories.
/// Explicit numeric values ensure compatibility with the API enum.
/// </summary>
public enum SensorCategoryOption
{
    Environmental = 1,
    PowerConsumption = 2,
    Actuator = 3
}

/// <summary>
/// Contains and validates values entered into the sensor-registration form.
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
/// Request sent to the Smart-X sensor-registration endpoint.
/// </summary>
public sealed record RegisterSensorRequest(
    [property: JsonPropertyName("deviceIdentifier")]
    string DeviceIdentifier,

    [property: JsonPropertyName("displayName")]
    string DisplayName,

    [property: JsonPropertyName("building")]
    string Building,

    [property: JsonPropertyName("floor")]
    string Floor,

    [property: JsonPropertyName("zone")]
    string Zone,

    [property: JsonPropertyName("category")]
    SensorCategoryOption Category,

    [property: JsonPropertyName("publishingIntervalSeconds")]
    int PublishingIntervalSeconds);

/// <summary>
/// Minimum response information required after successful registration.
/// Additional API response fields are safely ignored by the JSON parser.
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