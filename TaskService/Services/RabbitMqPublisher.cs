using System.Text;
using System.Text.Json;
using TaskApi.Models.Dtos;
using TaskApi.Services.Interfaces;
using RabbitMQ.Client;

namespace TaskApi.Services;

public class RabbitMqPublisher : ITaskEventPublisher, IAsyncDisposable
{
    private const string ExchangeName = "task.events";
    private const string RoutingKey = "task.completed";

    private readonly ConnectionFactory _factory;
    private IConnection? _connection;
    private IChannel? _channel;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public RabbitMqPublisher(IConfiguration configuration)
    {
        var host = configuration["RabbitMQ:Host"] ?? "localhost";
        _factory = new ConnectionFactory { HostName = host };
    }

    private async Task EnsureConnectedAsync(CancellationToken cancellationToken)
    {
        if (_channel is not null) return;

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_channel is not null) return;

            _connection = await _factory.CreateConnectionAsync(cancellationToken);
            _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

            // Гарантируем, что обменник существует
            await _channel.ExchangeDeclareAsync(
                exchange: ExchangeName,
                type: ExchangeType.Direct,
                durable: true,
                autoDelete: false,
                cancellationToken: cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task PublishTaskCompletedAsync(TaskCompletedEvent @event, CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(cancellationToken);

        var json = JsonSerializer.Serialize(@event);
        var body = Encoding.UTF8.GetBytes(json);

        var props = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent
        };

        await _channel!.BasicPublishAsync(
            exchange: ExchangeName,
            routingKey: RoutingKey,
            mandatory: false,
            basicProperties: props,
            body: body,
            cancellationToken: cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
        {
            await _channel.CloseAsync();
            await _channel.DisposeAsync();
        }

        if (_connection is not null)
        {
            await _connection.CloseAsync();
            await _connection.DisposeAsync();
        }

        _lock.Dispose();
    }
}