namespace UserReportServer.Models;

internal sealed class UserReportModel
{
    private readonly IReadOnlyList<FlightReportModel> _createdFlights;

    public string Username { get; }
    public IReadOnlyList<FlightReportModel> CreatedFlights => _createdFlights;
    public TicketStatsModel TicketStats { get; }
    public int FlightsWithImages { get; }
    public long TotalImageSize { get; }
    public long? AverageImageSize { get; }

    public UserReportModel(string username, IEnumerable<FlightReportModel> allFlights)
    {
        Username = username;

        var flightList = allFlights.ToList();
        _createdFlights = Array.AsReadOnly(
            flightList.Where(flight => flight.OwnerUsername == username).ToArray());

        var purchasedTickets = flightList
            .SelectMany(flight => flight.Tickets)
            .Where(ticket => ticket.BuyerUsername == username);
        TicketStats = new TicketStatsModel(purchasedTickets);

        var images = CreatedFlights
            .Where(flight => flight.Image != null && flight.Image.HasContent)
            .Select(flight => flight.Image!)
            .ToList();

        FlightsWithImages = images.Count;
        TotalImageSize = images.Sum(image => image.Size);

        if (FlightsWithImages > 0)
            AverageImageSize = TotalImageSize / FlightsWithImages;
    }
}
