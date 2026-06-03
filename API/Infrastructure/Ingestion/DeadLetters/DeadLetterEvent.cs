namespace API.Infrastructure.Ingestion.DeadLetters;

public enum DeadLetterReason
{
    MalformedJson,

    MissingEventType,

    UnknownEventType,

    DeserializationFailed,

    NullEvent
}


public sealed class DeadLetterEvent
{
    public Guid Id { get; set; }
    
    // RabbitMQ message key falls back to routing key
    public required string Key { get; set; }

    public required string Payload { get; set; }

    public string? EventType { get; set; }

    public required DeadLetterReason Reason { get; set; }

    public string? Error { get; set; }

    public DateTime DeadLetteredAt { get; set; }
}