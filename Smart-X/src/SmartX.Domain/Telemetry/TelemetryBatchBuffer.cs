namespace SmartX.Domain.Telemetry;

/// <summary>
/// Stores sequential telemetry readings in a memory-conscious jagged
/// array before transferring them into an optimised List&lt;T&gt;.
/// </summary>
/// <typeparam name="T">
/// The strongly typed telemetry item stored by the buffer.
/// </typeparam>
public sealed class TelemetryBatchBuffer<T>
    where T : notnull
{
    private readonly T[]?[] _batches;
    private readonly int[] _batchItemCounts;
    private readonly int _readingsPerBatch;

    private int _activeBatchIndex;

    /// <summary>
    /// Creates a jagged telemetry batch buffer.
    /// </summary>
    /// <param name="batchCapacity">
    /// The maximum number of sequential batches.
    /// </param>
    /// <param name="readingsPerBatch">
    /// The maximum number of readings stored in each batch.
    /// </param>
    public TelemetryBatchBuffer(
        int batchCapacity,
        int readingsPerBatch)
    {
        if (batchCapacity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(batchCapacity),
                "Batch capacity must be greater than zero.");
        }

        if (readingsPerBatch <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(readingsPerBatch),
                "Readings per batch must be greater than zero.");
        }

        BatchCapacity = batchCapacity;
        _readingsPerBatch = readingsPerBatch;

        _batches = new T[]?[batchCapacity];
        _batchItemCounts = new int[batchCapacity];
    }

    /// <summary>
    /// Gets the maximum number of batches supported by the buffer.
    /// </summary>
    public int BatchCapacity { get; }

    /// <summary>
    /// Gets the maximum number of readings stored in each batch.
    /// </summary>
    public int ReadingsPerBatch =>
        _readingsPerBatch;

    /// <summary>
    /// Gets the maximum number of readings supported by the buffer.
    /// </summary>
    public int TotalCapacity =>
        BatchCapacity * ReadingsPerBatch;

    /// <summary>
    /// Gets the total number of readings currently in the buffer.
    /// </summary>
    public int Count { get; private set; }

    /// <summary>
    /// Gets the number of batches that currently contain readings.
    /// </summary>
    public int BatchCount =>
        Count == 0
            ? 0
            : ((Count - 1) / ReadingsPerBatch) + 1;

    /// <summary>
    /// Gets a value indicating whether the buffer has reached capacity.
    /// </summary>
    public bool IsFull =>
        Count >= TotalCapacity;

    /// <summary>
    /// Gets a value indicating whether no telemetry is buffered.
    /// </summary>
    public bool IsEmpty =>
        Count == 0;

    /// <summary>
    /// Attempts to append a telemetry item to the active sequential batch.
    /// </summary>
    /// <returns>
    /// True when the item is stored; otherwise false when the buffer is full.
    /// </returns>
    public bool TryAdd(T item)
    {
        if (IsFull)
        {
            return false;
        }

        T[] activeBatch =
            _batches[_activeBatchIndex] ??=
                new T[ReadingsPerBatch];

        int insertionIndex =
            _batchItemCounts[_activeBatchIndex];

        activeBatch[insertionIndex] = item;

        _batchItemCounts[_activeBatchIndex]++;
        Count++;

        if (_batchItemCounts[_activeBatchIndex] ==
            ReadingsPerBatch)
        {
            _activeBatchIndex++;
        }

        return true;
    }

    /// <summary>
    /// Adds a telemetry item or throws an exception when the buffer is full.
    /// </summary>
    public void Add(T item)
    {
        if (!TryAdd(item))
        {
            throw new InvalidOperationException(
                $"The telemetry buffer has reached its capacity of " +
                $"{TotalCapacity} readings.");
        }
    }

    /// <summary>
    /// Creates a safe snapshot containing only populated array positions.
    /// </summary>
    public T[][] CreateSnapshot()
    {
        T[][] snapshot = new T[BatchCount][];

        for (int batchIndex = 0;
             batchIndex < BatchCount;
             batchIndex++)
        {
            int itemCount =
                _batchItemCounts[batchIndex];

            T[] sourceBatch =
                _batches[batchIndex] ??
                throw new InvalidOperationException(
                    "An expected telemetry batch was not allocated.");

            T[] copiedBatch = new T[itemCount];

            Array.Copy(
                sourceBatch,
                copiedBatch,
                itemCount);

            snapshot[batchIndex] = copiedBatch;
        }

        return snapshot;
    }

    /// <summary>
    /// Transfers all buffered telemetry into a pre-sized List&lt;T&gt;
    /// and resets the array buffer for reuse.
    /// </summary>
    public List<T> DrainToOptimisedList()
    {
        List<T> optimisedTelemetry = new(Count);

        int populatedBatchCount = BatchCount;

        for (int batchIndex = 0;
             batchIndex < populatedBatchCount;
             batchIndex++)
        {
            T[] sourceBatch =
                _batches[batchIndex] ??
                throw new InvalidOperationException(
                    "An expected telemetry batch was not allocated.");

            int itemCount =
                _batchItemCounts[batchIndex];

            for (int itemIndex = 0;
                 itemIndex < itemCount;
                 itemIndex++)
            {
                optimisedTelemetry.Add(
                    sourceBatch[itemIndex]);
            }
        }

        Clear();

        return optimisedTelemetry;
    }

    /// <summary>
    /// Clears buffered telemetry while retaining allocated arrays for reuse.
    /// </summary>
    public void Clear()
    {
        for (int batchIndex = 0;
             batchIndex < _batches.Length;
             batchIndex++)
        {
            T[]? batch = _batches[batchIndex];

            if (batch is not null)
            {
                Array.Clear(batch);
            }

            _batchItemCounts[batchIndex] = 0;
        }

        _activeBatchIndex = 0;
        Count = 0;
    }
}