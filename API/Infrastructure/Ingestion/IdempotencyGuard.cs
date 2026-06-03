using Marten;

namespace API.Infrastructure.Ingestion;

public sealed class IdempotencyGuard
{
    private readonly IDocumentSession _session;

    public IdempotencyGuard(IDocumentSession session)
    {
        _session = session;
    }
    
    public async Task<bool> HasBeenProcessedAsync(string eventId, CancellationToken cancellationToken)
    {
        // Rely purely on the DB for the check. It's safe and fast for PK lookups.
        var existing = await _session.LoadAsync<ProcessedEvent>(eventId, cancellationToken);
        return existing is not null;
    }
    
    public void MarkAsProcessed(string eventId)
    {
        // Use Insert() instead of Store(). 
        // If a concurrent request gets past the LoadAsync check, calling SaveChangesAsync() 
        // on this session will throw an exception because the ID already exists, preventing duplicate processing.
        _session.Insert(new ProcessedEvent { Id = eventId, ProcessedAt = DateTime.UtcNow });
    }
}

public sealed class ProcessedEvent
{
    // Marten automatically uses properties named 'Id' as the Primary Key
    public required string Id { get; set; } 
    public DateTime ProcessedAt { get; set; }
}