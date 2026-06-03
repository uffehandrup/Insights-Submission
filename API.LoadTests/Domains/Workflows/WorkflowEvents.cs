namespace API.LoadTests.Domains.Workflows;

public sealed record WorkflowStartedEvent : DomainEvent
{
    public required string WorkflowName { get; init; }
}

public sealed record WorkflowStepCompletedEvent : DomainEvent;

public sealed record WorkflowCompletedEvent : DomainEvent;

public sealed record WorkflowFailedEvent : DomainEvent
{
    public required string Error { get; init; }
}

public sealed record WorkflowParkedEvent : DomainEvent;

public sealed record WorkflowResumedEvent : DomainEvent;

public sealed record WorkflowCancelledEvent : DomainEvent;
