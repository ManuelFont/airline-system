using System.Net.WebSockets;
using Microsoft.AspNetCore.Mvc;
using Server.Services;

namespace Server.Controllers;

public class TicketWebSocketController : ControllerBase
{
    [Route("/ws/tickets")]
    public async Task Get()
    {
        if (!HttpContext.WebSockets.IsWebSocketRequest)
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
        TicketWebSocketConnectionService.Add(webSocket);

        try
        {
            await WaitForCloseAsync(webSocket);
        }
        catch (WebSocketException ex)
        {
            Console.WriteLine($"WebSocket connection error: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error: {ex.Message}");
        }
        finally
        {
            TicketWebSocketConnectionService.Remove(webSocket);
            webSocket.Dispose();
        }
    }

    private static async Task WaitForCloseAsync(WebSocket webSocket)
    {
        var buffer = new byte[1024 * 4];
        while (webSocket.State == WebSocketState.Open)
        {
            var receiveResult = await webSocket.ReceiveAsync(
                new ArraySegment<byte>(buffer),
                CancellationToken.None);

            if (receiveResult.CloseStatus.HasValue)
            {
                await webSocket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "Closing",
                    CancellationToken.None);

                break;
            }
        }
    }
}
