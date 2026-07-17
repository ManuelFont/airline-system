namespace Domain;

public sealed class TicketSnapshot
{
    public Guid Id { get; }
    public string BuyerUsername { get; }
    public decimal PriceAtPurchase { get; }
    public bool IsCanceled { get; }

    internal TicketSnapshot(Ticket ticket)
    {
        Id = ticket.Id;
        BuyerUsername = ticket.User.Username;
        PriceAtPurchase = ticket.PriceAtPurchase;
        IsCanceled = ticket.IsCanceled;
    }
}
