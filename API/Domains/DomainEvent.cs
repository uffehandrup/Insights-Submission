namespace API.Domains;

public abstract record DomainEvent
{
    public required string EventId { get; init; }
    public required string StreamId { get; init; }
    public DateTime OccurredAt { get; init; }
    public DateTime ReceivedAt { get; init; } = DateTime.UtcNow;
}

[AttributeUsage(AttributeTargets.Class)]
public sealed class EventTypeAttribute(string discriminator) : Attribute
{
    public string Discriminator { get; } = discriminator;
}
