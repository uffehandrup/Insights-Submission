using API.Domains.Workflows;
using Marten.Events.Aggregation;

namespace API.Domains.Workflows.Projections;

public class WorkflowProjectionFunctions : SingleStreamProjection<WorkflowProjectionDetails, string> 
{
    public WorkflowProjectionDetails Create(WorkflowStartedDomainEvent @event)
    {
        return new WorkflowProjectionDetails
        {
            Id = @event.StreamId,
            WorkflowId = @event.WorkflowId,
            WorkflowName = @event.WorkflowName,
            CurrentStatus = "Running",
            StepNumber = 1,
            StartedAt = @event.OccurredAt,
            LastUpdatedAt = @event.OccurredAt,
            TotalEventsProcessed = 1
        };
    }
    
    public void Apply(WorkflowStepCompletedDomainEvent @event, WorkflowProjectionDetails current)
    {
        current.StepNumber++;
        current.TotalEventsProcessed++;
        current.LastUpdatedAt = @event.OccurredAt;
    }
    
    public void Apply(WorkflowCompletedDomainEvent @event, WorkflowProjectionDetails current)
    {
        current.CurrentStatus = "Completed";
        current.LastUpdatedAt = @event.OccurredAt;
        current.CompletedAt = @event.OccurredAt;
        current.TotalEventsProcessed++;
    }
    
    public void Apply(WorkflowFailedDomainEvent @event, WorkflowProjectionDetails current)
    {
        current.CurrentStatus = "Failed";
        current.LastUpdatedAt = @event.OccurredAt;
        current.Note = @event.Error;
        current.TotalEventsProcessed++;
    }
    
    public void Apply(WorkflowParkedDomainEvent @event, WorkflowProjectionDetails current)
    {
        current.CurrentStatus = "Parked";
        current.LastUpdatedAt = @event.OccurredAt;
        current.TotalEventsProcessed++;
    }
    
    public void Apply(WorkflowCancelledDomainEvent @event, WorkflowProjectionDetails current)
    {
        current.CurrentStatus = "Cancelled";
        current.LastUpdatedAt = @event.OccurredAt;
        current.TotalEventsProcessed++;
    }
    
    public void Apply(WorkflowResumedDomainEvent @event, WorkflowProjectionDetails current)
    {
        // When a workflow is resumed it is effectively running again
        current.CurrentStatus = "Running";
        current.LastUpdatedAt = @event.OccurredAt;
        current.TotalEventsProcessed++;
    }
    
    
    
}