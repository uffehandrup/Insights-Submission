using System.Collections.Concurrent;
using System.Net;
using API.LoadTests.Infrastructure.Http;
using NBomber.CSharp;
using NBomber.Contracts;

namespace API.LoadTests.Scenarios;

public static class QueryApiScenario
{
    public static ScenarioProps Build(
        QueryApiClient client,
        ConcurrentBag<int> workflowIdRegistry,
        LoadProfile profile)
    {
        return Scenario.Create("query_api", async ctx =>
        {
            var dashStep = await Step.Run("get_dashboard", ctx, async () =>
            {
                using var response = await client.GetDashboardAsync();
                return response.IsSuccessStatusCode
                    ? Response.Ok()
                    : Response.Fail(message: ((int)response.StatusCode).ToString());
            });

            if (workflowIdRegistry.TryPeek(out var workflowId))
            {
                var detailStep = await Step.Run("get_workflow_details", ctx, async () =>
                {
                    using var response = await client.GetWorkflowDetailsAsync(workflowId);

                    // 404 is acceptable: projection may not yet have materialised.
                    if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotFound)
                        return Response.Ok();

                    return Response.Fail(message: ((int)response.StatusCode).ToString());
                });

                if (detailStep.IsError) return detailStep;
            }

            return dashStep;
        })
        .WithoutWarmUp()
        .WithLoadSimulations(
            Simulation.Inject(rate: profile.QueryRps,
                interval: TimeSpan.FromSeconds(1),
                during: TimeSpan.FromSeconds(profile.TotalSeconds))
        );
    }
}
