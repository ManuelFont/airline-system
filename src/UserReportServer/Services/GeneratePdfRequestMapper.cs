using System.Globalization;
using UserReportContracts;
using UserReportServer.Models;

namespace UserReportServer.Services;

internal static class GeneratePdfRequestMapper
{
    private const decimal InvalidPrice = -999;

    public static IReadOnlyList<UserReportModel> Map(GeneratePdfRequest request)
    {
        var flights = request.Flights.Select(MapFlight).ToList();

        return request.Users
            .Select(user => new UserReportModel(user.Username, flights))
            .ToList();
    }

    private static FlightReportModel MapFlight(FlightMessage flight)
    {
        var tickets = flight.Tickets.Select(MapTicket);
        var image = MapImage(flight.Image);

        return new FlightReportModel(
            flight.Code,
            flight.Origin,
            flight.Destination,
            flight.OwnerUsername,
            flight.Date,
            flight.Seats,
            flight.AvailableSeats,
            ParsePrice(flight.Price),
            flight.Duration,
            flight.IsCancelled,
            flight.PassengerUsernames,
            tickets,
            image);
    }

    private static TicketReportModel MapTicket(TicketMessage ticket)
    {
        return new TicketReportModel(
            ticket.Id,
            ticket.BuyerUsername,
            ParsePrice(ticket.PriceAtPurchase),
            ticket.IsCanceled);
    }

    private static FlightImageReportModel? MapImage(FlightImageMessage? image)
    {
        if (image == null || image.Content.IsEmpty)
            return null;

        return new FlightImageReportModel(image.FileName, image.Content.ToByteArray());
    }

    private static decimal ParsePrice(string price)
    {
        if (decimal.TryParse(
                price,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var parsedPrice))
            return parsedPrice;

        return InvalidPrice;
    }
}
