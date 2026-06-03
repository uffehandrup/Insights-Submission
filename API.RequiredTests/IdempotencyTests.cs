namespace Tests;

// FR-003: Idempotent Processing.
// Inject duplicate eventId payloads and assert the database rejects the duplicate
// without halting the consumer thread.
[Collection("Integration")]
public class IdempotencyTests
{
    private readonly IntegrationFixture _fixture;

    public IdempotencyTests(IntegrationFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Duplicate_eventId_is_discarded_and_consumer_continues()
    {
        var streamId = $"stream-{Guid.NewGuid():N}";
        var workflowId = Random.Shared.Next(100_000, 999_999);
        var duplicatedEventId = Guid.NewGuid().ToString();

        object started = new
        {
            eventId = duplicatedEventId,
            eventType = "workflow.started",
            streamId,
            workflowId,
            workflowName = "Idempotent Test",
            occurredAt = DateTime.UtcNow
        };

        await _fixture.ProduceAsync(streamId, started);
        await _fixture.ProduceAsync(streamId, started); // same eventId — reject
        await _fixture.WaitForProjectionAsync(streamId);

        // follow-up event on the same stream to prove the consumer is still alive.
        await _fixture.ProduceAsync(streamId, new
        {
            eventId = Guid.NewGuid().ToString(),
            eventType = "workflow.step.completed",
            streamId,
            workflowId,
            occurredAt = DateTime.UtcNow
        });

        var projection = await _fixture.WaitForProjectionAsync(streamId, p => p.StepNumber >= 2);

        // The started event was applied exactly once -> StepNumber starts at 1,
        // the follow-up step.completed bumps it to 2. TotalEventsProcessed == 2 and not 3
        Assert.Equal(2, projection.StepNumber);
        Assert.Equal(2, projection.TotalEventsProcessed);
        Assert.Equal("Running", projection.CurrentStatus);
    }
}
