using SmartX.Domain.Enums;
using SmartX.Domain.Telemetry;

namespace SmartX.Api.Services;

/// <summary>
/// Maintains separate strongly typed telemetry histories to avoid
/// converting float, integer and Boolean values into object.
/// </summary>
public sealed class TelemetryHistoryStore
{
    private const int DefaultCapacityPerType = 1_000;

    private readonly TelemetryIngestionCollection<TelemetryPacket<float>>
        _floatingPointHistory;

    private readonly TelemetryIngestionCollection<TelemetryPacket<int>>
        _integerHistory;

    private readonly TelemetryIngestionCollection<TelemetryPacket<bool>>
        _booleanHistory;

    /// <summary>
    /// Creates a bounded telemetry history store.
    /// </summary>
    public TelemetryHistoryStore(
        int capacityPerType = DefaultCapacityPerType)
    {
        if (capacityPerType <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(capacityPerType),
                "Telemetry history capacity must be greater than zero.");
        }

        _floatingPointHistory =
            new TelemetryIngestionCollection<TelemetryPacket<float>>(
                capacityPerType);

        _integerHistory =
            new TelemetryIngestionCollection<TelemetryPacket<int>>(
                capacityPerType);

        _booleanHistory =
            new TelemetryIngestionCollection<TelemetryPacket<bool>>(
                capacityPerType);
    }

    /// <summary>
    /// Records a floating-point telemetry packet.
    /// </summary>
    public void Record(
        TelemetryPacket<float> packet,
        TelemetryIngestionState state,
        string? message = null)
    {
        RecordPacket(
            _floatingPointHistory,
            packet,
            state,
            message);
    }

    /// <summary>
    /// Records an integer telemetry packet.
    /// </summary>
    public void Record(
        TelemetryPacket<int> packet,
        TelemetryIngestionState state,
        string? message = null)
    {
        RecordPacket(
            _integerHistory,
            packet,
            state,
            message);
    }

    /// <summary>
    /// Records a Boolean telemetry packet.
    /// </summary>
    public void Record(
        TelemetryPacket<bool> packet,
        TelemetryIngestionState state,
        string? message = null)
    {
        RecordPacket(
            _booleanHistory,
            packet,
            state,
            message);
    }

    /// <summary>
    /// Returns a snapshot of floating-point telemetry history.
    /// </summary>
    public IReadOnlyList<
        TelemetryIngestionRecord<TelemetryPacket<float>>>
        GetFloatingPointHistory()
    {
        return _floatingPointHistory.CreateSnapshot();
    }

    /// <summary>
    /// Returns a snapshot of integer telemetry history.
    /// </summary>
    public IReadOnlyList<
        TelemetryIngestionRecord<TelemetryPacket<int>>>
        GetIntegerHistory()
    {
        return _integerHistory.CreateSnapshot();
    }

    /// <summary>
    /// Returns a snapshot of Boolean telemetry history.
    /// </summary>
    public IReadOnlyList<
        TelemetryIngestionRecord<TelemetryPacket<bool>>>
        GetBooleanHistory()
    {
        return _booleanHistory.CreateSnapshot();
    }

    /// <summary>
    /// Produces dashboard-ready ingestion counts.
    /// </summary>
    public TelemetryHistorySummary GetSummary()
    {
        int accepted =
            CountStateAcrossAllTypes(
                TelemetryIngestionState.Accepted);

        int flagged =
            CountStateAcrossAllTypes(
                TelemetryIngestionState.Flagged);

        int rejected =
            CountStateAcrossAllTypes(
                TelemetryIngestionState.Rejected);

        return new TelemetryHistorySummary(
            Total: _floatingPointHistory.Count +
                   _integerHistory.Count +
                   _booleanHistory.Count,
            Accepted: accepted,
            Flagged: flagged,
            Rejected: rejected,
            FloatingPoint: _floatingPointHistory.Count,
            Integer: _integerHistory.Count,
            Boolean: _booleanHistory.Count);
    }

    private int CountStateAcrossAllTypes(
        TelemetryIngestionState state)
    {
        return
            _floatingPointHistory.CountByState(state) +
            _integerHistory.CountByState(state) +
            _booleanHistory.CountByState(state);
    }

    private static void RecordPacket<T>(
        TelemetryIngestionCollection<TelemetryPacket<T>> history,
        TelemetryPacket<T> packet,
        TelemetryIngestionState state,
        string? message)
        where T : notnull
    {
        ArgumentNullException.ThrowIfNull(packet);

        switch (state)
        {
            case TelemetryIngestionState.Accepted:
                history.AddAccepted(packet);
                break;

            case TelemetryIngestionState.Flagged:
                history.AddFlagged(
                    packet,
                    message ??
                    "The telemetry reading requires investigation.");
                break;

            case TelemetryIngestionState.Rejected:
                history.AddRejected(
                    packet,
                    message ??
                    "The telemetry reading was rejected.");
                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(state),
                    state,
                    "The telemetry ingestion state is not supported.");
        }
    }
}

/// <summary>
/// Contains aggregated telemetry counts for the dashboard.
/// </summary>
public sealed record TelemetryHistorySummary(
    int Total,
    int Accepted,
    int Flagged,
    int Rejected,
    int FloatingPoint,
    int Integer,
    int Boolean);