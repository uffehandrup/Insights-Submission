using API.Domains.Workflows.Projections;
using Marten;

namespace API.Domains.Workflows.Queries.GetWorkflowDashboard;

public static class Endpoint
{
    public static RouteGroupBuilder MapGetWorkflowDashboardEndpoint(this RouteGroupBuilder group)
    {
        group.MapGet("/dashboard", async (IQuerySession session, CancellationToken cancellationToken) =>
        {
            var workflows = await session.Query<WorkflowProjectionDetails>().ToListAsync(cancellationToken);
            var metrics = WorkflowMetricsCalculator.Calculate(workflows);
            return Results.Ok(metrics);
        })
        .WithName("GetWorkflowDashboardMetrics")
        .WithTags("Workflows")
        .Produces<WorkflowDashboardMetrics>();

        return group;
    }
}
