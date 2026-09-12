using System.Collections;
using SmartX.Domain.Enums;

namespace SmartX.Domain.Telemetry;

/// <summary>
/// Provides a bounded, thread-safe custom collection for tracking
/// telemetry ingestion results.
/// </summary>
/// <typeparam name="T">
/// The strongly typed telemetry item stored in each ingestion record.
/// </typeparam>
public sealed class TelemetryIngestionCollection<T> :
    IReadOnlyCollection<TelemetryIngestionRecord<T>>
    where T : notnull
{
    private const int DefaultCapacity = 1_000;

    private readonly object _synchronisationLock = new();

    private readonly List<TelemetryIngestionRecord<T>> _records;

    private readonly int _trimCount;

    /// <summary>
    /// Creates a bounded ingestion collection.
    /// </summary>
    /// <param name="capacity">
    /// The maximum number of recent records retained in memory.
    /// </param>
    public TelemetryIngestionCollection(
        int capacity = DefaultCapacity)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(capacity),
                "Collection capacity must be greater than zero.");
        }

        Capacity = capacity;

        _records =
            new List<TelemetryIngestionRecord<T>>(capacity);

        // Remove records in small batches instead of shifting the
        // entire list every time a single new item arrives.
        _trimCount = Math.Max(1, capacity / 10);
    }

    /// <summary>
    /// Gets the maximum number of records retained in memory.
    /// </summary>
    public int Capacity { get; }

    /// <summary>
    /// Gets the current number of ingestion records.
    /// </summary>
    public int Count
    {
        get
        {
            lock (_synchronisationLock)
            {
                return _records.Count;
            }
        }
    }

    /// <summary>
    /// Adds an accepted telemetry item.
    /// </summary>
    public void AddAccepted(
        T item,
        DateTimeOffset? processedAtUtc = null)
    {
        Add(
            new TelemetryIngestionRecord<T>(
                item,
                TelemetryIngestionState.Accepted,
                processedAtUtc ?? DateTimeOffset.UtcNow));
    }

    /// <summary>
    /// Adds a telemetry item that requires investigation.
    /// </summary>
    public void AddFlagged(
        T item,
        string message,
        DateTimeOffset? processedAtUtc = null)
    {
        Add(
            new TelemetryIngestionRecord<T>(
                item,
                TelemetryIngestionState.Flagged,
                processedAtUtc ?? DateTimeOffset.UtcNow,
                message));
    }

    /// <summary>
    /// Adds a rejected telemetry item and its rejection reason.
    /// </summary>
    public void AddRejected(
        T item,
        string message,
        DateTimeOffset? processedAtUtc = null)
    {
        Add(
            new TelemetryIngestionRecord<T>(
                item,
                TelemetryIngestionState.Rejected,
                processedAtUtc ?? DateTimeOffset.UtcNow,
                message));
    }

    /// <summary>
    /// Adds a prepared ingestion record to the collection.
    /// </summary>
    public void Add(
        TelemetryIngestionRecord<T> record)
    {
        ArgumentNullException.ThrowIfNull(record);

        lock (_synchronisationLock)
        {
            TrimOldestRecordsWhenRequired();
            _records.Add(record);
        }
    }

    /// <summary>
    /// Returns the number of records with the specified state.
    /// </summary>
    public int CountByState(
        TelemetryIngestionState state)
    {
        EnsureValidState(state);

        lock (_synchronisationLock)
        {
            return _records.Count(
                record => record.State == state);
        }
    }

    /// <summary>
    /// Returns a read-only snapshot filtered by ingestion state.
    /// </summary>
    public IReadOnlyList<TelemetryIngestionRecord<T>> GetByState(
        TelemetryIngestionState state)
    {
        EnsureValidState(state);

        lock (_synchronisationLock)
        {
            List<TelemetryIngestionRecord<T>> matches =
                _records.FindAll(
                    record => record.State == state);

            return matches.AsReadOnly();
        }
    }

    /// <summary>
    /// Attempts to retrieve the most recently processed record.
    /// </summary>
    public bool TryGetLatest(
        out TelemetryIngestionRecord<T>? latestRecord)
    {
        lock (_synchronisationLock)
        {
            if (_records.Count == 0)
            {
                latestRecord = null;
                return false;
            }

            latestRecord = _records[^1];
            return true;
        }
    }

    /// <summary>
    /// Creates a safe read-only snapshot of all retained records.
    /// </summary>
    public IReadOnlyList<TelemetryIngestionRecord<T>> CreateSnapshot()
    {
        lock (_synchronisationLock)
        {
            List<TelemetryIngestionRecord<T>> snapshot =
                new(_records);

            return snapshot.AsReadOnly();
        }
    }

    /// <summary>
    /// Removes all retained ingestion records.
    /// </summary>
    public void Clear()
    {
        lock (_synchronisationLock)
        {
            _records.Clear();
        }
    }

    /// <summary>
    /// Returns a snapshot enumerator so callers cannot modify
    /// the collection while iterating over it.
    /// </summary>
    public IEnumerator<TelemetryIngestionRecord<T>> GetEnumerator()
    {
        return CreateSnapshot().GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    private void TrimOldestRecordsWhenRequired()
    {
        if (_records.Count < Capacity)
        {
            return;
        }

        int recordsToRemove =
            Math.Min(_trimCount, _records.Count);

        _records.RemoveRange(
            index: 0,
            count: recordsToRemove);
    }

    private static void EnsureValidState(
        TelemetryIngestionState state)
    {
        if (!Enum.IsDefined(state))
        {
            throw new ArgumentOutOfRangeException(
                nameof(state),
                state,
                "The telemetry ingestion state is not supported.");
        }
    }
}