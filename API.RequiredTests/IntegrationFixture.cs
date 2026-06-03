using System.Text;
using System.Text.Json;
using API.Domains;
using API.Domains.Workflows.Projections;
using API.Infrastructure.Ingestion.DeadLetters;
using Marten;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;

namespace Tests;

/// <summary>
/// Shared host for all integration tests. Boots the API in-process against the
/// Postgres + RabbitMQ brought up via docker.
/// </summary>
public sealed class IntegrationFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public string Exchange { get; } = $"xflow.insights.test-{Guid.NewGuid()}";
    public string Queue { get; } = $"insights.events.test-{Guid.NewGuid()}";

    private IConnection? _connection;
    private IChannel? _channel;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RabbitMq:HostName"] = "localhost",
                ["RabbitMq:Port"] = "5672",
                ["RabbitMq:VirtualHost"] = "/",
                ["RabbitMq:UserName"] = "guest",
                ["RabbitMq:Password"] = "guest",
                ["RabbitMq:ClientProvidedName"] = $"insights-test-{Guid.NewGuid()}",
                ["RabbitMq:Exchange"] = Exchange,
                ["RabbitMq:Queue"] = Queue,
                ["RabbitMq:PrefetchCount"] = "50",
                ["Database:PostgreSQLConnectionString"] =
                    "Host=localhost;Port=5432;Database=event_sourcing;Username=es_admin;Password=StrongForNow1;Maximum Pool Size=40"
            });
        });
    }

    public async Task InitializeAsync()
    {
        _ = CreateClient();

        var factory = new ConnectionFactory
        {
            HostName = "localhost",
            Port = 5672,
            UserName = "guest",
            Password = "guest",
            ClientProvidedName = "insights-test-producer"
        };

        _connection = await factory.CreateConnectionAsync();
        _channel = await _connection.CreateChannelAsync();

        await _channel.ExchangeDeclareAsync(
            exchange: Exchange,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false);

        await Task.Delay(TimeSpan.FromSeconds(2));
    }

    public new async Task DisposeAsync()
    {
        if (_channel is not null)
        {
            await _channel.DisposeAsync();
        }
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }
        await base.DisposeAsync();
    }

    public IDocumentStore Store => Services.GetRequiredService<IDocumentStore>();

    public async Task ProduceAsync(string streamId, object payload)
    {
        var body = JsonSerializer.SerializeToUtf8Bytes(payload, JsonOptions);

        using var doc = JsonDocument.Parse(body);
        var routingKey = doc.RootElement.TryGetProperty("eventType", out var et) && et.GetString() is { Length: > 0 } v
            ? v
            : streamId;

        var properties = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent,
            MessageId = Guid.NewGuid().ToString()
        };

        await _channel!.BasicPublishAsync(
            exchange: Exchange,
            routingKey: routingKey,
            mandatory: false,
            basicProperties: properties,
            body: body);
    }

    public async Task<WorkflowProjectionDetails> WaitForProjectionAsync(
        string streamId,
        Func<WorkflowProjectionDetails, bool>? predicate = null,
        TimeSpan? timeout = null)
    {
        timeout ??= TimeSpan.FromSeconds(20);
        var deadline = DateTime.UtcNow + timeout.Value;

        while (DateTime.UtcNow < deadline)
        {
            await using var session = Store.QuerySession();
            var doc = await session.LoadAsync<WorkflowProjectionDetails>(streamId);
            if (doc is not null && (predicate is null || predicate(doc)))
                return doc;
            await Task.Delay(200);
        }

        throw new TimeoutException($"Projection for stream '{streamId}' did not reach the expected state within {timeout}.");
    }

    public async Task<DeadLetterEvent> WaitForDeadLetterAsync(
        Func<DeadLetterEvent, bool> predicate,
        TimeSpan? timeout = null)
    {
        timeout ??= TimeSpan.FromSeconds(20);
        var deadline = DateTime.UtcNow + timeout.Value;

        while (DateTime.UtcNow < deadline)
        {
            await using var session = Store.QuerySession();
            var items = await session.Query<DeadLetterEvent>().ToListAsync();
            var match = items.FirstOrDefault(predicate);
            if (match is not null)
                return match;
            await Task.Delay(200);
        }

        throw new TimeoutException($"No matching dead-lettered event appeared within {timeout}.");
    }
}

[CollectionDefinition("Integration")]
public sealed class IntegrationCollection : ICollectionFixture<IntegrationFixture> { }
