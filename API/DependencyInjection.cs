using API.Infrastructure.Data;
using API.Infrastructure.Ingestion;
using API.Infrastructure.Ingestion.DeadLetters;
using API.Infrastructure.Metrics;
using JasperFx.Events.Daemon;
using Marten;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using RabbitMQ.Client;

namespace API;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration,
        bool isDevelopment)
    {
        var martenSetup = new MartenSetup(configuration);

        services.AddMarten(martenSetup.GetMartenOptions(isDevelopment))
            .AddAsyncDaemon(DaemonMode.Solo)
            .UseLightweightSessions();

        return services;
    }

    public static IServiceCollection AddIngestion(this IServiceCollection services)
    {
        services.AddScoped<EventDispatcher>();
        services.AddScoped<IdempotencyGuard>();
        services.AddSingleton<IngestionRateTracker>();
        services.AddHostedService<RabbitMqConsumer>();
        services.AddHostedService<IngestionRateSnapshotWriter>();
        services.AddScoped<DeadLetterStore>();

        return services;
    }

    // Two probes: "live" runs no checks (the process answering = alive), "ready" runs the
    // dependency checks tagged "ready" so traffic is only routed once Postgres and RabbitMQ are reachable.
    public static IServiceCollection AddReadinessAndLivenessChecks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var postgresConnectionString = configuration["Database:PostgreSQLConnectionString"]
            ?? throw new InvalidOperationException("Database connection string is not configured");

        // The RabbitMQ health check resolves an IConnection from DI.
        services.AddSingleton<IConnection>(_ =>
        {
            var options = configuration.GetSection("RabbitMq").Get<RabbitMqOptions>()
                ?? throw new InvalidOperationException("RabbitMq is not configured");

            var factory = new ConnectionFactory
            {
                HostName = options.HostName,
                Port = options.Port,
                VirtualHost = options.VirtualHost,
                UserName = options.UserName,
                Password = options.Password,
                ClientProvidedName = $"{options.ClientProvidedName}-healthcheck"
            };

            return factory.CreateConnectionAsync().GetAwaiter().GetResult();
        });

        services.AddHealthChecks()
            .AddNpgSql(postgresConnectionString, name: "postgresql", tags: ["ready"])
            .AddRabbitMQ(name: "rabbitmq", tags: ["ready"]); // Uses IConnection from DI

        return services;
    }

    public static WebApplication MapHealthCheckEndpoints(this WebApplication app)
    {
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false
        });

        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready")
        });

        return app;
    }
}
