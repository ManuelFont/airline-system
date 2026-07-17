using System.Text;
using System.Text.Json.Nodes;
using Domain;
using Protocol;

namespace Server.Services;

internal static class TicketService
{
    public static async Task<ProtocolMessage> BuyTicketResponseAsync(ProtocolMessage request, User user)
    {
        try
        {
            var code = Encoding.UTF8.GetString(request.Data);
            var flight = FlightService.FindFlight(code);

            if (flight == null)
                throw new ArgumentException("Flight with that code doesnt exist");

            var ticket = flight.BuyTicket(user);
            var ticketPurchasedEvent = CreateTicketPurchasedEvent(ticket);
            try
            {
                await TicketWebSocketConnectionService.BroadcastAsync(ticketPurchasedEvent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not broadcast ticket purchase: {ex.Message}");
            }
            return BuildResponse(CommandCode.BuyTicket, NewTicketResponse(ticket));
        }
        catch (ArgumentException ex)
        {
            return BuildResponse(CommandCode.Error, ex.Message);
        }
    }

    public static ProtocolMessage CancelTicketResponse(ProtocolMessage request, User user)
    {
        try
        {
            var (code, ticketId) = ParseCancelTicketValues(Encoding.UTF8.GetString(request.Data));
            var flight = FlightService.FindFlight(code);

            if (flight == null)
                throw new ArgumentException("Flight with that code doesnt exist");

            flight.CancelTicket(Guid.Parse(ticketId), user);
            return BuildResponse(CommandCode.CancelTicket, "ticket successfully cancelled!");
        }
        catch (ArgumentException ex)
        {
            return BuildResponse(CommandCode.Error, ex.Message);
        }
    }

    private static string NewTicketResponse(Ticket ticket)
    {
        var message = "Ticket Succesfully bought!\n";
        message += $"Flight: {ticket.Flight.Code}\n";
        message += $"Departure: {ticket.Flight.Date}\n";
        message += $"TicketCode: {ticket.Id}\n";
        return message;
    }

    private static JsonObject CreateTicketPurchasedEvent(Ticket ticket)
    {
        return new JsonObject
        {
            ["action"] = "ticketPurchased",
            ["flightCode"] = ticket.Flight.Code,
            ["buyer"] = ticket.User.Username
        };
    }

    private static (string code, string ticketId) ParseCancelTicketValues(string data)
    {
        var parts = data.Split('|');
        return (
            code: parts[0],
            ticketId: parts[1]
        );
    }

    private static ProtocolMessage BuildResponse(string code, string message)
        => new ProtocolMessage("RES", code, Encoding.UTF8.GetBytes(message));
}
