using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace FlightReportQueue;

public sealed class FlightReportPublisher : IDisposable
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private readonly SemaphoreSlim _publishSemaphore = new SemaphoreSlim(1, 1);

    private FlightReportPublisher(IConnection connection, IChannel channel)
    {
        _connection = connection;
        _channel = channel;
    }

    public static async Task<FlightReportPublisher> CreateAsync()
    {
        var factory = RabbitMqSettings.CreateConnectionFactory();
        var connection = await factory.CreateConnectionAsync();
        var channel = await connection.CreateChannelAsync();

        await channel.QueueDeclareAsync(
            queue: RabbitMqSettings.FlightReportQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object?> { { "x-queue-type", "quorum" } });

        return new FlightReportPublisher(connection, channel);
    }

    public async Task PublishAsync(FlightCancellationReportRequest request)
    {
        await _publishSemaphore.WaitAsync();

        try
        {
            var message = JsonSerializer.Serialize(request);
            var body = Encoding.UTF8.GetBytes(message);
            var properties = new BasicProperties
            {
                Persistent = true
            };

            await _channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: RabbitMqSettings.FlightReportQueueName,
                mandatory: true,
                basicProperties: properties,
                body: body);
        }
        finally
        {
            _publishSemaphore.Release();
        }
    }

    public void Dispose()
    {
        _publishSemaphore.Dispose();
        _channel.Dispose();
        _connection.Dispose();
    }
}
