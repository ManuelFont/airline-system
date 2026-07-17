using System.Net;
using System.Net.Sockets;
using System.Text;
using ClientAdmin.Services;
using Common;
using Protocol;

namespace ClientAdmin;

class Program
{
    private static IPAddress _clientIp = IPAddress.Parse("0.0.0.0");
    private static string _serverHost = Environment.GetEnvironmentVariable(ServerConnectionConfig.ServerHostKey) ?? ServerConnectionConfig.DefaultServerHost;
    private static IPAddress _serverIp = IPAddress.Parse(_serverHost);
    private static int _serverPort = int.Parse(Environment.GetEnvironmentVariable(ServerConnectionConfig.ServerPortKey) ?? ServerConnectionConfig.DefaultServerPort);
    private static int _webSocketPort = int.Parse(Environment.GetEnvironmentVariable(ServerConnectionConfig.WebSocketPortKey) ?? ServerConnectionConfig.DefaultWebSocketPort);
    private static ProtocolSocket _protocolSocket = null!;
    private static bool _runClient = true;

    static async Task Main(string[] args)
    {
        try
        {
            IPEndPoint clientEndpoint = new IPEndPoint(_clientIp, 0);
            IPEndPoint serverEndpoint = new IPEndPoint(_serverIp, _serverPort);

            Console.WriteLine($"Client Endpoint {clientEndpoint}");
            Console.WriteLine($"Connecting to server at {serverEndpoint}...");

            Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            clientSocket.Bind(clientEndpoint);
            await clientSocket.ConnectAsync(serverEndpoint);

            Console.WriteLine("Connected to server successfully!");
            _protocolSocket = new ProtocolSocket(clientSocket);

            try
            {
                await LoginMenuAsync();
                while (_runClient)
                {
                    await MainMenuAsync();
                }
            }
            finally
            {
                if (clientSocket.Connected)
                {
                    clientSocket.Shutdown(SocketShutdown.Both);
                }
                clientSocket.Close();
                Console.WriteLine("Disconnected from server.");
            }
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"Socket error: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    private static async Task LoginMenuAsync()
    {
        bool runMenu = true;

        while (runMenu)
        {
            Console.WriteLine("\n=== Main Menu ===");
            Console.WriteLine("1. Log in");
            Console.WriteLine("0. Exit");
            Console.Write("\nOption: ");

            string? input = Console.ReadLine();

            switch (input?.Trim())
            {
                case "1":
                    var loginRequest = AuthService.LoginRequest();
                    var response = await CommunicateWithServerAsync(loginRequest);
                    runMenu = response.Command != CommandCode.Login;
                    break;
                case "0":
                    runMenu = false;
                    _runClient = false;
                    break;
                default:
                    Console.WriteLine("Invalid option, try again.");
                    break;
            }
        }
    }

    private static async Task MainMenuAsync()
    {
        Console.WriteLine("\n=== Main Menu ===");
        Console.WriteLine("1. Cancel flight");
        Console.WriteLine("2. View live ticket purchases");
        Console.WriteLine("0. Exit");
        Console.Write("\nOption: ");

        string? input = Console.ReadLine();

        switch (input?.Trim())
        {
            case "1":
                var cancelFlightRequest = FlightService.CancelFlightRequest();
                await CommunicateWithServerAsync(cancelFlightRequest);
                break;
            case "2":
                await ViewLiveTicketPurchasesAsync();
                break;
            case "0":
                _runClient = false;
                break;
            default:
                Console.WriteLine("Invalid option, try again.");
                break;
        }
    }

    private static async Task ViewLiveTicketPurchasesAsync()
    {
        var webSocketUri = $"ws://{_serverHost}:{_webSocketPort}/ws/tickets";
        using var cancellationTokenSource = new CancellationTokenSource();
        var listenerTask = TicketWebSocketService.ListenAsync(webSocketUri, cancellationTokenSource.Token);

        Console.WriteLine("Listening for ticket purchases. Hit enter to return to the main menu.");
        Console.ReadLine();

        await cancellationTokenSource.CancelAsync();
        await listenerTask;
    }

    private static async Task<ProtocolMessage> CommunicateWithServerAsync(ProtocolMessage request)
    {
        await _protocolSocket.SendAsync(request);

        Console.WriteLine("Waiting for server response...");
        var response = await _protocolSocket.ReceiveAsync();
        Console.WriteLine($"Received: {response}");
        Console.WriteLine($"Response content: {Encoding.UTF8.GetString(response.Data)}");
        Console.WriteLine("Hit enter to continue...");
        Console.ReadLine();
        return response;
    }
}
