namespace Tests;

// FR-004: Read-Model Projection.
// Sequential ingestion of a workflow lifecycle produces the correct aggregated state.
[Collection("Integration")]
public class ProjectionLifecycleTests
{
    private readonly IntegrationFixture _fixture;

    public ProjectionLifecycleTests(IntegrationFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Started_step_completed_workflow_yields_completed_projection()
    {
        var streamId = $"stream-{Guid.NewGuid():N}";
        var workflowId = Random.Shared.Next(100_000, 999_999);
        var startedAt = DateTime.UtcNow;

        await _fixture.ProduceAsync(streamId, new
        {
            eventId = Guid.NewGuid().ToString(),
            eventType = "workflow.started",
            streamId,
            workflowId,
            workflowName = "Lifecycle Test",
            occurredAt = startedAt
        });
        await _fixture.ProduceAsync(streamId, new
        {
            eventId = Guid.NewGuid().ToString(),
            eventType = "workflow.step.completed",
            streamId,
            workflowId,
            occurredAt = startedAt.AddSeconds(1)
        });
        await _fixture.ProduceAsync(streamId, new
        {
            eventId = Guid.NewGuid().ToString(),
            eventType = "workflow.completed",
            streamId,
            workflowId,
            occurredAt = startedAt.AddSeconds(2)
        });

        var projection = await _fixture.WaitForProjectionAsync(
            streamId,
            p => p.CurrentStatus == "Completed");

        Assert.Equal("Completed", projection.CurrentStatus);
        Assert.Equal(2, projection.StepNumber);
        Assert.Equal(3, projection.TotalEventsProcessed);
        Assert.NotNull(projection.CompletedAt);
    }
}
