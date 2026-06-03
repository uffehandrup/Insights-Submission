namespace API.Infrastructure.Metrics;

/// <summary>
/// Singleton, thread-safe counter for events ingested by the RabbitMQ consumer.
/// Records the total since process start; rolling rate is derived by the
/// <see cref="IngestionRateSnapshotWriter"/> from periodic deltas.
/// </summary>
public sealed class IngestionRateTracker
{
    private long _totalEvents;

    public DateTime StartedAt { get; } = DateTime.UtcNow;

    public void Record() => Interlocked.Increment(ref _totalEvents);

    public long TotalEvents => Interlocked.Read(ref _totalEvents);
}
