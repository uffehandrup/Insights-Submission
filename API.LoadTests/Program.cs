using System.Collections.Concurrent;
using API.LoadTests.Infrastructure.Http;
using API.LoadTests.Infrastructure.RabbitMq;
using API.LoadTests.Scenarios;
using Microsoft.Extensions.Configuration;
using NBomber.Contracts.Stats;
using NBomber.CSharp;

var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

var producerSettings = config.GetSection("RabbitMq").Get<RabbitMqProducerSettings>()
    ?? throw new InvalidOperationException("RabbitMq section missing from appsettings.json");
var apiBaseUrl = config["ApiBaseUrl"]
    ?? throw new InvalidOperationException("ApiBaseUrl missing from appsettings.json");
var tenantCount = config.GetValue<int?>("TenantCount") ?? 5;
var profile = config.GetSection("LoadProfile").Get<LoadProfile>() ?? new LoadProfile();

await using var producer = new RabbitMqEventProducer(producerSettings);
await producer.InitializeAsync();

using var httpClient = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };
var queryClient = new QueryApiClient(httpClient);
var workflowIdRegistry = new ConcurrentBag<int>();

NBomberRunner
    .RegisterScenarios(
        ProduceEventsScenario.Build(producer, workflowIdRegistry, tenantCount, profile),
        QueryApiScenario.Build(queryClient, workflowIdRegistry, profile))
    .WithReportFolder("reports")
    .WithReportFormats(ReportFormat.Html, ReportFormat.Md)
    .Run();
