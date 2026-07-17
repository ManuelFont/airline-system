namespace Domain;

public class Ticket
{
    public Guid Id { get; }
    public Flight Flight { get; }
    public User User { get; }
    public decimal PriceAtPurchase { get; }

    public bool IsCanceled { get; set; }

    public Ticket(Flight flight, User user, decimal priceAtPurchase)
    {
        Id = Guid.NewGuid();
        Flight = flight;
        User = user;
        IsCanceled = false;
        PriceAtPurchase = priceAtPurchase;
    }
}