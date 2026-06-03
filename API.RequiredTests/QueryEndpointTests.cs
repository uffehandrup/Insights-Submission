using System.Net;
using System.Net.Http.Json;
using API.Domains.Workflows.Projections;
using API.Domains.Workflows.Queries.GetWorkflowDashboard;

namespace Tests;

// FR-006: Query Interface.
// HTTP integration tests asserting valid 200 OK responses and correct schema
// formatting for single-workflow and aggregated dashboard queries.
[Collection("Integration")]
public class QueryEndpointTests
{
    private readonly IntegrationFixture _fixture;

    public QueryEndpointTests(IntegrationFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task GET_workflow_dashboard_returns_200_with_projection_schema()
    {
        var streamId = $"stream-{Guid.NewGuid():N}";
        var workflowId = Random.Shared.Next(100_000, 999_999);

        await _fixture.ProduceAsync(streamId, new
        {
            eventId = Guid.NewGuid().ToString(),
            eventType = "workflow.started",
            streamId,
            workflowId,
            workflowName = "Query Endpoint Seed",
            occurredAt = DateTime.UtcNow
        });
        await _fixture.WaitForProjectionAsync(streamId);

        var client = _fixture.CreateClient();
        var response = await client.GetAsync($"/api/workflows/{workflowId}/dashboard");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<WorkflowProjectionDetails>();
        Assert.NotNull(body);
        Assert.Equal(workflowId,             body.WorkflowId);
        Assert.Equal("Query Endpoint Seed",  body.WorkflowName);
        Assert.Equal("Running",              body.CurrentStatus);
    }

    [Fact]
    public async Task GET_aggregated_dashboard_returns_200_with_metrics_schema()
    {
        var client = _fixture.CreateClient();
        var response = await client.GetAsync("/api/workflows/dashboard");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var metrics = await response.Content.ReadFromJsonAsync<WorkflowDashboardMetrics>();
        Assert.NotNull(metrics);
        Assert.Equal("dashboard-metrics", metrics.Id);
        Assert.True(metrics.TotalWorkflowsCreated >= 0);
    }

    [Fact]
    public async Task GET_unknown_workflow_returns_404()
    {
        var client = _fixture.CreateClient();
        var response = await client.GetAsync($"/api/workflows/{int.MaxValue}/dashboard");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
    
    [Fact]
    public async Task GET_readiness_check_returns_200()
    {
        var client = _fixture.CreateClient();
        var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
    
    [Fact]
    public async Task GET_liveness_check_returns_200()
    {
        var client = _fixture.CreateClient();
        var response = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

}
