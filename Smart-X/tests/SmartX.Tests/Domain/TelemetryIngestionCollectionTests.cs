using SmartX.Domain.Enums;
using SmartX.Domain.Telemetry;
using Xunit;

namespace SmartX.Tests.Domain;

/// <summary>
/// Verifies the custom telemetry ingestion collection.
/// </summary>
public sealed class TelemetryIngestionCollectionTests
{
    [Fact]
    public void AddMethods_TrackAcceptedFlaggedAndRejectedStates()
    {
        TelemetryIngestionCollection<string> collection =
            new(capacity: 10);

        collection.AddAccepted("packet-1");

        collection.AddFlagged(
            "packet-2",
            "Temperature spike detected.");

        collection.AddRejected(
            "packet-3",
            "The telemetry payload was invalid.");

        Assert.Equal(3, collection.Count);

        Assert.Equal(
            1,
            collection.CountByState(
                TelemetryIngestionState.Accepted));

        Assert.Equal(
            1,
            collection.CountByState(
                TelemetryIngestionState.Flagged));

        Assert.Equal(
            1,
            collection.CountByState(
                TelemetryIngestionState.Rejected));
    }

    [Fact]
    public void TryGetLatest_ReturnsMostRecentIngestionRecord()
    {
        TelemetryIngestionCollection<string> collection =
            new(capacity: 10);

        collection.AddAccepted("packet-1");
        collection.AddAccepted("packet-2");

        collection.AddFlagged(
            "packet-3",
            "Reading is outside the normal trend.");

        bool wasFound =
            collection.TryGetLatest(
                out TelemetryIngestionRecord<string>? latestRecord);

        Assert.True(wasFound);
        Assert.NotNull(latestRecord);
        Assert.Equal("packet-3", latestRecord!.Item);

        Assert.Equal(
            TelemetryIngestionState.Flagged,
            latestRecord.State);
    }

    [Fact]
    public void Add_WhenCapacityIsReached_TrimsOldestRecord()
    {
        TelemetryIngestionCollection<string> collection =
            new(capacity: 3);

        collection.AddAccepted("packet-1");
        collection.AddAccepted("packet-2");
        collection.AddAccepted("packet-3");
        collection.AddAccepted("packet-4");

        IReadOnlyList<TelemetryIngestionRecord<string>> snapshot =
            collection.CreateSnapshot();

        Assert.Equal(3, collection.Count);

        Assert.DoesNotContain(
            snapshot,
            record => record.Item == "packet-1");

        Assert.Contains(
            snapshot,
            record => record.Item == "packet-4");
    }
}