using System.Text;
using Protocol;

namespace ClientAdmin.Services;

internal static class FlightService
{
    public static ProtocolMessage CancelFlightRequest()
    {
        var (flightCode, reason, webhookUrl) = AskCancellationValues();
        var data = $"{flightCode}|{reason}|{webhookUrl}";
        var dataBytes = Encoding.UTF8.GetBytes(data);
        return new ProtocolMessage("REQ", CommandCode.CancelFlight, dataBytes);
    }

    private static (string flightCode, string reason, string webhookUrl) AskCancellationValues()
    {
        string? flightCode;
        string? reason;
        string? webhookUrl;

        do
        {
            Console.Write("Flight Code: ");
            flightCode = Console.ReadLine()?.Trim();
        } while (string.IsNullOrWhiteSpace(flightCode) || flightCode.Contains('|'));

        do
        {
            Console.Write("Cancellation Reason: ");
            reason = Console.ReadLine()?.Trim();
        } while (string.IsNullOrWhiteSpace(reason) || reason.Contains('|'));

        do
        {
            Console.Write("Webhook URL (optional): ");
            webhookUrl = Console.ReadLine()?.Trim();
        } while (!string.IsNullOrEmpty(webhookUrl) && !IsValidWebhookUrl(webhookUrl));

        return (flightCode, reason, webhookUrl ?? string.Empty);
    }

    private static bool IsValidWebhookUrl(string webhookUrl)
    {
        return !webhookUrl.Contains('|')
            && Uri.TryCreate(webhookUrl, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
