using System.Text;
using Domain;
using Protocol;

namespace Server.Services;

internal static class FlightService
{
    private static List<Flight> _flights = [];
    private static readonly object Locker = new object();

    public static List<Flight> GetFlightsSnapshot()
    {
        lock (Locker)
        {
            return new List<Flight>(_flights);
        }
    }

    public static ProtocolMessage CreateFlightResponse(ProtocolMessage request, User user)
    {
        try
        {
            var (code, origin, destination, date, seats, duration, price) = ParseValues(Encoding.UTF8.GetString(request.Data));
            var flight = new Flight(code, origin, destination, date, seats, duration, price, user);

            lock (Locker)
            {
                if (_flights.Any(f => f.Code == code))
                    throw new ArgumentException("Flight with that code already exists");

                _flights.Add(flight);
            }

            return BuildResponse(CommandCode.CreateFlight, "Flight created successfully");
        }
        catch (ArgumentException ex)
        {
            return BuildResponse(CommandCode.Error, ex.Message);
        }
    }

    public static ProtocolMessage EditFlightResponse(ProtocolMessage request, User user)
    {
        try
        {
            var (code, origin, destination, date, seats, duration, price) = ParseValues(Encoding.UTF8.GetString(request.Data));

            Flight? flight;
            lock (Locker)
            {
                flight = _flights.FirstOrDefault(f => f.Code == code);
                if (flight == null)
                    throw new ArgumentException("No flight with that code exists");
                ValidateEditFlight(flight, user);
                flight.Update(origin, destination, date, seats, duration, price);
            }

            return BuildResponse(CommandCode.ModifyFlight, "Flight modified successfully");
        }
        catch (ArgumentException ex)
        {
            return BuildResponse(CommandCode.Error, ex.Message);
        }
    }

    public static ProtocolMessage GetFlightResponse(ProtocolMessage request)
    {
        try
        {
            var code = Encoding.UTF8.GetString(request.Data);
            Flight? flight;

            lock (Locker)
            {
                flight = _flights.FirstOrDefault(f => f.Code == code);
            }

            if (flight == null)
                throw new ArgumentException("No flight with that code exists");

            return BuildResponse(CommandCode.GetFlight, flight.ToDisplayString());
        }
        catch (ArgumentException ex)
        {
            return BuildResponse(CommandCode.Error, ex.Message);
        }
    }

    public static ProtocolMessage DeleteFlightResponse(ProtocolMessage request, User user)
    {
        try
        {
            var code = Encoding.UTF8.GetString(request.Data);
            Flight? flightToDelete;

            lock (Locker)
            {
                flightToDelete = _flights.FirstOrDefault(f => f.Code == code);
                if (flightToDelete == null)
                    throw new ArgumentException("No flight with that code exists");
                flightToDelete.ValidateDeletion(user);
                _flights.Remove(flightToDelete);
            }
            if (flightToDelete.ImagePath != null)
                RemoveImage(flightToDelete.ImagePath);
            return BuildResponse(CommandCode.DeleteFlight, "Flight deleted successfully");
        }
        catch (ArgumentException ex)
        {
            return BuildResponse(CommandCode.Error, ex.Message);
        }
    }

    public static async Task<ProtocolMessage> CancelFlightResponseAsync(ProtocolMessage request, User user)
    {
        try
        {
            var (flightCode, reason, webhookUrl) =
                ParseCancellationValues(Encoding.UTF8.GetString(request.Data));

            if (string.IsNullOrWhiteSpace(flightCode))
                throw new ArgumentException("Flight code is required");

            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("Cancellation reason is required");

            Flight flight;
            FlightCancellation cancellation;

            lock (Locker)
            {
                flight = _flights.FirstOrDefault(f => f.Code == flightCode)
                    ?? throw new ArgumentException("No flight with that code exists");

                cancellation = flight.Cancel(reason, user);
            }

            var reportId = await FlightCancellationReportService.PublishAsync(
                flight,
                cancellation,
                webhookUrl);
            return BuildResponse(
                CommandCode.CancelFlight,
                $"Flight cancelled successfully. Report ID: {reportId}");
        }
        catch (ArgumentException ex)
        {
            return BuildResponse(CommandCode.Error, ex.Message);
        }
    }

    private static readonly HashSet<string> ImageExtensions = [".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp"];

    private static void RemoveImage(string path)
    {
        if (!File.Exists(path))
            throw new ArgumentException($"No file at: {path}");

        string ext = Path.GetExtension(path).ToLowerInvariant();
        if (!ImageExtensions.Contains(ext))
            throw new ArgumentException($"File is not a recognized image: {path}");

        File.Delete(path);
    }

    public static ProtocolMessage FilterFlightsResponse(ProtocolMessage request)
    {
        try
        {
            var (origin, destination, hasAvailableSeats) = ParseFilters(Encoding.UTF8.GetString(request.Data));
            List<Flight> snapshot;

            lock (Locker)
            {
                snapshot = new List<Flight>(_flights);
            }

            var filteredFlights = FilteredFlights(snapshot, origin, destination, hasAvailableSeats);
            return BuildResponse(CommandCode.ListFlights, FlightsToString(filteredFlights));
        }
        catch (ArgumentException ex)
        {
            return BuildResponse(CommandCode.Error, ex.Message);
        }
    }

    private static string FlightsToString(List<Flight> flights)
    {
        string result = "No flights match those filters";
        if (flights.Count > 0)
            result = string.Join("\n---\n", flights.Select(f => f.ToDisplayString()));

        return result;
    }

    private static List<Flight> FilteredFlights(List<Flight> flights, string origin, string destination, string hasAvailableSeatsString)
    {
        var filtered = flights.AsEnumerable();

        if (origin.Trim() != "")
            filtered = filtered.Where(f => f.Origin.Equals(origin.Trim(), StringComparison.OrdinalIgnoreCase));

        if (destination.Trim() != "")
            filtered = filtered.Where(f => f.Destination.Equals(destination.Trim(), StringComparison.OrdinalIgnoreCase));

        if (hasAvailableSeatsString == "y")
            filtered = filtered.Where(f => f.HasAvailableSeats);
        else if (hasAvailableSeatsString == "n")
            filtered = filtered.Where(f => !f.HasAvailableSeats);

        return filtered.ToList();
    }

    public static Flight? FindFlight(string code)
    {
        lock (Locker)
        {
            return _flights.FirstOrDefault(f => f.Code == code);
        }
    }

    private static void ValidateEditFlight(Flight flight, User user)
    {
        if (flight.AvailableSeats < flight.Seats)
            throw new ArgumentException("The flight has sold not cancelled tickets");

        if (flight.HasDeparted)
            throw new ArgumentException("Flight has departed already :(");

        if (flight.Owner != user)
            throw new ArgumentException("Cant modify, you are not the owner");
    }

    private static (string origin, string destination, string hasAvailableSeats) ParseFilters(string data)
    {
        var parts = data.Split('|');
        return (
            origin: parts[0],
            destination: parts[1],
            hasAvailableSeats: parts[2]
        );
    }

    private static (string code, string origin, string destination, string date, int seats, string duration, decimal price) ParseValues(string data)
    {
        var parts = data.Split('|');
        return (
            code: parts[0],
            origin: parts[1],
            destination: parts[2],
            date: parts[3],
            seats: int.Parse(parts[4]),
            duration: parts[5],
            price: decimal.Parse(parts[6])
        );
    }

    private static (string flightCode, string reason, string? webhookUrl) ParseCancellationValues(
        string data)
    {
        var parts = data.Split('|');

        if (parts.Length is < 2 or > 3)
            throw new ArgumentException(
                "Cancellation request must contain flight code, reason and optional webhook URL");

        String? webhookUrl;

        if (parts.Length == 3 && !string.IsNullOrWhiteSpace(parts[2]))
        {
            webhookUrl = parts[2].Trim();
        }
        else
        {
            webhookUrl = null;
        }

        if (webhookUrl != null && !IsValidWebhookUrl(webhookUrl))
            throw new ArgumentException("Webhook URL must use HTTP or HTTPS");

        return (parts[0].Trim(), parts[1].Trim(), webhookUrl);
    }

    private static bool IsValidWebhookUrl(string webhookUrl)
    {
        return Uri.TryCreate(webhookUrl, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    private static ProtocolMessage BuildResponse(string code, string message)
        => new ProtocolMessage("RES", code, Encoding.UTF8.GetBytes(message));
}
