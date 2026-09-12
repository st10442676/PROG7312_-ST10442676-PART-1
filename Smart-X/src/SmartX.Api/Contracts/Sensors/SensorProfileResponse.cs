using SmartX.Domain.Entities;
using SmartX.Domain.Enums;

namespace SmartX.Api.Contracts.Sensors;

/// <summary>
/// Contains a dashboard-ready representation of a sensor profile.
/// </summary>
public sealed record SensorProfileResponse(
    Guid Id,
    string DeviceIdentifier,
    string DisplayName,
    string Facility,
    string Zone,
    string? SubZone,
    string NodeId,
    string DeploymentPath,
    SensorCategory Category,
    TelemetryDataType DataType,
    string Unit,
    double? ExpectedMinimum,
    double? ExpectedMaximum,
    int PublishingIntervalSeconds,
    SensorConnectionStatus ConnectionStatus,
    DateTimeOffset RegisteredAtUtc,
    DateTimeOffset? LastSeenAtUtc)
{
    /// <summary>
    /// Converts a domain sensor profile into an API response.
    /// </summary>
    public static SensorProfileResponse FromDomain(
        SensorProfile sensor)
    {
        ArgumentNullException.ThrowIfNull(sensor);

        return new SensorProfileResponse(
            sensor.Id,
            sensor.Identifier.Value,
            sensor.DisplayName,
            sensor.Location.Facility,
            sensor.Location.Zone,
            sensor.Location.SubZone,
            sensor.Location.NodeId,
            sensor.Location.Path,
            sensor.Category,
            sensor.DataType,
            sensor.Unit,
            sensor.ExpectedMinimum,
            sensor.ExpectedMaximum,
            checked((int)sensor.PublishingInterval.TotalSeconds),
            sensor.ConnectionStatus,
            sensor.RegisteredAtUtc,
            sensor.LastSeenAtUtc);
    }
}