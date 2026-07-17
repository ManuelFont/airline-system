using System.Text;
using Protocol;

namespace Client.Services;

internal static class FlightService
{
    public static ProtocolMessage CreateFlightRequest()
    {
        var dataBytes = AskBytesValues();
        var request = new ProtocolMessage("REQ", CommandCode.CreateFlight, dataBytes);
        return request;
    }

    public static ProtocolMessage EditFlightRequest()
    {
        var dataBytes = AskBytesValues();
        var request = new ProtocolMessage("REQ", CommandCode.ModifyFlight, dataBytes);
        return request;
    }

    public static ProtocolMessage GetFlightRequest()
    {
        var dataBytes = AskBytesCode();
        var request = new ProtocolMessage("REQ", CommandCode.GetFlight, dataBytes);
        return request;
    }
    
    public static ProtocolMessage FilterFlightsRequest()
    {
        var dataBytes = AskBytesFilters();
        var request = new ProtocolMessage("REQ", CommandCode.ListFlights, dataBytes);
        return request;
    }

    public static ProtocolMessage DeleteFlightRequest()
    {
        var dataBytes = AskBytesCode();
        var request = new ProtocolMessage("REQ", CommandCode.DeleteFlight, dataBytes);
        return request;
    }

    private static byte[] AskBytesCode()
    {
        var code = AskCode();
        return Encoding.UTF8.GetBytes(code);
    }
    
    private static string AskCode()
    {
        string? code;

        do
        {
            Console.Write("Code: ");
            code = Console.ReadLine();
        } while (string.IsNullOrWhiteSpace(code) || code.Contains('|'));

        return code;
    }

    private static byte[] AskBytesFilters()
    {
        var (code, destination, hasAvailableSeats) = AskFilters();
        var data = $"{code}|{destination}|{hasAvailableSeats}";
        return Encoding.UTF8.GetBytes(data);
    }

    private static (string origin, string destination, string hasAvailableSeats) AskFilters()
    {
        Console.WriteLine("Search for flights (leave field empty for no filter)");

        string? origin, destination, hasAvailableSeats;

        do
        {
            Console.Write("Origin: ");
            origin = Console.ReadLine();
            if (String.IsNullOrEmpty(origin))
                origin = " ";
        } while (origin.Contains('|') || origin.Any(char.IsDigit));

        do
        {
            Console.Write("Destination: ");
            destination = Console.ReadLine();
            if (String.IsNullOrEmpty(destination))
                destination = " ";
        } while (destination.Contains('|') || destination.Any(char.IsDigit));
        
        do
        {
            Console.Write("Has available seats (y/n): ");
            hasAvailableSeats = Console.ReadLine();
            if (String.IsNullOrEmpty(hasAvailableSeats))
                hasAvailableSeats = " ";
        } while (hasAvailableSeats != "y" && hasAvailableSeats != "n" && hasAvailableSeats != " ");

        return (origin, destination, hasAvailableSeats);
    }

    private static byte[] AskBytesValues()
    {
        var (code, origin, destination, date, seats, duration, price) = AskValues();
        var data = $"{code}|{origin}|{destination}|{date}|{seats}|{duration}|{price}";
        return Encoding.UTF8.GetBytes(data);
    }

    private static (string code, string origin, string destination, string date, int seats, string duration, decimal price) AskValues()
    {
        string? code, origin, destination, date, seats, duration, price;

        do
        {
            Console.Write("Code: ");
            code = Console.ReadLine();
        } while (string.IsNullOrWhiteSpace(code) || code.Contains('|'));

        do
        {
            Console.Write("Origin: ");
            origin = Console.ReadLine();
        } while (string.IsNullOrWhiteSpace(origin) || origin.Contains('|') || origin.Any(char.IsDigit));

        do
        {
            Console.Write("Destination: ");
            destination = Console.ReadLine();
        } while (string.IsNullOrWhiteSpace(destination) || destination.Contains('|') || destination.Any(char.IsDigit));

        do
        {
            Console.Write("Date (dd/MM/yyyy): ");
            date = Console.ReadLine();
        } while (string.IsNullOrWhiteSpace(date) || date.Contains('|') ||
                 !DateTime.TryParseExact(date, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime parsedDate) ||
                 parsedDate < DateTime.Today);

        do
        {
            Console.Write("Seats: ");
            seats = Console.ReadLine();
        } while (string.IsNullOrWhiteSpace(seats) || !int.TryParse(seats, out int s) || s <= 0);

        do
        {
            Console.Write("Price: ");
            price = Console.ReadLine();
        } while (string.IsNullOrWhiteSpace(price) || !double.TryParse(price, out double p) || p <= 0);

        do
        {
            Console.Write("Duration (HH:mm): ");
            duration = Console.ReadLine();
        } while (string.IsNullOrWhiteSpace(duration) || duration.Contains('|') ||
                 !TimeSpan.TryParseExact(duration, "hh\\:mm", null, out _));

        return (code, origin, destination, date, int.Parse(seats), duration, decimal.Parse(price));
    }
}
