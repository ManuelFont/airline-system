using FlightReportQueue;
using FlightReportServer.Services;

try
{
    using var consumer = await FlightReportConsumer.CreateAsync();
    await consumer.StartAsync(FlightReportService.GenerateAsync);

    Console.WriteLine("Waiting for flight cancellation report requests. Press enter to exit.");
    Console.ReadLine();
}
catch (Exception exception)
{
    Console.WriteLine($"Error: {exception.Message}");
}
