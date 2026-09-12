using SmartX.Domain.Enums;
using SmartX.Domain.Telemetry;

namespace SmartX.Api.Contracts.Telemetry;

/// <summary>
/// Contains a strongly typed telemetry value submitted by a sensor.
/// </summary>
public sealed class IngestTelemetryRequest<T>
    where T : struct
{
    public long SequenceNumber { get; init; }

    public T Value { get; init; }

    public DateTimeOffset CapturedAtUtc { get; init; }
}

/// <summary>
/// Describes the result of a strongly typed telemetry ingestion.
/// </summary>
public sealed record TelemetryIngestionResponse<T>(
    Guid PacketId,
    Guid SensorId,
    long SequenceNumber,
    T Value,
    string Unit,
    string ValueType,
    DateTimeOffset CapturedAtUtc,
    DateTimeOffset ReceivedAtUtc,
    double IngestionLatencyMilliseconds,
    TelemetryHealthState HealthState,
    TelemetryIngestionState IngestionState,
    string? Message)
    where T : struct
{
    public static TelemetryIngestionResponse<T> FromDomain(
        TelemetryPacket<T> packet,
        TelemetryIngestionState ingestionState,
        string? message)
    {
        ArgumentNullException.ThrowIfNull(packet);

        return new TelemetryIngestionResponse<T>(
            packet.PacketId,
            packet.SensorId,
            packet.SequenceNumber,
            packet.Value,
            packet.Unit,
            packet.ValueTypeName,
            packet.CapturedAtUtc,
            packet.ReceivedAtUtc,
            packet.IngestionLatency.TotalMilliseconds,
            packet.HealthState,
            ingestionState,
            message);
    }
}

/// <summary>
/// Contains all telemetry retained for a particular sensor.
/// </summary>
public sealed record SensorTelemetryHistoryResponse(
    Guid SensorId,
    IReadOnlyList<TelemetryIngestionResponse<float>>
        FloatingPointReadings,
    IReadOnlyList<TelemetryIngestionResponse<int>>
        IntegerReadings,
    IReadOnlyList<TelemetryIngestionResponse<bool>>
        BooleanReadings);