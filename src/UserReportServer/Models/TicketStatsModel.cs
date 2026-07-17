namespace UserReportServer.Models;

internal sealed class TicketStatsModel
{
    public int PurchasedCount { get; }
    public int CancelledCount { get; }
    public decimal? AveragePrice { get; }
    public decimal? CheapestPrice { get; }
    public decimal? MostExpensivePrice { get; }

    public TicketStatsModel(IEnumerable<TicketReportModel> tickets)
    {
        var ticketList = tickets.ToList();

        PurchasedCount = ticketList.Count;
        CancelledCount = ticketList.Count(ticket => ticket.IsCanceled);

        if (ticketList.Count == 0)
            return;

        var prices = ticketList.Select(ticket => ticket.PriceAtPurchase).ToList();
        AveragePrice = prices.Average();
        CheapestPrice = prices.Min();
        MostExpensivePrice = prices.Max();
    }
}
