using Marten;

namespace API.Infrastructure.Ingestion.DeadLetters;

public sealed class DeadLetterStore
{
    private readonly IDocumentStore _store;
    private readonly ILogger<DeadLetterStore> _logger;

    public DeadLetterStore(IDocumentStore store, ILogger<DeadLetterStore> logger)
    {
        _store = store;
        _logger = logger;
    }

    public async Task RecordAsync(
        string key,
        string payload,
        string? eventType,
        DeadLetterReason reason,
        string? error,
        CancellationToken cancellationToken)
    {
        await using var session = _store.LightweightSession();
        session.Store(new DeadLetterEvent
        {
            Key = key,
            Payload = payload,
            EventType = eventType,
            Reason = reason,
            Error = error,
            DeadLetteredAt = DateTime.UtcNow
        });
        await session.SaveChangesAsync(cancellationToken);

        _logger.LogWarning("Dead-lettered message. Key={Key}, Reason={Reason}, EventType={EventType}",
            key, reason, eventType);
    }
}