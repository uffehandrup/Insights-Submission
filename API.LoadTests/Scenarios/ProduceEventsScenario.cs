using System.Collections.Concurrent;
using API.LoadTests.Domains.Workflows;
using API.LoadTests.Infrastructure.RabbitMq;
using NBomber.CSharp;
using NBomber.Contracts;

namespace API.LoadTests.Scenarios;

public static class ProduceEventsScenario
{
    public static ScenarioProps Build(
        RabbitMqEventProducer producer,
        ConcurrentBag<int> workflowIdRegistry,
        int tenantCount,
        LoadProfile profile)
    {
        return Scenario.Create("produce_events", async ctx =>
        {
            var streamId = WorkflowEventFactory.NewStreamId(tenantCount);
            var workflowId = Random.Shared.Next(1, 100_000);
            var events = WorkflowEventFactory.CreateLifecycle(streamId, workflowId);

            workflowIdRegistry.Add(workflowId);

            foreach (var evt in events)
            {
                var step = await Step.Run("produce_event", ctx, async () =>
                {
                    try
                    {
                        await producer.ProduceEventAsync(streamId, evt);
                        return Response.Ok();
                    }
                    catch (Exception ex)
                    {
                        return Response.Fail(message: ex.Message);
                    }
                });

                if (step.IsError) return step;
            }

            return Response.Ok();
        })
        .WithoutWarmUp()
        .WithLoadSimulations(
            Simulation.RampingInject(rate: profile.WarmupRps,
                interval: TimeSpan.FromSeconds(1),
                during: TimeSpan.FromSeconds(profile.WarmupSeconds)),
            Simulation.RampingInject(rate: profile.Ramp1Rps,
                interval: TimeSpan.FromSeconds(1),
                during: TimeSpan.FromSeconds(profile.Ramp1Seconds)),
            Simulation.RampingInject(rate: profile.Ramp2Rps,
                interval: TimeSpan.FromSeconds(1),
                during: TimeSpan.FromSeconds(profile.Ramp2Seconds)),
            Simulation.Inject(rate: profile.HoldRps,
                interval: TimeSpan.FromSeconds(1),
                during: TimeSpan.FromSeconds(profile.HoldSeconds))
        );
    }
}
