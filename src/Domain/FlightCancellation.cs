namespace Domain;

public sealed class FlightCancellation
{
    public DateTime CancelledAt { get; }
    public string Reason { get; }
    public User CancelledBy { get; }

    public FlightCancellation(DateTime cancelledAt, string reason, User cancelledBy)
    {
        CancelledAt = cancelledAt;
        Reason = reason;
        CancelledBy = cancelledBy;
    }
}
