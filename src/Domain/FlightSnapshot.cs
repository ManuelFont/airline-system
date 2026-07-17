namespace Domain;

public sealed class FlightSnapshot
{
    public string Code { get; }
    public string Origin { get; }
    public string Destination { get; }
    public string OwnerUsername { get; }
    public DateTime Date { get; }
    public int Seats { get; }
    public int AvailableSeats { get; }
    public decimal Price { get; }
    public TimeSpan Duration { get; }
    public bool IsCancelled { get; }
    public IReadOnlyList<string> PassengerUsernames { get; }
    public IReadOnlyList<TicketSnapshot> Tickets { get; }
    public string? ImagePath { get; }

    internal FlightSnapshot(Flight flight)
    {
        Code = flight.Code;
        Origin = flight.Origin;
        Destination = flight.Destination;
        OwnerUsername = flight.Owner.Username;
        Date = flight.Date;
        Seats = flight.Seats;
        AvailableSeats = flight.AvailableSeats;
        Price = flight.Price;
        Duration = flight.Duration;
        IsCancelled = flight.IsCancelled;
        PassengerUsernames = Array.AsReadOnly(
            flight.Passengers.Select(passenger => passenger.Username).ToArray());
        Tickets = Array.AsReadOnly(
            flight.Tickets.Select(ticket => new TicketSnapshot(ticket)).ToArray());
        ImagePath = flight.ImagePath;
    }
}
