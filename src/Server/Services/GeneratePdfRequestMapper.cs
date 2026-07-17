using System.Globalization;
using Domain;
using Google.Protobuf;
using UserReportContracts;

namespace Server.Services;

internal static class GeneratePdfRequestMapper
{
    public static GeneratePdfRequest Map(IEnumerable<User> users, IEnumerable<Flight> flights)
    {
        var request = new GeneratePdfRequest();
        request.Users.Add(users.Select(MapUser));
        request.Flights.Add(flights.Select(MapFlight));
        return request;
    }

    private static UserMessage MapUser(User user)
    {
        return new UserMessage
        {
            Username = user.Username
        };
    }

    private static FlightMessage MapFlight(Flight flight)
    {
        var flightSnapshot = flight.GetSnapshot();
        var mappedFlight = new FlightMessage
        {
            Code = flightSnapshot.Code,
            Origin = flightSnapshot.Origin,
            Destination = flightSnapshot.Destination,
            OwnerUsername = flightSnapshot.OwnerUsername,
            Date = flightSnapshot.Date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
            Seats = flightSnapshot.Seats,
            AvailableSeats = flightSnapshot.AvailableSeats,
            Price = flightSnapshot.Price.ToString(CultureInfo.InvariantCulture),
            Duration = flightSnapshot.Duration.ToString("hh\\:mm", CultureInfo.InvariantCulture),
            IsCancelled = flightSnapshot.IsCancelled
        };

        mappedFlight.PassengerUsernames.Add(flightSnapshot.PassengerUsernames);
        mappedFlight.Tickets.Add(flightSnapshot.Tickets.Select(MapTicket));

        if (flightSnapshot.ImagePath != null)
            mappedFlight.Image = MapImage(flightSnapshot.ImagePath);

        return mappedFlight;
    }

    private static TicketMessage MapTicket(TicketSnapshot ticket)
    {
        return new TicketMessage
        {
            Id = ticket.Id.ToString(),
            BuyerUsername = ticket.BuyerUsername,
            PriceAtPurchase = ticket.PriceAtPurchase.ToString(CultureInfo.InvariantCulture),
            IsCanceled = ticket.IsCanceled
        };
    }

    private static FlightImageMessage MapImage(string imagePath)
    {
        return new FlightImageMessage
        {
            FileName = Path.GetFileName(imagePath),
            Content = ByteString.CopyFrom(File.ReadAllBytes(imagePath))
        };
    }
}
