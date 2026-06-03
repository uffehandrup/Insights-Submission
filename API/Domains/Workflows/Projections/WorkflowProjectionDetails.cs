namespace API.Domains.Workflows.Projections;

public class WorkflowProjectionDetails
{
    public required string Id { get; set; }
    public required int WorkflowId { get; set; }
    public string WorkflowName { get; set; } = "";
    public string CurrentStatus { get; set; } = "Running";
    public int StepNumber { get; set; } = 1; 
    public string Note { get; set; } = "";
    
    public DateTime StartedAt { get; set; }
    public DateTime LastUpdatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public int TotalEventsProcessed { get; set; }
}