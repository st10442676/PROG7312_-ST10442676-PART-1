using SmartX.Domain.Entities;
using SmartX.Domain.Repositories;
using SmartX.Domain.ValueObjects;

namespace SmartX.Api.Repositories;

/// <summary>
/// Provides thread-safe, in-memory storage for registered sensor profiles.
/// </summary>
public sealed class InMemorySensorProfileRepository :
    ISensorProfileRepository,
    IDisposable
{
    private readonly ReaderWriterLockSlim _repositoryLock =
        new(LockRecursionPolicy.NoRecursion);

    private readonly Dictionary<Guid, SensorProfile> _sensorsById = [];

    private readonly Dictionary<string, Guid> _sensorIdsByIdentifier =
        new(StringComparer.OrdinalIgnoreCase);

    private bool _isDisposed;

    /// <inheritdoc />
    public ValueTask<IReadOnlyList<SensorProfile>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();

        _repositoryLock.EnterReadLock();

        try
        {
            List<SensorProfile> sensors =
                _sensorsById.Values
                    .OrderBy(sensor => sensor.DisplayName)
                    .ThenBy(sensor => sensor.Identifier.Value)
                    .ToList();

            IReadOnlyList<SensorProfile> snapshot =
                sensors.AsReadOnly();

            return ValueTask.FromResult(snapshot);
        }
        finally
        {
            _repositoryLock.ExitReadLock();
        }
    }

    /// <inheritdoc />
    public ValueTask<SensorProfile?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();

        if (id == Guid.Empty)
        {
            return ValueTask.FromResult<SensorProfile?>(null);
        }

        _repositoryLock.EnterReadLock();

        try
        {
            _sensorsById.TryGetValue(
                id,
                out SensorProfile? sensor);

            return ValueTask.FromResult(sensor);
        }
        finally
        {
            _repositoryLock.ExitReadLock();
        }
    }

    /// <inheritdoc />
    public ValueTask<SensorProfile?> GetByDeviceIdentifierAsync(
        DeviceIdentifier identifier,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(identifier);
        cancellationToken.ThrowIfCancellationRequested();

        _repositoryLock.EnterReadLock();

        try
        {
            if (!_sensorIdsByIdentifier.TryGetValue(
                    identifier.Value,
                    out Guid sensorId))
            {
                return ValueTask.FromResult<SensorProfile?>(null);
            }

            _sensorsById.TryGetValue(
                sensorId,
                out SensorProfile? sensor);

            return ValueTask.FromResult(sensor);
        }
        finally
        {
            _repositoryLock.ExitReadLock();
        }
    }

    /// <inheritdoc />
    public ValueTask<bool> AddAsync(
        SensorProfile sensor,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(sensor);
        cancellationToken.ThrowIfCancellationRequested();

        _repositoryLock.EnterWriteLock();

        try
        {
            bool idAlreadyExists =
                _sensorsById.ContainsKey(sensor.Id);

            bool identifierAlreadyExists =
                _sensorIdsByIdentifier.ContainsKey(
                    sensor.Identifier.Value);

            if (idAlreadyExists || identifierAlreadyExists)
            {
                return ValueTask.FromResult(false);
            }

            _sensorsById.Add(
                sensor.Id,
                sensor);

            _sensorIdsByIdentifier.Add(
                sensor.Identifier.Value,
                sensor.Id);

            return ValueTask.FromResult(true);
        }
        finally
        {
            _repositoryLock.ExitWriteLock();
        }
    }

    /// <inheritdoc />
    public ValueTask<int> CountAsync(
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();

        _repositoryLock.EnterReadLock();

        try
        {
            return ValueTask.FromResult(
                _sensorsById.Count);
        }
        finally
        {
            _repositoryLock.ExitReadLock();
        }
    }

    /// <summary>
    /// Releases repository synchronisation resources.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _repositoryLock.Dispose();
        _isDisposed = true;
    }

    private void ThrowIfDisposed()
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(
                nameof(InMemorySensorProfileRepository));
        }
    }
}