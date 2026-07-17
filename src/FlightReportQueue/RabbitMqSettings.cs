using RabbitMQ.Client;

namespace FlightReportQueue;

internal static class RabbitMqSettings
{
    private const string HostNameKey = "RABBITMQ_HOST";
    private const string PortKey = "RABBITMQ_PORT";
    private const string UserNameKey = "RABBITMQ_USER";
    private const string PasswordKey = "RABBITMQ_PASSWORD";

    public static ConnectionFactory CreateConnectionFactory()
    {
        return new ConnectionFactory
        {
            HostName = Environment.GetEnvironmentVariable(HostNameKey) ?? "localhost",
            Port = int.Parse(Environment.GetEnvironmentVariable(PortKey) ?? "5672"),
            UserName = Environment.GetEnvironmentVariable(UserNameKey) ?? "guest",
            Password = Environment.GetEnvironmentVariable(PasswordKey) ?? "guest"
        };
    }

    public const string FlightReportQueueName = "flight-report-requests";
}
