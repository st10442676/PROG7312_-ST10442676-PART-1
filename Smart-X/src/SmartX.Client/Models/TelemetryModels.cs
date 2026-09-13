using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SmartX.Client.Models;

/// <summary>
/// Contains values entered into the telemetry-submission form.
/// </summary>
public sealed class TelemetrySubmissionFormModel :
    IValidatableObject
{
    [Required(
        ErrorMessage = "A sensor profile identifier is required.")]
    public string SensorId { get; set; } = string.Empty;

    [Range(
        1,
        int.MaxValue,
        ErrorMessage =
            "The sequence number must be greater than zero.")]
    public int SequenceNumber { get; set; } = 1;

    [Range(
        1,
        3,
        ErrorMessage = "Select a telemetry data type.")]
    public int DataType { get; set; } =
        (int)TelemetryDataTypeOption.FloatingPoint;

    public double FloatingPointValue { get; set; }

    public int IntegerValue { get; set; }

    public bool BooleanValue { get; set; }

    public DateTimeOffset CapturedAtUtc { get; set; } =
        DateTimeOffset.UtcNow;

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(SensorId))
        {
            yield break;
        }

        if (!Guid.TryParse(
                SensorId.Trim(),
                out Guid parsedSensorId) ||
            parsedSensorId == Guid.Empty)
        {
            yield return new ValidationResult(
                "Enter a valid sensor profile identifier.",
                new[]
                {
                    nameof(SensorId)
                });
        }
    }
}

/// <summary>
/// Generic telemetry packet sent to the Smart-X API.
/// The same structure supports float, integer, and Boolean values.
/// </summary>
/// <typeparam name="T">
/// The type of telemetry value carried by the packet.
/// </typeparam>
public sealed record TelemetrySubmissionRequest<T>(
    [property: JsonPropertyName("sequenceNumber")]
    int SequenceNumber,

    [property: JsonPropertyName("value")]
    T Value,

    [property: JsonPropertyName("capturedAtUtc")]
    DateTimeOffset CapturedAtUtc);

/// <summary>
/// Represents one processed telemetry reading returned by the API.
/// </summary>
/// <typeparam name="T">
/// The underlying telemetry value type.
/// </typeparam>
public sealed class TelemetryReadingResponse<T>
{
    [JsonPropertyName("packetId")]
    public Guid PacketId { get; init; }

    [JsonPropertyName("sensorId")]
    public Guid SensorId { get; init; }

    [JsonPropertyName("sequenceNumber")]
    public int SequenceNumber { get; init; }

    [JsonPropertyName("value")]
    public T? Value { get; init; }

    [JsonPropertyName("unit")]
    public string Unit { get; init; } = string.Empty;

    [JsonPropertyName("valueType")]
    public string ValueType { get; init; } = string.Empty;

    [JsonPropertyName("capturedAtUtc")]
    public DateTimeOffset CapturedAtUtc { get; init; }

    [JsonPropertyName("receivedAtUtc")]
    public DateTimeOffset ReceivedAtUtc { get; init; }

    [JsonPropertyName("ingestionLatencyMilliseconds")]
    public double IngestionLatencyMilliseconds { get; init; }

    [JsonPropertyName("healthState")]
    public string HealthState { get; init; } = string.Empty;

    [JsonPropertyName("ingestionState")]
    public string IngestionState { get; init; } = string.Empty;

    [JsonPropertyName("message")]
    public string? Message { get; init; }
}

/// <summary>
/// Contains telemetry history grouped by its generic value type.
/// </summary>
public sealed class SensorTelemetryHistoryResponse
{
    [JsonPropertyName("sensorId")]
    public Guid SensorId { get; init; }

    [JsonPropertyName("floatingPointReadings")]
    public IReadOnlyList<TelemetryReadingResponse<float>>
        FloatingPointReadings
    { get; init; } =
            Array.Empty<TelemetryReadingResponse<float>>();

    [JsonPropertyName("integerReadings")]
    public IReadOnlyList<TelemetryReadingResponse<int>>
        IntegerReadings
    { get; init; } =
            Array.Empty<TelemetryReadingResponse<int>>();

    [JsonPropertyName("booleanReadings")]
    public IReadOnlyList<TelemetryReadingResponse<bool>>
        BooleanReadings
    { get; init; } =
            Array.Empty<TelemetryReadingResponse<bool>>();
}