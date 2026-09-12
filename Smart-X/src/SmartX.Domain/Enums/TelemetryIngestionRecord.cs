using SmartX.Domain.Enums;

namespace SmartX.Domain.Telemetry;

/// <summary>
/// Records the result of processing a strongly typed telemetry item.
/// </summary>
/// <typeparam name="T">
/// The original telemetry item type.
/// </typeparam>
public sealed record TelemetryIngestionRecord<T>
    where T : notnull
{
    /// <summary>
    /// Creates a new telemetry ingestion record.
    /// </summary>
    public TelemetryIngestionRecord(
        T item,
        TelemetryIngestionState state,
        DateTimeOffset processedAtUtc,
        string? message = null)
    {
        if (!Enum.IsDefined(state))
        {
            throw new ArgumentOutOfRangeException(
                nameof(state),
                state,
                "The telemetry ingestion state is not supported.");
        }

        string? normalisedMessage =
            string.IsNullOrWhiteSpace(message)
                ? null
                : message.Trim();

        if (state != TelemetryIngestionState.Accepted &&
            normalisedMessage is null)
        {
            throw new ArgumentException(
                "Flagged and rejected telemetry requires an explanatory message.",
                nameof(message));
        }

        Item = item;
        State = state;
        ProcessedAtUtc = processedAtUtc;
        Message = normalisedMessage;
    }

    /// <summary>
    /// Gets the original strongly typed telemetry item.
    /// </summary>
    public T Item { get; }

    /// <summary>
    /// Gets the result of the ingestion attempt.
    /// </summary>
    public TelemetryIngestionState State { get; }

    /// <summary>
    /// Gets the UTC time at which the item was processed.
    /// </summary>
    public DateTimeOffset ProcessedAtUtc { get; }

    /// <summary>
    /// Gets an optional explanation for the ingestion result.
    /// </summary>
    public string? Message { get; }
}