namespace Tests;

// FR-005: Multi-Tenant Isolation.
// Events from different tenant prefixes must land on separate streams and not bleed
// into each other's projections.
[Collection("Integration")]
public class MultiTenantIsolationTests
{
    private readonly IntegrationFixture _fixture;

    public MultiTenantIsolationTests(IntegrationFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Tenants_with_overlapping_workflowId_remain_isolated_by_streamId()
    {
        var sharedSuffix = Guid.NewGuid().ToString("N");
        var tenantAStream = $"tenantA:{sharedSuffix}";
        var tenantBStream = $"tenantB:{sharedSuffix}";
        var workflowIdA = Random.Shared.Next(100_000, 999_999);
        var workflowIdB = Random.Shared.Next(100_000, 999_999);

        await _fixture.ProduceAsync(tenantAStream, new
        {
            eventId = Guid.NewGuid().ToString(),
            eventType = "workflow.started",
            streamId = tenantAStream,
            workflowId = workflowIdA,
            workflowName = "Tenant A workflow",
            occurredAt = DateTime.UtcNow
        });

        await _fixture.ProduceAsync(tenantBStream, new
        {
            eventId = Guid.NewGuid().ToString(),
            eventType = "workflow.started",
            streamId = tenantBStream,
            workflowId = workflowIdB,
            workflowName = "Tenant B workflow",
            occurredAt = DateTime.UtcNow
        });

        // Fail tenant B only. tenant A must stay running.
        await _fixture.ProduceAsync(tenantBStream, new
        {
            eventId = Guid.NewGuid().ToString(),
            eventType = "workflow.failed",
            streamId = tenantBStream,
            workflowId = workflowIdB,
            error = "tenant B failure",
            occurredAt = DateTime.UtcNow
        });

        var projA = await _fixture.WaitForProjectionAsync(tenantAStream);
        var projB = await _fixture.WaitForProjectionAsync(tenantBStream, p => p.CurrentStatus == "Failed");

        Assert.Equal("Tenant A workflow", projA.WorkflowName);
        Assert.Equal("Running",           projA.CurrentStatus);
        Assert.Equal(workflowIdA,         projA.WorkflowId);

        Assert.Equal("Tenant B workflow", projB.WorkflowName);
        Assert.Equal("Failed",            projB.CurrentStatus);
        Assert.Equal(workflowIdB,         projB.WorkflowId);
        Assert.Equal("tenant B failure",  projB.Note);
    }
}
