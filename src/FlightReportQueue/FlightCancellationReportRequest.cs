namespace FlightReportQueue;

public sealed class FlightCancellationReportRequest
{
    public Guid ReportId { get; }
    public string FlightCode { get; }
    public string Origin { get; }
    public string Destination { get; }
    public DateTime FlightDate { get; }
    public int Seats { get; }
    public TimeSpan Duration { get; }
    public decimal Price { get; }
    public string FlightOwnerUsername { get; }
    public DateTime CancelledAt { get; }
    public string CancellationReason { get; }
    public string CancelledByUsername { get; }
    public string? WebhookUrl { get; }

    public FlightCancellationReportRequest(
        Guid reportId,
        string flightCode,
        string origin,
        string destination,
        DateTime flightDate,
        int seats,
        TimeSpan duration,
        decimal price,
        string flightOwnerUsername,
        DateTime cancelledAt,
        string cancellationReason,
        string cancelledByUsername,
        string? webhookUrl = null)
    {
        ReportId = reportId;
        FlightCode = flightCode;
        Origin = origin;
        Destination = destination;
        FlightDate = flightDate;
        Seats = seats;
        Duration = duration;
        Price = price;
        FlightOwnerUsername = flightOwnerUsername;
        CancelledAt = cancelledAt;
        CancellationReason = cancellationReason;
        CancelledByUsername = cancelledByUsername;
        WebhookUrl = webhookUrl;
    }
}
