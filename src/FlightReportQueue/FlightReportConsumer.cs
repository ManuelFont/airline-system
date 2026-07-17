using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace FlightReportQueue;

public sealed class FlightReportConsumer : IDisposable
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;

    private FlightReportConsumer(IConnection connection, IChannel channel)
    {
        _connection = connection;
        _channel = channel;
    }

    public static async Task<FlightReportConsumer> CreateAsync()
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

        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);

        return new FlightReportConsumer(connection, channel);
    }

    public async Task StartAsync(
        Func<FlightCancellationReportRequest, Task> handleRequestAsync)
    {
        // Creates an asynchronous consumer using the existing RabbitMQ channel.
        var consumer = new AsyncEventingBasicConsumer(_channel);

        // Registers the operation executed whenever RabbitMQ delivers a message.
        consumer.ReceivedAsync += async (_, eventArgs) =>
        {
            // Converts the message body from JSON into the shared request type.
            var body = eventArgs.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            var request = JsonSerializer.Deserialize<FlightCancellationReportRequest>(message)
                ?? throw new InvalidOperationException(
                    "The flight report request is invalid");

            Console.WriteLine(
                $"Received flight cancellation report request for id: {request.ReportId}");

            // Delegates report generation to the SERV-provided operation.
            await handleRequestAsync(request);

            // Confirms successful processing so RabbitMQ can remove the message.
            await _channel.BasicAckAsync(
                deliveryTag: eventArgs.DeliveryTag,
                multiple: false);
        };

        // Starts consuming from the shared queue.
        // Manual acknowledgement prevents removal before processing finishes.
        await _channel.BasicConsumeAsync(
            RabbitMqSettings.FlightReportQueueName,
            autoAck: false,
            consumer: consumer);
    }

    public void Dispose()
    {
        _channel.Dispose();
        _connection.Dispose();
    }
}
