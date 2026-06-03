using API.Domains.Workflows.Projections;
using Marten;

namespace API.Domains.Workflows.Queries.GetWorkflowDetails;

public static class Endpoint
{
    public static RouteGroupBuilder MapGetWorkflowDetailsEndpoint(this RouteGroupBuilder group)
    {
        group.MapGet("/{workflowId:int}/dashboard", async (int workflowId, IQuerySession session, CancellationToken cancellationToken) =>
        {
            var details = await session.Query<WorkflowProjectionDetails>()
                .SingleOrDefaultAsync(x => x.WorkflowId == workflowId, cancellationToken);

            return details is null
                ? Results.NotFound()
                : Results.Ok(details);
        })
        .WithName("GetWorkflowDetails")
        .WithTags("Workflows")
        .Produces<WorkflowProjectionDetails>()
        .Produces(StatusCodes.Status404NotFound);

        return group;
    }
}
