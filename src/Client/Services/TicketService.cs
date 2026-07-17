using System.Text;
using Protocol;

namespace Client.Services;

internal static class TicketService
{
    public static ProtocolMessage BuyTicketRequest()
    {
        var dataBytes = AskBytesCode();
        var request = new ProtocolMessage("REQ", CommandCode.BuyTicket, dataBytes);
        return request;
    }

    public static ProtocolMessage CancelTicketrequest()
    {
        var dataBytes = AskBytesCancelValues();
        var request = new ProtocolMessage("REQ", CommandCode.CancelTicket, dataBytes);
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
            Console.Write("Flight Code: ");
            code = Console.ReadLine();
        } while (string.IsNullOrWhiteSpace(code) || code.Contains('|'));

        return code;
    }

    private static byte[] AskBytesCancelValues()
    {
        var (code, ticketId) = AskCancelValues();
        var data = $"{code}|{ticketId}";
        return Encoding.UTF8.GetBytes(data);
    }

    private static (string code, string ticketId) AskCancelValues()
    {
        string? code, ticketId;

        do
        {
            Console.Write("Flight Code: ");
            code = Console.ReadLine();
        } while (string.IsNullOrWhiteSpace(code) || code.Contains('|'));
        
        do
        {
            Console.Write("Ticket Code (must be GUID): ");
            ticketId = Console.ReadLine();
        } while (string.IsNullOrWhiteSpace(ticketId) || !Guid.TryParse(ticketId, out _));

        return (code, ticketId);
    }
}
