namespace Tests;

// FR-001: Event Ingestion.
// Integration test verifying consumption of events produced by a mock producer.
[Collection("Integration")]
public class EventIngestionTests
{
    private readonly IntegrationFixture _fixture;

    public EventIngestionTests(IntegrationFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Started_event_is_consumed_and_projected()
    {
        var streamId = $"stream-{Guid.NewGuid():N}";
        var workflowId = Random.Shared.Next(100_000, 999_999);

        await _fixture.ProduceAsync(streamId, new
        {
            eventId = Guid.NewGuid().ToString(),
            eventType = "workflow.started",
            streamId,
            workflowId,
            workflowName = "Order Processing",
            occurredAt = DateTime.UtcNow
        });

        var projection = await _fixture.WaitForProjectionAsync(streamId);

        Assert.Equal(workflowId, projection.WorkflowId);
        Assert.Equal("Order Processing", projection.WorkflowName);
        Assert.Equal("Running", projection.CurrentStatus);
        Assert.Equal(1, projection.TotalEventsProcessed);
    }
}
