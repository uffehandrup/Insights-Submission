namespace API.Domains.Workflows;

public abstract record WorkflowDomainEvent : DomainEvent
{
    public required int WorkflowId { get; init; }
}

[EventType("workflow.started")]
public sealed record WorkflowStartedDomainEvent(string WorkflowName) : WorkflowDomainEvent;

[EventType("workflow.step.completed")]
public sealed record WorkflowStepCompletedDomainEvent : WorkflowDomainEvent;

[EventType("workflow.completed")]
public sealed record WorkflowCompletedDomainEvent : WorkflowDomainEvent;

[EventType("workflow.failed")]
public sealed record WorkflowFailedDomainEvent(string Error) : WorkflowDomainEvent;

[EventType("workflow.parked")]
public sealed record WorkflowParkedDomainEvent : WorkflowDomainEvent;

[EventType("workflow.resumed")]
public sealed record WorkflowResumedDomainEvent : WorkflowDomainEvent;

[EventType("workflow.cancelled")]
public sealed record WorkflowCancelledDomainEvent : WorkflowDomainEvent;
