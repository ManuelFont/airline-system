namespace Domain;

public class Flight
{
    public string Code { get; set; }
    public string Origin { get; set; }
    public string Destination { get; set; }
    public User Owner { get; set; }
    public List<User> Passengers { get; set; } = [];
    public DateTime Date { get; set; }
    public int Seats { get; set; }
    public int AvailableSeats { get; set; }
    public decimal Price { get; set; }
    public TimeSpan Duration { get; set; }
    public List<Ticket> Tickets { get; set; } = [];
    public string? ImagePath { get; set; }
    public FlightCancellation? Cancellation { get; private set; }
    public bool HasDeparted => Date <= DateTime.UtcNow;
    public bool HasAvailableSeats => AvailableSeats > 0;
    public bool IsCancelled => Cancellation != null;

    private readonly Lock _locker = new Lock();

    public Ticket BuyTicket(User buyer)
    {
        lock (_locker)
        {
            var priceAtPurchase = Price;

            if (IsCancelled)
                throw new ArgumentException("Flight is cancelled");
            if (HasDeparted)
                throw new ArgumentException("Flight has already departed");
            if (!HasAvailableSeats)
                throw new ArgumentException("No available seats");
            if (Tickets.Any(t => t.User == buyer && !t.IsCanceled))
                throw new ArgumentException("You already have a ticket for this flight");

            var ticket = new Ticket(this, buyer, priceAtPurchase);

            AvailableSeats--;
            Passengers.Add(buyer);
            Tickets.Add(ticket);

            return ticket;
        }
    }

    public void CancelTicket(Guid ticketId, User buyer)
    {
        lock (_locker)
        {
            if (IsCancelled)
                throw new ArgumentException("Flight is cancelled");
            if (HasDeparted)
                throw new ArgumentException("Flight has already departed");

            var ticket = FindTicket(ticketId);
            if (ticket == null)
                throw new ArgumentException("Ticket not found");

            if (ticket.User != buyer)
                throw new ArgumentException("You are not the owner of this ticket");

            if (ticket.IsCanceled)
                throw new ArgumentException("This ticket is already cancelled");

            AvailableSeats++;
            ticket.IsCanceled = true;
            Passengers.Remove(buyer);
        }
    }

    public FlightCancellation Cancel(string reason, User cancelledBy)
    {
        lock (_locker)
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("Cancellation reason is required");

            if (IsCancelled)
                throw new ArgumentException("Flight is already cancelled");

            if (HasDeparted)
                throw new ArgumentException("Flight has already departed");

            foreach (var ticket in Tickets.Where(ticket => !ticket.IsCanceled))
            {
                ticket.IsCanceled = true;
            }

            Passengers.Clear();
            AvailableSeats = Seats;
            var cancellation = new FlightCancellation(DateTime.UtcNow, reason, cancelledBy);
            Cancellation = cancellation;
            return cancellation;
        }
    }

    public void ValidateDeletion(User user)
    {
        lock (_locker)
        {
            if (IsCancelled)
                throw new ArgumentException("Flight is cancelled");

            if (HasDeparted)
                throw new ArgumentException("Flight has departed already :(");

            if (AvailableSeats < Seats)
                throw new ArgumentException("The flight has sold not-cancelled tickets");

            if (Owner != user)
                throw new ArgumentException("Cant modify, you are not the owner");
        }
    }

    public List<Ticket> GetTicketsSnapshot()
    {
        lock (_locker)
        {
            return new List<Ticket>(Tickets);
        }
    }

    public FlightSnapshot GetSnapshot()
    {
        lock (_locker)
        {
            return new FlightSnapshot(this);
        }
    }

    private Ticket? FindTicket(Guid id)
    {
        return Tickets.FirstOrDefault(f => f.Id == id);
    }

    public Flight(string code, string origin, string destination, string date, int seats, string duration, decimal price, User owner)
    {
        Code = code;
        Origin = origin;
        Destination = destination;
        Date = DateTime.ParseExact(date, "dd/MM/yyyy", null);
        Seats = seats;
        AvailableSeats = Seats;
        Duration = TimeSpan.ParseExact(duration, "hh\\:mm", null);
        Price = price;
        Owner = owner;
    }

    public void Update(string origin, string destination, string date, int seats, string duration, decimal price, string imagePath)
    {
        var parsedDate = DateTime.ParseExact(date, "dd/MM/yyyy", null);
        var parsedDuration = TimeSpan.ParseExact(duration, "hh\\:mm", null);

        lock (_locker)
        {
            if (IsCancelled)
                throw new ArgumentException("Flight is cancelled");

            Origin = origin;
            Destination = destination;
            Date = parsedDate;
            Seats = seats;
            Duration = parsedDuration;
            Price = price;
            ImagePath = imagePath;
        }
    }

    public void Update(string origin, string destination, string date, int seats, string duration, decimal price)
    {
        var parsedDate = DateTime.ParseExact(date, "dd/MM/yyyy", null);
        var parsedDuration = TimeSpan.ParseExact(duration, "hh\\:mm", null);

        lock (_locker)
        {
            if (IsCancelled)
                throw new ArgumentException("Flight is cancelled");

            Origin = origin;
            Destination = destination;
            Date = parsedDate;
            Seats = seats;
            Duration = parsedDuration;
            Price = price;
        }
    }

    public string ToDisplayString()
    {
        string code, origin, destination, ownerUsername;
        string? imagePath;
        DateTime date;
        int seats, availableSeats;
        decimal price;
        TimeSpan duration;
        List<string> passengerUsernames;
        List<string> ticketStrings;
        bool isCancelled;

        lock (_locker)
        {
            code = Code;
            origin = Origin;
            destination = Destination;
            date = Date;
            seats = Seats;
            availableSeats = AvailableSeats;
            price = Price;
            duration = Duration;
            ownerUsername = Owner.Username;
            imagePath = ImagePath;
            isCancelled = IsCancelled;
            passengerUsernames = Passengers.Select(p => p.Username).ToList();
            ticketStrings = Tickets.Select(t => t.IsCanceled
                ? $"{t.Id} - {t.User.Username}(cancelled)"
                : $"{t.Id} - {t.User.Username}").ToList();
        }

        return $"Code: {code}\n" +
               $"Origin: {origin}\n" +
               $"Destination: {destination}\n" +
               $"Date: {date:dd/MM/yyyy}\n" +
               $"Seats: {seats}\n" +
               $"Available Seats: {availableSeats}\n" +
               $"Duration: {duration:hh\\:mm}\n" +
               $"Price: {price}\n" +
               $"Owner: {ownerUsername}\n" +
               $"Cancelled: {isCancelled}\n" +
               $"Passengers: {string.Join(", ", passengerUsernames)}\n" +
               $"Tickets: {string.Join(", ", ticketStrings)}\n" +
               $"Image: {imagePath ?? "None"}";
    }
}
