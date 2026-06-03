using System.Text.Json;
using System.Text.Json.Serialization;
using API.Domains;
using API.Infrastructure.Ingestion.DeadLetters;
using Marten;

namespace API.Infrastructure.Ingestion;

/// <summary>
/// Deserializes incoming JSON payloads into domain events and appends them
/// to the correct Marten event stream, guarded by <see cref="IdempotencyGuard"/>.
/// Messages that can never succeed (malformed, unknown, undeserializable) are
/// routed to the <see cref="DeadLetterStore"/> rather than silently dropped.
/// </summary>
public sealed class EventDispatcher
{
    private readonly IDocumentSession _session;
    private readonly IdempotencyGuard _idempotencyGuard;
    private readonly DeadLetterStore _deadLetters;
    private readonly ILogger<EventDispatcher> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public EventDispatcher(
        IDocumentSession session,
        IdempotencyGuard idempotencyGuard,
        DeadLetterStore deadLetters,
        ILogger<EventDispatcher> logger)
    {
        _session = session;
        _idempotencyGuard = idempotencyGuard;
        _deadLetters = deadLetters;
        _logger = logger;
    }

    /// <summary>
    /// Dispatches a raw message payload into Marten's event store.
    /// </summary>
    public async Task DispatchAsync(string key, string payload, CancellationToken cancellationToken)
    {
        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(payload);
        }
        catch (JsonException ex)
        {
            await _deadLetters.RecordAsync(key, payload, null, DeadLetterReason.MalformedJson, ex.Message, cancellationToken);
            return;
        }

        using (doc)
        {
            var root = doc.RootElement;

            var eventTypeName = root.ValueKind == JsonValueKind.Object
                                && root.TryGetProperty("eventType", out var eventTypeProp)
                ? eventTypeProp.GetString()
                : null;

            if (eventTypeName is null)
            {
                await _deadLetters.RecordAsync(key, payload, null, DeadLetterReason.MissingEventType, null, cancellationToken);
                return;
            }

            if (!EventTypeRegistry.Map.TryGetValue(eventTypeName, out var concreteType))
            {
                await _deadLetters.RecordAsync(key, payload, eventTypeName, DeadLetterReason.UnknownEventType, null, cancellationToken);
                return;
            }

            DomainEvent? domainEvent;
            try
            {
                domainEvent = (DomainEvent?)JsonSerializer.Deserialize(payload, concreteType, JsonOptions);
            }
            catch (JsonException ex)
            {
                await _deadLetters.RecordAsync(key, payload, eventTypeName, DeadLetterReason.DeserializationFailed, ex.Message, cancellationToken);
                return;
            }

            if (domainEvent is null)
            {
                await _deadLetters.RecordAsync(key, payload, eventTypeName, DeadLetterReason.NullEvent, null, cancellationToken);
                return;
            }

            if (await _idempotencyGuard.HasBeenProcessedAsync(domainEvent.EventId, cancellationToken))
            {
                _logger.LogDebug("Duplicate event {EventId} skipped", domainEvent.EventId);
                return;
            }

            _session.Events.Append(domainEvent.StreamId, domainEvent);
            _idempotencyGuard.MarkAsProcessed(domainEvent.EventId);

            await _session.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Appended {EventType} to stream {StreamId}", eventTypeName, domainEvent.StreamId);
        }
    }
}
