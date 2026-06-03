namespace API.Infrastructure.Metrics;

public static class Endpoint
{
    public static IEndpointRouteBuilder MapIngestionMetricsEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/metrics/ingestion", (IngestionRateTracker tracker) =>
        {
            var now = DateTime.UtcNow;
            var total = tracker.TotalEvents;
            var uptime = now - tracker.StartedAt;
            var avg = uptime.TotalSeconds > 0 ? total / uptime.TotalSeconds : 0d;

            return Results.Ok(new
            {
                startedAt = tracker.StartedAt,
                now,
                uptimeSeconds = uptime.TotalSeconds,
                totalEvents = total,
                averageEventsPerSecond = Math.Round(avg, 2)
            });
        })
        .WithName("GetIngestionMetrics")
        .WithTags("Metrics");

        return app;
    }
}
