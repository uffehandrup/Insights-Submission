namespace API.LoadTests.Domains.Workflows;

public static class WorkflowEventFactory
{
    private const string EvtStarted = "workflow.started";
    private const string EvtStepCompleted = "workflow.step.completed";
    private const string EvtCompleted = "workflow.completed";
    private const string EvtFailed = "workflow.failed";
    private const string EvtParked = "workflow.parked";
    private const string EvtResumed = "workflow.resumed";
    private const string EvtCancelled = "workflow.cancelled";

    public static string NewStreamId(int tenantCount) =>
        $"tenant-{Random.Shared.Next(0, tenantCount)}:{Guid.NewGuid()}";

    public static List<DomainEvent> CreateLifecycle(string streamId, int workflowId)
    {
        var events = new List<DomainEvent>();
        var clock = DateTime.UtcNow;

        events.Add(new WorkflowStartedEvent
        {
            EventId = Guid.NewGuid().ToString(),
            eventType = EvtStarted,
            StreamId = streamId,
            WorkflowId = workflowId,
            OccurredAt = Advance(ref clock),
            WorkflowName = $"WorkflowType-{workflowId % 10}"
        });

        var stepCount = Random.Shared.Next(3, 8);
        for (var i = 0; i < stepCount; i++)
        {
            events.Add(new WorkflowStepCompletedEvent
            {
                EventId = Guid.NewGuid().ToString(),
                eventType = EvtStepCompleted,
                StreamId = streamId,
                WorkflowId = workflowId,
                OccurredAt = Advance(ref clock)
            });
        }

        var roll = Random.Shared.NextDouble();
        if (roll < 0.05)
        {
            events.Add(new WorkflowParkedEvent
            {
                EventId = Guid.NewGuid().ToString(),
                eventType = EvtParked,
                StreamId = streamId,
                WorkflowId = workflowId,
                OccurredAt = Advance(ref clock)
            });
            events.Add(new WorkflowResumedEvent
            {
                EventId = Guid.NewGuid().ToString(),
                eventType = EvtResumed,
                StreamId = streamId,
                WorkflowId = workflowId,
                OccurredAt = Advance(ref clock)
            });
            events.Add(new WorkflowCompletedEvent
            {
                EventId = Guid.NewGuid().ToString(),
                eventType = EvtCompleted,
                StreamId = streamId,
                WorkflowId = workflowId,
                OccurredAt = Advance(ref clock)
            });
        }
        else if (roll < 0.12)
        {
            events.Add(new WorkflowCancelledEvent
            {
                EventId = Guid.NewGuid().ToString(),
                eventType = EvtCancelled,
                StreamId = streamId,
                WorkflowId = workflowId,
                OccurredAt = Advance(ref clock)
            });
        }
        else if (roll < 0.22)
        {
            events.Add(new WorkflowFailedEvent
            {
                EventId = Guid.NewGuid().ToString(),
                eventType = EvtFailed,
                StreamId = streamId,
                WorkflowId = workflowId,
                OccurredAt = Advance(ref clock),
                Error = "Simulated load test failure"
            });
        }
        else
        {
            events.Add(new WorkflowCompletedEvent
            {
                EventId = Guid.NewGuid().ToString(),
                eventType = EvtCompleted,
                StreamId = streamId,
                WorkflowId = workflowId,
                OccurredAt = Advance(ref clock)
            });
        }

        return events;
    }

    private static DateTime Advance(ref DateTime clock)
    {
        clock = clock.AddSeconds(Random.Shared.Next(1, 6));
        return clock;
    }
}
