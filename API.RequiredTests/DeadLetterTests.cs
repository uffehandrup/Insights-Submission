using API.Infrastructure.Ingestion.DeadLetters;

namespace Tests;

// Dead Letter Queue: messages that can never be ingested are preserved for review
// instead of being silently dropped.
[Collection("Integration")]
public class DeadLetterTests
{
    private readonly IntegrationFixture _fixture;

    public DeadLetterTests(IntegrationFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Undeserializable_payload_is_dead_lettered()
    {
        var streamId = $"stream-{Guid.NewGuid():N}";
        var marker = Guid.NewGuid().ToString("N");

        // eventType is valid (so the message routes to the queue) but workflowId is
        // the wrong type, so deserialization into the concrete event fails.
        await _fixture.ProduceAsync(streamId, new
        {
            eventId = Guid.NewGuid().ToString(),
            eventType = "workflow.started",
            streamId,
            workflowId = "not-an-integer",
            workflowName = marker,
            occurredAt = DateTime.UtcNow
        });

        var deadLetter = await _fixture.WaitForDeadLetterAsync(dl => dl.Payload.Contains(marker));

        Assert.Equal(DeadLetterReason.DeserializationFailed, deadLetter.Reason);
        Assert.Equal("workflow.started", deadLetter.EventType);
        Assert.Contains(marker, deadLetter.Payload);
        Assert.NotNull(deadLetter.Error);
    }
}
