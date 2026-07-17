using System.Net;
using System.Net.Sockets;
using System.Text;
using Domain;
using Protocol;
using Server.Services;

namespace Server;

class Program
{
    private const string AvailableCommandsMessage = "Available commands: report, cancel, get-report";
    private static IPAddress _ip = IPAddress.Parse("0.0.0.0");
    private static int _port = int.Parse(Environment.GetEnvironmentVariable(ServerConfig.ServerPortKey) ?? "5100");
    private static int _webSocketPort = int.Parse(Environment.GetEnvironmentVariable(ServerConfig.WebSocketPortKey) ?? "5101");

    private static async Task Main(string[] args)
    {
        try
        {
            await FlightCancellationReportService.InitializeAsync();
            var webSocketApplication = BuildWebSocketApplication(args);
            var tcpServerTask = RunTcpServerAsync();
            var webSocketServerTask = webSocketApplication.RunAsync($"http://0.0.0.0:{_webSocketPort}");

            await Task.WhenAll(tcpServerTask, webSocketServerTask);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
        finally
        {
            FlightCancellationReportService.Dispose();
        }
    }

    private static WebApplication BuildWebSocketApplication(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddControllers();

        var application = builder.Build();
        application.UseWebSockets();
        application.MapControllers();
        return application;
    }

    private static async Task RunTcpServerAsync()
    {
        IPEndPoint serverEndpoint = new IPEndPoint(_ip, _port);
        Console.WriteLine($"Starting server on {serverEndpoint}");

        Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        serverSocket.Bind(serverEndpoint);
        serverSocket.Listen(5);

        _ = Task.Run(() =>
        {
            Console.WriteLine(AvailableCommandsMessage);

            string? line;
            while ((line = Console.ReadLine()) != null)
            {
                var trimmed = line.Trim().ToLower();
                if (trimmed == "report")
                {
                    _ = Task.Run(UserReportRequestService.GenerateAsync);
                }
                else if (trimmed == "cancel")
                {
                    _ = Task.Run(UserReportRequestService.CancelAsync);
                }
                else if (trimmed == "get-report")
                {
                    Console.Write("Username: ");
                    var username = Console.ReadLine();

                    if (string.IsNullOrWhiteSpace(username))
                    {
                        Console.WriteLine("Username cannot be empty.");
                        continue;
                    }

                    _ = Task.Run(() => UserReportRequestService.GetReportAsync(username.Trim()));
                }
                else
                {
                    Console.WriteLine("Unknown command.");
                    Console.WriteLine(AvailableCommandsMessage);
                }
            }
        });

        Console.WriteLine("Server is listening for clients...");

        while (true)
        {
            Socket clientSocket = await serverSocket.AcceptAsync();
            Console.WriteLine($"\n[SERVER] Client connected from {clientSocket.RemoteEndPoint}");

            _ = HandleClientAsync(clientSocket);
        }
    }

    static async Task HandleClientAsync(Socket clientSocket)
    {
        string clientEndpoint = clientSocket.RemoteEndPoint?.ToString() ?? "Unknown";
        var protocolSocket = new ProtocolSocket(clientSocket);
        User? user = null;

        try
        {
            Console.WriteLine($"[{clientEndpoint}] Handling client...");

            while (true)
            {
                ProtocolMessage request;

                try
                {
                    request = await protocolSocket.ReceiveAsync();
                }
                catch (SocketException ex)
                {
                    if (ex.SocketErrorCode == SocketError.ConnectionReset)
                    {
                        Console.WriteLine($"[{clientEndpoint}] Connection reset by client.");
                    }
                    else
                    {
                        Console.WriteLine($"[{clientEndpoint}] Socket error: {ex.Message}");
                    }
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{clientEndpoint}] Invalid protocol message: {ex.Message}");
                    break;
                }

                try
                {
                    user = await HandleRequestAsync(protocolSocket, request, user);
                }
                catch (SocketException ex)
                {
                    Console.WriteLine($"[{clientEndpoint}] Socket error: {ex.Message}");
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{clientEndpoint}] Request error: {ex.Message}");

                    try
                    {
                        await HandleErrorAsync(protocolSocket, ex.Message);
                    }
                    catch (SocketException socketException)
                    {
                        Console.WriteLine($"[{clientEndpoint}] Socket error: {socketException.Message}");
                        break;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{clientEndpoint}] Client error: {ex.Message}");
        }
        finally
        {
            if (clientSocket.Connected)
            {
                clientSocket.Shutdown(SocketShutdown.Both);
            }
            clientSocket.Close();
            Console.WriteLine($"[{clientEndpoint}] Connection closed.");
        }
    }

    private static async Task<User?> HandleRequestAsync(ProtocolSocket protocolSocket, ProtocolMessage request, User? user)
    {
        switch (request.Command)
        {
            case CommandCode.Register:
                return await HandleRegisterAsync(protocolSocket, request);
            case CommandCode.Login:
                return await HandleLoginAsync(protocolSocket, request);
            case CommandCode.CreateFlight:
                await HandleCreateFlightAsync(protocolSocket, request, user ?? throw new InvalidOperationException("no user logged"));
                return user;
            case CommandCode.ModifyFlight:
                await HandleUpdateFlightAsync(protocolSocket, request, user ?? throw new InvalidOperationException("no user logged"));
                return user;
            case CommandCode.GetFlight:
                await HandleGetFlightAsync(protocolSocket, request);
                return user;
            case CommandCode.DeleteFlight:
                await HandleDeleteFlightAsync(protocolSocket, request, user ?? throw new InvalidOperationException("no user logged"));
                return user;
            case CommandCode.CancelFlight:
                await HandleCancelFlightAsync(protocolSocket, request, user ?? throw new InvalidOperationException("no user logged"));
                return user;
            case CommandCode.ListFlights:
                await HandleListFlightAsync(protocolSocket, request);
                return user;
            case CommandCode.BuyTicket:
                await HandleBuyTicketAsync(protocolSocket, request, user ?? throw new InvalidOperationException("no user logged"));
                return user;
            case CommandCode.CancelTicket:
                await HandleCancelTicketAsync(protocolSocket, request, user ?? throw new InvalidOperationException("no user logged"));
                return user;
            case CommandCode.UploadImageInfo:
                await HandleImageInfoAsync(protocolSocket, request, user ?? throw new InvalidOperationException("no user logged"));
                return user;
            case CommandCode.UploadImageData:
                await HandleImageDataAsync(protocolSocket, request);
                return user;
            case CommandCode.DownloadImage:
                await HandleDownloadImageInfoAsync(protocolSocket, request);
                return user;
            case CommandCode.DownloadImageData:
                await HandleDownloadImageDataAsync(protocolSocket, request);
                return user;
            default:
                await HandleUnknownAsync(protocolSocket);
                return user;
        }
    }

    private static async Task HandleImageDataAsync(ProtocolSocket protocolSocket, ProtocolMessage request)
    {
        Console.WriteLine($"[{protocolSocket.RemoteEndPoint}] {request}");
        var response = await ImageService.UploadImageDataResponseAsync(request);
        Console.WriteLine($"[SERVER] {response}");
        await protocolSocket.SendAsync(response);
    }

    private static async Task HandleImageInfoAsync(ProtocolSocket protocolSocket, ProtocolMessage request, User user)
    {
        Console.WriteLine($"[{protocolSocket.RemoteEndPoint}] {request}");
        var response = ImageService.UploadImageInfoResponse(request, user);
        Console.WriteLine($"[SERVER] {response}");
        await protocolSocket.SendAsync(response);
    }

    private static async Task HandleDownloadImageInfoAsync(ProtocolSocket protocolSocket, ProtocolMessage request)
    {
        Console.WriteLine($"[{protocolSocket.RemoteEndPoint}] {request}");
        var response = ImageService.DownloadImageInfoResponse(request);
        Console.WriteLine($"[SERVER] {response}");
        await protocolSocket.SendAsync(response);
    }

    private static async Task HandleDownloadImageDataAsync(ProtocolSocket protocolSocket, ProtocolMessage request)
    {
        Console.WriteLine($"[{protocolSocket.RemoteEndPoint}] {request}");
        var response = await ImageService.DownloadImageDataResponse(request);
        Console.WriteLine($"[SERVER] {response}");
        await protocolSocket.SendAsync(response);
    }

    private static async Task HandleCancelTicketAsync(ProtocolSocket protocolSocket, ProtocolMessage request, User user)
    {
        Console.WriteLine($"[{protocolSocket.RemoteEndPoint}] {request}");
        var response = TicketService.CancelTicketResponse(request, user);
        Console.WriteLine($"[SERVER] {response}");
        await protocolSocket.SendAsync(response);
    }

    private static async Task HandleBuyTicketAsync(ProtocolSocket protocolSocket, ProtocolMessage request, User user)
    {
        Console.WriteLine($"[{protocolSocket.RemoteEndPoint}] {request}");
        var response = await TicketService.BuyTicketResponseAsync(request, user);
        Console.WriteLine($"[SERVER] {response}");
        await protocolSocket.SendAsync(response);
    }

    private static async Task HandleListFlightAsync(ProtocolSocket protocolSocket, ProtocolMessage request)
    {
        Console.WriteLine($"[{protocolSocket.RemoteEndPoint}] {request}");
        var response = FlightService.FilterFlightsResponse(request);
        Console.WriteLine($"[SERVER] {response}");
        await protocolSocket.SendAsync(response);
    }

    private static async Task HandleDeleteFlightAsync(ProtocolSocket protocolSocket, ProtocolMessage request, User user)
    {
        Console.WriteLine($"[{protocolSocket.RemoteEndPoint}] {request}");
        var response = FlightService.DeleteFlightResponse(request, user);
        Console.WriteLine($"[SERVER] {response}");
        await protocolSocket.SendAsync(response);
    }

    private static async Task HandleCancelFlightAsync(ProtocolSocket protocolSocket, ProtocolMessage request, User user)
    {
        Console.WriteLine($"[{protocolSocket.RemoteEndPoint}] {request}");
        var response = await FlightService.CancelFlightResponseAsync(request, user);
        Console.WriteLine($"[SERVER] {response}");
        await protocolSocket.SendAsync(response);
    }

    private static async Task HandleGetFlightAsync(ProtocolSocket protocolSocket, ProtocolMessage request)
    {
        Console.WriteLine($"[{protocolSocket.RemoteEndPoint}] {request}");
        var response = FlightService.GetFlightResponse(request);
        Console.WriteLine($"[SERVER] {response}");
        await protocolSocket.SendAsync(response);
    }

    private static async Task HandleUpdateFlightAsync(ProtocolSocket protocolSocket, ProtocolMessage request, User user)
    {
        Console.WriteLine($"[{protocolSocket.RemoteEndPoint}] {request}");
        var response = FlightService.EditFlightResponse(request, user);
        Console.WriteLine($"[SERVER] {response}");
        await protocolSocket.SendAsync(response);
    }

    private static async Task HandleCreateFlightAsync(ProtocolSocket protocolSocket, ProtocolMessage request, User user)
    {
        Console.WriteLine($"[{protocolSocket.RemoteEndPoint}] {request}");
        var response = FlightService.CreateFlightResponse(request, user);
        Console.WriteLine($"[SERVER] {response}");
        await protocolSocket.SendAsync(response);
    }

    private static async Task<User?> HandleRegisterAsync(ProtocolSocket protocolSocket, ProtocolMessage request)
    {
        Console.WriteLine($"[{protocolSocket.RemoteEndPoint}] {request}");
        var (user, response) = AuthService.RegistrationResponse(request);
        Console.WriteLine($"[SERVER] {response}");
        await protocolSocket.SendAsync(response);
        return user;
    }

    private static async Task<User?> HandleLoginAsync(ProtocolSocket protocolSocket, ProtocolMessage request)
    {
        Console.WriteLine($"[{protocolSocket.RemoteEndPoint}] {request}");
        var (user, response) = AuthService.LoginResponse(request);
        Console.WriteLine($"[SERVER] {response}");
        await protocolSocket.SendAsync(response);
        return user;
    }

    private static async Task HandleUnknownAsync(ProtocolSocket protocolSocket)
    {
        await HandleErrorAsync(protocolSocket, "Unknown command");
    }

    private static async Task HandleErrorAsync(ProtocolSocket protocolSocket, string message)
    {
        var bytesMessage = Encoding.UTF8.GetBytes(message);
        var protocolMessage = new ProtocolMessage("RES", CommandCode.Error, bytesMessage);
        await protocolSocket.SendAsync(protocolMessage);
    }

}
