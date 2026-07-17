using System.Net.WebSockets;
using System.Text;
using System.Text.Json.Nodes;

namespace Server.Services;

internal static class TicketWebSocketConnectionService
{
    private static readonly List<WebSocket> WebSockets = [];
    private static readonly Lock WebSocketsLock = new();
    private static readonly SemaphoreSlim BroadcastSemaphore = new(1, 1);

    public static void Add(WebSocket webSocket)
    {
        lock (WebSocketsLock)
        {
            WebSockets.Add(webSocket);
        }
    }

    public static void Remove(WebSocket webSocket)
    {
        lock (WebSocketsLock)
        {
            WebSockets.Remove(webSocket);
        }
    }

    public static async Task BroadcastAsync(JsonObject message)
    {
        var messageBytes = Encoding.UTF8.GetBytes(message.ToJsonString());
        await BroadcastSemaphore.WaitAsync(CancellationToken.None);

        try
        {
            var webSockets = GetWebSocketsSnapshot();

            foreach (var webSocket in webSockets)
            {
                if (webSocket.State != WebSocketState.Open)
                {
                    RemoveAndDispose(webSocket);
                    continue;
                }

                try
                {
                    await webSocket.SendAsync(
                        new ArraySegment<byte>(messageBytes),
                        WebSocketMessageType.Text,
                        true,
                        CancellationToken.None);
                }
                catch (WebSocketException)
                {
                    RemoveAndDispose(webSocket);
                }
            }
        }
        finally
        {
            BroadcastSemaphore.Release();
        }
    }

    private static List<WebSocket> GetWebSocketsSnapshot()
    {
        lock (WebSocketsLock)
        {
            return [.. WebSockets];
        }
    }

    private static void RemoveAndDispose(WebSocket webSocket)
    {
        Remove(webSocket);
        webSocket.Dispose();
    }
}
