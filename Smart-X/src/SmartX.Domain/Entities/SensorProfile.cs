using SmartX.Domain.Enums;
using SmartX.Domain.ValueObjects;
using SensorDataType = SmartX.Domain.Enums.TelemetryDataType;

namespace SmartX.Domain.Entities;

/// <summary>
/// Represents a registered physical sensor in the Smart-X ecosystem.
/// </summary>
public sealed class SensorProfile
{
    private const int MinimumNameLength = 3;
    private const int MaximumNameLength = 100;
    private const int MaximumUnitLength = 20;

    private static readonly TimeSpan MinimumPublishingInterval =
        TimeSpan.FromSeconds(1);

    private static readonly TimeSpan MaximumPublishingInterval =
        TimeSpan.FromHours(24);

    private SensorProfile(
        Guid id,
        DeviceIdentifier identifier,
        string displayName,
        DeploymentLocation location,
        SensorCategory category,
        SensorDataType dataType,
        string unit,
        double? expectedMinimum,
        double? expectedMaximum,
        TimeSpan publishingInterval,
        DateTimeOffset registeredAtUtc)
    {
        Id = id;
        Identifier = identifier;
        DisplayName = displayName;
        Location = location;
        Category = category;
        DataType = dataType;
        Unit = unit;
        ExpectedMinimum = expectedMinimum;
        ExpectedMaximum = expectedMaximum;
        PublishingInterval = publishingInterval;
        RegisteredAtUtc = registeredAtUtc;
        ConnectionStatus = SensorConnectionStatus.Pending;
    }

    /// <summary>
    /// Gets the internally generated sensor profile identifier.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Gets the validated MAC address or unique device identifier.
    /// </summary>
    public DeviceIdentifier Identifier { get; }

    /// <summary>
    /// Gets the user-friendly name of the sensor.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the hierarchical deployment location.
    /// </summary>
    public DeploymentLocation Location { get; }

    /// <summary>
    /// Gets the operational category of the sensor.
    /// </summary>
    public SensorCategory Category { get; }

    /// <summary>
    /// Gets the strongly typed telemetry format.
    /// </summary>
    public SensorDataType DataType { get; }

    /// <summary>
    /// Gets the measurement unit shown on the dashboard.
    /// </summary>
    public string Unit { get; }

    /// <summary>
    /// Gets the expected minimum value for numeric sensors.
    /// </summary>
    public double? ExpectedMinimum { get; }

    /// <summary>
    /// Gets the expected maximum value for numeric sensors.
    /// </summary>
    public double? ExpectedMaximum { get; }

    /// <summary>
    /// Gets the sensor's expected telemetry publishing interval.
    /// </summary>
    public TimeSpan PublishingInterval { get; }

    /// <summary>
    /// Gets the sensor's current connection status.
    /// </summary>
    public SensorConnectionStatus ConnectionStatus { get; private set; }

    /// <summary>
    /// Gets the UTC time at which the sensor was registered.
    /// </summary>
    public DateTimeOffset RegisteredAtUtc { get; }

    /// <summary>
    /// Gets the UTC time at which telemetry was last received.
    /// </summary>
    public DateTimeOffset? LastSeenAtUtc { get; private set; }

    /// <summary>
    /// Creates and validates a new Smart-X sensor profile.
    /// </summary>
    public static SensorProfile Create(
        string deviceIdentifier,
        string displayName,
        string facility,
        string zone,
        string? subZone,
        string nodeId,
        SensorCategory category,
        SensorDataType dataType,
        string unit,
        double? expectedMinimum,
        double? expectedMaximum,
        TimeSpan publishingInterval,
        DateTimeOffset? registeredAtUtc = null)
    {
        DeviceIdentifier validatedIdentifier =
            DeviceIdentifier.Create(deviceIdentifier);

        DeploymentLocation validatedLocation =
            DeploymentLocation.Create(
                facility,
                zone,
                subZone,
                nodeId);

        string normalisedName = NormaliseRequiredText(
            displayName,
            nameof(displayName),
            MinimumNameLength,
            MaximumNameLength);

        ValidateEnum(category, nameof(category));
        ValidateEnum(dataType, nameof(dataType));

        string normalisedUnit = NormaliseRequiredText(
            unit,
            nameof(unit),
            1,
            MaximumUnitLength);

        ValidatePublishingInterval(publishingInterval);

        ValidateExpectedRange(
            dataType,
            expectedMinimum,
            expectedMaximum);

        return new SensorProfile(
            Guid.NewGuid(),
            validatedIdentifier,
            normalisedName,
            validatedLocation,
            category,
            dataType,
            normalisedUnit,
            expectedMinimum,
            expectedMaximum,
            publishingInterval,
            registeredAtUtc ?? DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// Records incoming telemetry and marks the sensor as online.
    /// </summary>
    public void RecordTelemetry(DateTimeOffset receivedAtUtc)
    {
        if (receivedAtUtc < RegisteredAtUtc)
        {
            throw new ArgumentOutOfRangeException(
                nameof(receivedAtUtc),
                "Telemetry cannot be received before sensor registration.");
        }

        if (LastSeenAtUtc.HasValue &&
            receivedAtUtc < LastSeenAtUtc.Value)
        {
            throw new ArgumentOutOfRangeException(
                nameof(receivedAtUtc),
                "Telemetry cannot be older than the previous reading.");
        }

        LastSeenAtUtc = receivedAtUtc;
        ConnectionStatus = SensorConnectionStatus.Online;
    }

    /// <summary>
    /// Recalculates connectivity using the expected publishing interval.
    /// </summary>
    public SensorConnectionStatus RefreshConnectionStatus(
        DateTimeOffset evaluatedAtUtc)
    {
        if (ConnectionStatus == SensorConnectionStatus.Maintenance)
        {
            return ConnectionStatus;
        }

        if (!LastSeenAtUtc.HasValue)
        {
            ConnectionStatus = SensorConnectionStatus.Pending;
            return ConnectionStatus;
        }

        if (evaluatedAtUtc < LastSeenAtUtc.Value)
        {
            throw new ArgumentOutOfRangeException(
                nameof(evaluatedAtUtc),
                "Evaluation time cannot be earlier than the last reading.");
        }

        TimeSpan elapsedTime =
            evaluatedAtUtc - LastSeenAtUtc.Value;

        TimeSpan staleThreshold =
            PublishingInterval * 2;

        TimeSpan disconnectedThreshold =
            PublishingInterval * 4;

        if (elapsedTime <= staleThreshold)
        {
            ConnectionStatus = SensorConnectionStatus.Online;
        }
        else if (elapsedTime <= disconnectedThreshold)
        {
            ConnectionStatus = SensorConnectionStatus.Stale;
        }
        else
        {
            ConnectionStatus = SensorConnectionStatus.Disconnected;
        }

        return ConnectionStatus;
    }

    /// <summary>
    /// Places the sensor into intentional maintenance mode.
    /// </summary>
    public void PlaceInMaintenance()
    {
        ConnectionStatus = SensorConnectionStatus.Maintenance;
    }

    /// <summary>
    /// Removes maintenance mode and recalculates connectivity.
    /// </summary>
    public void ResumeMonitoring(DateTimeOffset evaluatedAtUtc)
    {
        ConnectionStatus = SensorConnectionStatus.Pending;
        RefreshConnectionStatus(evaluatedAtUtc);
    }

    private static void ValidatePublishingInterval(
        TimeSpan publishingInterval)
    {
        if (publishingInterval < MinimumPublishingInterval ||
            publishingInterval > MaximumPublishingInterval)
        {
            throw new ArgumentOutOfRangeException(
                nameof(publishingInterval),
                "Publishing interval must be between one second and 24 hours.");
        }
    }

    private static void ValidateExpectedRange(
        SensorDataType dataType,
        double? expectedMinimum,
        double? expectedMaximum)
    {
        if (dataType == SensorDataType.Boolean)
        {
            if (expectedMinimum.HasValue ||
                expectedMaximum.HasValue)
            {
                throw new ArgumentException(
                    "Boolean sensors cannot have a numeric expected range.");
            }

            return;
        }

        if (!expectedMinimum.HasValue ||
            !expectedMaximum.HasValue)
        {
            throw new ArgumentException(
                "Numeric sensors require an expected minimum and maximum.");
        }

        if (!double.IsFinite(expectedMinimum.Value) ||
            !double.IsFinite(expectedMaximum.Value))
        {
            throw new ArgumentException(
                "Expected ranges must contain finite numeric values.");
        }

        if (expectedMinimum.Value >= expectedMaximum.Value)
        {
            throw new ArgumentException(
                "Expected minimum must be lower than expected maximum.");
        }
    }

    private static void ValidateEnum<TEnum>(
        TEnum value,
        string parameterName)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                $"The supplied {typeof(TEnum).Name} is not supported.");
        }
    }

    private static string NormaliseRequiredText(
        string? value,
        string parameterName,
        int minimumLength,
        int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "A value is required.",
                parameterName);
        }

        string normalisedValue = string.Join(
            " ",
            value.Split(
                ' ',
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries));

        if (normalisedValue.Length < minimumLength ||
            normalisedValue.Length > maximumLength)
        {
            throw new ArgumentException(
                $"The value must contain between {minimumLength} " +
                $"and {maximumLength} characters.",
                parameterName);
        }

        return normalisedValue;
    }
}