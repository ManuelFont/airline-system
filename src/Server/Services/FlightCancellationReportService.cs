using Domain;
using FlightReportQueue;

namespace Server.Services;

internal static class FlightCancellationReportService
{
    private static FlightReportPublisher? _publisher;

    public static async Task InitializeAsync()
    {
        _publisher = await FlightReportPublisher.CreateAsync();
    }

    public static async Task<Guid> PublishAsync(
        Flight flight,
        FlightCancellation cancellation,
        string? webhookUrl)
    {
        var reportId = Guid.NewGuid();
        var request = new FlightCancellationReportRequest(
            reportId,
            flight.Code,
            flight.Origin,
            flight.Destination,
            flight.Date,
            flight.Seats,
            flight.Duration,
            flight.Price,
            flight.Owner.Username,
            cancellation.CancelledAt,
            cancellation.Reason,
            cancellation.CancelledBy.Username,
            webhookUrl);

        try
        {
            var publisher = _publisher
                ?? throw new InvalidOperationException("Flight report publisher is not initialized");
            await publisher.PublishAsync(request);
        }
        catch (Exception exception)
        {
            Console.WriteLine($"Failed to publish flight cancellation report {reportId}: {exception.Message}");
        }

        return reportId;
    }

    public static void Dispose()
    {
        _publisher?.Dispose();
        _publisher = null;
    }
}
