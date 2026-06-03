using API.Domains.Workflows.Projections;

namespace API.Domains.Workflows.Queries.GetWorkflowDashboard;

public static class WorkflowMetricsCalculator
{
    public static WorkflowDashboardMetrics Calculate(IReadOnlyList<WorkflowProjectionDetails> allWorkflows)
    {
        var metrics = new WorkflowDashboardMetrics
        {
            Id = "dashboard-metrics",
            TotalWorkflowsCreated = allWorkflows.Count,
            TotalWorkflowsCompleted = allWorkflows.Count(w => string.Equals(w.CurrentStatus, "Completed", StringComparison.OrdinalIgnoreCase)),
            TotalWorkflowsRunning = allWorkflows.Count(w => string.Equals(w.CurrentStatus, "Running", StringComparison.OrdinalIgnoreCase)),
            TotalWorkflowsFailed = allWorkflows.Count(w => string.Equals(w.CurrentStatus, "Failed", StringComparison.OrdinalIgnoreCase)),
            TotalWorkflowsCancelled = allWorkflows.Count(w => string.Equals(w.CurrentStatus, "Cancelled", StringComparison.OrdinalIgnoreCase)),
            TotalWorkflowsParked = allWorkflows.Count(w => string.Equals(w.CurrentStatus, "Parked", StringComparison.OrdinalIgnoreCase)),
            LastUpdated = DateTime.UtcNow
        };

        var completedWorkflows = allWorkflows.Where(w => w.CompletedAt != null).ToList();
        if (completedWorkflows.Count > 0)
        {
            metrics.TotalCompletionTimeMinutes = completedWorkflows
                .Sum(w => (w.CompletedAt!.Value - w.StartedAt).TotalMinutes);
            metrics.AverageCompletionTimeMinutes = metrics.TotalCompletionTimeMinutes / completedWorkflows.Count;
        }

        if (allWorkflows.Count > 0)
        {
            metrics.TotalSteps = allWorkflows.Sum(w => w.StepNumber);
            metrics.AverageStepsPerWorkflow = metrics.TotalSteps / allWorkflows.Count;
        }

        return metrics;
    }
}
