namespace UserReportServer.Models;

internal sealed class TicketReportModel
{
    public string Id { get; }
    public string BuyerUsername { get; }
    public decimal PriceAtPurchase { get; }
    public bool IsCanceled { get; }

    public TicketReportModel(string id, string buyerUsername, decimal priceAtPurchase, bool isCanceled)
    {
        Id = id;
        BuyerUsername = buyerUsername;
        PriceAtPurchase = priceAtPurchase;
        IsCanceled = isCanceled;
    }
}
