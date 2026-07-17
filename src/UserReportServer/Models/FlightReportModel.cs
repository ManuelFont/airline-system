using System.Text;

namespace UserReportServer.Models;

internal sealed class FlightReportModel
{
    private readonly IReadOnlyList<string> _passengerUsernames;
    private readonly IReadOnlyList<TicketReportModel> _tickets;

    public string Code { get; }
    public string Origin { get; }
    public string Destination { get; }
    public string OwnerUsername { get; }
    public string Date { get; }
    public int Seats { get; }
    public int AvailableSeats { get; }
    public decimal Price { get; }
    public string Duration { get; }
    public bool IsCancelled { get; }
    public IReadOnlyList<string> PassengerUsernames => _passengerUsernames;
    public IReadOnlyList<TicketReportModel> Tickets => _tickets;
    public FlightImageReportModel? Image { get; }

    public FlightReportModel(
        string code,
        string origin,
        string destination,
        string ownerUsername,
        string date,
        int seats,
        int availableSeats,
        decimal price,
        string duration,
        bool isCancelled,
        IEnumerable<string> passengerUsernames,
        IEnumerable<TicketReportModel> tickets,
        FlightImageReportModel? image)
    {
        Code = code;
        Origin = origin;
        Destination = destination;
        OwnerUsername = ownerUsername;
        Date = date;
        Seats = seats;
        AvailableSeats = availableSeats;
        Price = price;
        Duration = duration;
        IsCancelled = isCancelled;
        _passengerUsernames = Array.AsReadOnly(passengerUsernames.ToArray());
        _tickets = Array.AsReadOnly(tickets.ToArray());
        Image = image;
    }

    public string ToDisplayString()
    {
        var ticketStrings = Tickets.Select(ticket => ticket.IsCanceled
            ? $"{ticket.Id} - {ticket.BuyerUsername}(cancelled)"
            : $"{ticket.Id} - {ticket.BuyerUsername}");

        var display = new StringBuilder();
        display.AppendLine($"Code: {Code}");
        display.AppendLine($"Origin: {Origin}");
        display.AppendLine($"Destination: {Destination}");
        display.AppendLine($"Date: {Date}");
        display.AppendLine($"Seats: {Seats}");
        display.AppendLine($"Available Seats: {AvailableSeats}");
        display.AppendLine($"Duration: {Duration}");
        display.AppendLine($"Price: {Price}");
        display.AppendLine($"Owner: {OwnerUsername}");
        display.AppendLine($"Cancelled: {IsCancelled}");
        display.AppendLine($"Passengers: {string.Join(", ", PassengerUsernames)}");
        display.AppendLine($"Tickets: {string.Join(", ", ticketStrings)}");
        display.Append($"Image: {Image?.FileName ?? "None"}");
        return display.ToString();
    }
}
