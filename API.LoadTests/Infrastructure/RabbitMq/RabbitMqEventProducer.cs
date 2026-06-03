using System.Text.Json;
using RabbitMQ.Client;

namespace API.LoadTests.Infrastructure.RabbitMq;

public sealed class RabbitMqEventProducer : IAsyncDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly RabbitMqProducerSettings _settings;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private IConnection? _connection;
    private IChannel? _channel;

    public RabbitMqEventProducer(RabbitMqProducerSettings settings)
    {
        _settings = settings;
    }

    public async Task InitializeAsync()
    {
        var factory = new ConnectionFactory
        {
            HostName = _settings.HostName,
            Port = _settings.Port,
            VirtualHost = _settings.VirtualHost,
            UserName = _settings.UserName,
            Password = _settings.Password,
            ClientProvidedName = _settings.ClientProvidedName
        };

        _connection = await factory.CreateConnectionAsync();
        _channel = await _connection.CreateChannelAsync();

        await _channel.ExchangeDeclareAsync(
            exchange: _settings.Exchange,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false);
    }

    public async Task ProduceEventAsync(string streamId, object payload)
    {
        if (_channel is null)
        {
            throw new InvalidOperationException("Producer not initialized. Call InitializeAsync first.");
        }

        var body = JsonSerializer.SerializeToUtf8Bytes(payload, payload.GetType(), JsonOptions);

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

        await _lock.WaitAsync();
        try
        {
            await _channel.BasicPublishAsync(
                exchange: _settings.Exchange,
                routingKey: routingKey,
                mandatory: false,
                basicProperties: properties,
                body: body);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
        {
            await _channel.DisposeAsync();
        }
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }
        _lock.Dispose();
    }
}

public sealed class RabbitMqProducerSettings
{
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string VirtualHost { get; set; } = "/";
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string ClientProvidedName { get; set; } = "insights-loadtests";
    public string Exchange { get; set; } = "xflow.insights";
}
