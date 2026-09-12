using SmartX.Domain.Telemetry;
using Xunit;

namespace SmartX.Tests.Domain;

/// <summary>
/// Verifies jagged-array telemetry buffering and List&lt;T&gt; transfer.
/// </summary>
public sealed class TelemetryBatchBufferTests
{
    [Fact]
    public void Add_MultipleReadings_CreatesSequentialJaggedBatches()
    {
        TelemetryBatchBuffer<int> buffer =
            new(
                batchCapacity: 3,
                readingsPerBatch: 2);

        buffer.Add(10);
        buffer.Add(20);
        buffer.Add(30);
        buffer.Add(40);
        buffer.Add(50);

        int[][] snapshot =
            buffer.CreateSnapshot();

        Assert.Equal(5, buffer.Count);
        Assert.Equal(3, buffer.BatchCount);
        Assert.Equal(3, snapshot.Length);

        Assert.Equal(
            new[] { 10, 20 },
            snapshot[0]);

        Assert.Equal(
            new[] { 30, 40 },
            snapshot[1]);

        Assert.Equal(
            new[] { 50 },
            snapshot[2]);
    }

    [Fact]
    public void DrainToOptimisedList_TransfersValuesAndResetsBuffer()
    {
        TelemetryBatchBuffer<float> buffer =
            new(
                batchCapacity: 2,
                readingsPerBatch: 3);

        buffer.Add(20.5f);
        buffer.Add(21.5f);
        buffer.Add(22.5f);
        buffer.Add(23.5f);

        List<float> readings =
            buffer.DrainToOptimisedList();

        Assert.Equal(
            new[] { 20.5f, 21.5f, 22.5f, 23.5f },
            readings);

        Assert.True(buffer.IsEmpty);
        Assert.False(buffer.IsFull);
        Assert.Equal(0, buffer.Count);
        Assert.Equal(0, buffer.BatchCount);
    }

    [Fact]
    public void TryAdd_WhenBufferIsFull_ReturnsFalse()
    {
        TelemetryBatchBuffer<bool> buffer =
            new(
                batchCapacity: 1,
                readingsPerBatch: 2);

        Assert.True(buffer.TryAdd(true));
        Assert.True(buffer.TryAdd(false));
        Assert.False(buffer.TryAdd(true));

        Assert.True(buffer.IsFull);
        Assert.Equal(2, buffer.Count);

        Assert.Throws<InvalidOperationException>(
            () => buffer.Add(true));
    }
}