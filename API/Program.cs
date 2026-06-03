using API;
using API.Domains;
using API.Domains.Workflows.Queries.GetWorkflowDashboard;
using API.Domains.Workflows.Queries.GetWorkflowDetails;
using API.Infrastructure.Ingestion.DeadLetters;
using API.Infrastructure.Metrics;

// Fail fast: validate the [EventType] registry at startup rather than on first message.
_ = EventTypeRegistry.Map;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddPersistence(builder.Configuration, builder.Environment.IsDevelopment());
builder.Services.AddIngestion();
builder.Services.AddReadinessAndLivenessChecks(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapHealthCheckEndpoints();

app.UseHttpsRedirection();

var workflows = app.MapGroup("/api/workflows");
workflows.MapGetWorkflowDashboardEndpoint();
workflows.MapGetWorkflowDetailsEndpoint();

app.MapIngestionMetricsEndpoint();
app.MapDeadLetterEndpoints();

app.Run();

public partial class Program;
