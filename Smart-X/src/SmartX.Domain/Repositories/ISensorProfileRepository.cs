using SmartX.Domain.Entities;
using SmartX.Domain.ValueObjects;

namespace SmartX.Domain.Repositories;

/// <summary>
/// Defines asynchronous storage operations for Smart-X sensor profiles.
/// </summary>
public interface ISensorProfileRepository
{
    /// <summary>
    /// Returns all registered sensor profiles.
    /// </summary>
    ValueTask<IReadOnlyList<SensorProfile>> GetAllAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a sensor profile using its internal identifier.
    /// </summary>
    ValueTask<SensorProfile?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a sensor using its MAC address or unique device identifier.
    /// </summary>
    ValueTask<SensorProfile?> GetByDeviceIdentifierAsync(
        DeviceIdentifier identifier,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a sensor profile if its identifiers are unique.
    /// </summary>
    ValueTask<bool> AddAsync(
        SensorProfile sensor,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the current number of registered sensor profiles.
    /// </summary>
    ValueTask<int> CountAsync(
        CancellationToken cancellationToken = default);
}