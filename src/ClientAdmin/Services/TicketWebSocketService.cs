using System.Net.WebSockets;
using System.Text;

namespace ClientAdmin.Services;

internal static class TicketWebSocketService
{
    private const int BufferSize = 1024 * 4;
    private static readonly TimeSpan ReconnectDelay = TimeSpan.FromSeconds(2);

    public static async Task ListenAsync(string uri, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            using var webSocket = new ClientWebSocket();

            try
            {
                Console.WriteLine($"Connecting to ticket broadcasts at {uri}...");
                await webSocket.ConnectAsync(new Uri(uri), cancellationToken);
                Console.WriteLine("Connected to ticket broadcasts.");
                await ReceiveEventsAsync(webSocket, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (WebSocketException ex)
            {
                Console.WriteLine($"Ticket broadcast connection error: {ex.Message}");
            }
            finally
            {
                await CloseAsync(webSocket);
            }

            if (!cancellationToken.IsCancellationRequested)
            {
                Console.WriteLine("Reconnecting to ticket broadcasts...");

                try
                {
                    await Task.Delay(ReconnectDelay, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }

    private static async Task ReceiveEventsAsync(ClientWebSocket webSocket, CancellationToken cancellationToken)
    {
        var buffer = new byte[BufferSize];

        while (!cancellationToken.IsCancellationRequested && webSocket.State == WebSocketState.Open)
        {
            var receiveResult = await webSocket.ReceiveAsync(
                new ArraySegment<byte>(buffer),
                cancellationToken);

            if (receiveResult.MessageType == WebSocketMessageType.Close)
            {
                return;
            }

            var message = Encoding.UTF8.GetString(buffer, 0, receiveResult.Count);
            Console.WriteLine(message);
        }
    }

    private static async Task CloseAsync(ClientWebSocket webSocket)
    {
        var canClose = webSocket.State == WebSocketState.Open ||
                       webSocket.State == WebSocketState.CloseReceived;

        if (!canClose)
        {
            return;
        }

        try
        {
            await webSocket.CloseAsync(
                WebSocketCloseStatus.NormalClosure,
                "Closing",
                CancellationToken.None);
        }
        catch (WebSocketException)
        {
            Console.WriteLine("Websocket exception...");
        }
    }
}
