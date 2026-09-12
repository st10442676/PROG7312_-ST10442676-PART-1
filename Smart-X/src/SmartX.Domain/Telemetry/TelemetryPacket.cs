using SmartX.Domain.Enums;

namespace SmartX.Domain.Telemetry;

/// <summary>
/// Provides a reusable, strongly typed wrapper for telemetry received
/// from a Smart-X sensor.
/// </summary>
/// <typeparam name="T">
/// The original telemetry value type. Smart-X currently supports
/// float, int and bool values.
/// </typeparam>
public sealed class TelemetryPacket<T>
    where T : notnull
{
    private const int MaximumUnitLength = 20;

    private TelemetryPacket(
        Guid packetId,
        Guid sensorId,
        long sequenceNumber,
        T value,
        string unit,
        DateTimeOffset capturedAtUtc,
        DateTimeOffset receivedAtUtc,
        TelemetryHealthState healthState)
    {
        PacketId = packetId;
        SensorId = sensorId;
        SequenceNumber = sequenceNumber;
        Value = value;
        Unit = unit;
        CapturedAtUtc = capturedAtUtc;
        ReceivedAtUtc = receivedAtUtc;
        HealthState = healthState;
    }

    /// <summary>
    /// Gets the unique identifier assigned to this telemetry packet.
    /// </summary>
    public Guid PacketId { get; }

    /// <summary>
    /// Gets the internal profile identifier of the source sensor.
    /// </summary>
    public Guid SensorId { get; }

    /// <summary>
    /// Gets the sensor-generated packet sequence number.
    /// </summary>
    public long SequenceNumber { get; }

    /// <summary>
    /// Gets the telemetry value in its original strongly typed form.
    /// </summary>
    public T Value { get; }

    /// <summary>
    /// Gets the unit of measurement associated with the value.
    /// </summary>
    public string Unit { get; }

    /// <summary>
    /// Gets the UTC timestamp recorded by the sensor.
    /// </summary>
    public DateTimeOffset CapturedAtUtc { get; }

    /// <summary>
    /// Gets the UTC timestamp at which the gateway received the packet.
    /// </summary>
    public DateTimeOffset ReceivedAtUtc { get; }

    /// <summary>
    /// Gets the current validation and health state of the reading.
    /// </summary>
    public TelemetryHealthState HealthState { get; private set; }

    /// <summary>
    /// Gets the time taken for the packet to reach the gateway.
    /// </summary>
    public TimeSpan IngestionLatency =>
        ReceivedAtUtc - CapturedAtUtc;

    /// <summary>
    /// Gets the name of the value's original CLR data type.
    /// </summary>
    public string ValueTypeName =>
        typeof(T).Name;

    /// <summary>
    /// Creates and validates a strongly typed telemetry packet.
    /// </summary>
    public static TelemetryPacket<T> Create(
        Guid sensorId,
        long sequenceNumber,
        T value,
        string unit,
        DateTimeOffset capturedAtUtc,
        DateTimeOffset? receivedAtUtc = null,
        TelemetryHealthState healthState =
            TelemetryHealthState.Normal)
    {
        EnsureSupportedType();

        if (sensorId == Guid.Empty)
        {
            throw new ArgumentException(
                "A valid sensor profile identifier is required.",
                nameof(sensorId));
        }

        if (sequenceNumber < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sequenceNumber),
                "The telemetry sequence number cannot be negative.");
        }

        string normalisedUnit = NormaliseUnit(unit);

        DateTimeOffset actualReceivedAtUtc =
            receivedAtUtc ?? DateTimeOffset.UtcNow;

        if (capturedAtUtc > actualReceivedAtUtc)
        {
            throw new ArgumentOutOfRangeException(
                nameof(capturedAtUtc),
                "The captured timestamp cannot be later than the received timestamp.");
        }

        if (!Enum.IsDefined(healthState))
        {
            throw new ArgumentOutOfRangeException(
                nameof(healthState),
                healthState,
                "The telemetry health state is not supported.");
        }

        return new TelemetryPacket<T>(
            Guid.NewGuid(),
            sensorId,
            sequenceNumber,
            value,
            normalisedUnit,
            capturedAtUtc,
            actualReceivedAtUtc,
            healthState);
    }

    /// <summary>
    /// Updates the packet after anomaly or range validation.
    /// </summary>
    public void UpdateHealthState(
        TelemetryHealthState healthState)
    {
        if (!Enum.IsDefined(healthState))
        {
            throw new ArgumentOutOfRangeException(
                nameof(healthState),
                healthState,
                "The telemetry health state is not supported.");
        }

        HealthState = healthState;
    }

    /// <summary>
    /// Determines whether this packet contains a floating-point value.
    /// </summary>
    public bool ContainsFloatingPointValue()
    {
        return typeof(T) == typeof(float);
    }

    /// <summary>
    /// Determines whether this packet contains an integer value.
    /// </summary>
    public bool ContainsIntegerValue()
    {
        return typeof(T) == typeof(int);
    }

    /// <summary>
    /// Determines whether this packet contains a Boolean value.
    /// </summary>
    public bool ContainsBooleanValue()
    {
        return typeof(T) == typeof(bool);
    }

    private static void EnsureSupportedType()
    {
        Type suppliedType = typeof(T);

        bool isSupported =
            suppliedType == typeof(float) ||
            suppliedType == typeof(int) ||
            suppliedType == typeof(bool);

        if (!isSupported)
        {
            throw new NotSupportedException(
                $"Telemetry type '{suppliedType.Name}' is not supported. " +
                "Smart-X accepts float, int or bool telemetry.");
        }
    }

    private static string NormaliseUnit(string? unit)
    {
        if (string.IsNullOrWhiteSpace(unit))
        {
            throw new ArgumentException(
                "A telemetry unit is required.",
                nameof(unit));
        }

        string normalisedUnit = unit.Trim();

        if (normalisedUnit.Length > MaximumUnitLength)
        {
            throw new ArgumentException(
                $"The telemetry unit cannot exceed " +
                $"{MaximumUnitLength} characters.",
                nameof(unit));
        }

        return normalisedUnit;
    }
}