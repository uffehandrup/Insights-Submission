namespace API.LoadTests.Domains;

public abstract record DomainEvent
{
    public required string EventId { get; init; }
    public required string eventType { get; init; }
    public required string StreamId { get; init; }
    public required int WorkflowId { get; init; }
    public required DateTime OccurredAt { get; init; }
}
