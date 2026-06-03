using System.Text;
using API.Domains;
using API.Infrastructure.Metrics;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace API.Infrastructure.Ingestion;

/// <summary>
/// Background service that consumes workflow domain events from RabbitMQ
/// and delegates them to the <see cref="EventDispatcher"/> for ingestion into Marten.
/// </summary>
public sealed class RabbitMqConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IngestionRateTracker _rateTracker;
    private readonly ILogger<RabbitMqConsumer> _logger;
    private readonly RabbitMqOptions _options;

    private IConnection? _connection;
    private IChannel? _channel;

    public RabbitMqConsumer(
        IConfiguration configuration,
        IServiceProvider serviceProvider,
        IngestionRateTracker rateTracker,
        ILogger<RabbitMqConsumer> logger)
    {
        _serviceProvider = serviceProvider;
        _rateTracker = rateTracker;
        _logger = logger;

        _options = configuration.GetSection("RabbitMq").Get<RabbitMqOptions>()
                   ?? throw new InvalidOperationException("RabbitMq is not configured");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            VirtualHost = _options.VirtualHost,
            UserName = _options.UserName,
            Password = _options.Password,
            ClientProvidedName = _options.ClientProvidedName
        };

        _connection = await factory.CreateConnectionAsync(stoppingToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await _channel.ExchangeDeclareAsync(
            exchange: _options.Exchange,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            cancellationToken: stoppingToken);

        await _channel.QueueDeclareAsync(
            queue: _options.Queue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken);

        foreach (var routingKey in EventTypeRegistry.Map.Keys)
        {
            await _channel.QueueBindAsync(
                queue: _options.Queue,
                exchange: _options.Exchange,
                routingKey: routingKey,
                cancellationToken: stoppingToken);
        }

        await _channel.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: _options.PrefetchCount,
            global: false,
            cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += OnMessageAsync;

        await _channel.BasicConsumeAsync(
            queue: _options.Queue,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        _logger.LogInformation(
            "RabbitMQ consumer started on queue {Queue} (exchange {Exchange}, {KeyCount} routing keys)",
            _options.Queue, _options.Exchange, EventTypeRegistry.Map.Count);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }

        _logger.LogInformation("RabbitMQ consumer stopped");
    }

    private async Task OnMessageAsync(object sender, BasicDeliverEventArgs args)
    {
        if (_channel is null)
        {
            return;
        }

        var key = args.BasicProperties.MessageId ?? args.RoutingKey;
        var payload = Encoding.UTF8.GetString(args.Body.Span);

        try
        {
            await using var scope = _serviceProvider.CreateAsyncScope();
            var dispatcher = scope.ServiceProvider.GetRequiredService<EventDispatcher>();

            await dispatcher.DispatchAsync(key, payload, args.CancellationToken);
            _rateTracker.Record();

            await _channel.BasicAckAsync(args.DeliveryTag, multiple: false, args.CancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Shutting down — leave message unacked so it redelivers on next start.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message. Key={Key}", key);
            await _channel.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: true, args.CancellationToken);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);

        if (_channel is not null)
        {
            await _channel.DisposeAsync();
        }
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }
    }
}

public sealed class RabbitMqOptions
{
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string VirtualHost { get; set; } = "/";
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string ClientProvidedName { get; set; } = "insights-consumer";
    public string Exchange { get; set; } = "xflow.insights";
    public string Queue { get; set; } = "insights.events";
    public ushort PrefetchCount { get; set; } = 50;
}
