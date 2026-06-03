namespace API.Domains.Workflows.Queries.GetWorkflowDashboard;

public class WorkflowDashboardMetrics
{
    public string Id { get; set; } = "dashboard-metrics";
    public int TotalWorkflowsCreated { get; set; }
    public int TotalWorkflowsCompleted { get; set; }
    public int TotalWorkflowsRunning { get; set; }
    public int TotalWorkflowsFailed { get; set; }
    public int TotalWorkflowsCancelled { get; set; }
    public int TotalWorkflowsParked { get; set; }

    public double AverageCompletionTimeMinutes { get; set; }
    public double AverageStepsPerWorkflow { get; set; }

    public DateTime LastUpdated { get; set; }

    public double TotalCompletionTimeMinutes { get; set; }
    public double TotalSteps { get; set; }
}
