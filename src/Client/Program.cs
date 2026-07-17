using System.Net;
using System.Net.Sockets;
using System.Text;
using Client.Services;
using Common;
using Protocol;

namespace Client;

class Program
{
    private static IPAddress _clientIp = IPAddress.Parse("0.0.0.0");
    private static IPAddress _serverIp = IPAddress.Parse(Environment.GetEnvironmentVariable(ServerConnectionConfig.ServerHostKey) ?? ServerConnectionConfig.DefaultServerHost);
    private static int _serverPort = int.Parse(Environment.GetEnvironmentVariable(ServerConnectionConfig.ServerPortKey) ?? ServerConnectionConfig.DefaultServerPort);
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
        ProtocolMessage response;

        while (runMenu)
        {
            Console.WriteLine("\n=== Main Menu ===");
            Console.WriteLine("1. Register");
            Console.WriteLine("2. Log in");
            Console.WriteLine("0. Exit");
            Console.Write("\nOption: ");

            string? input = Console.ReadLine();

            switch (input?.Trim())
            {
                case "1":
                    var registrationRequest = AuthService.RegistrationRequest();
                    response = await CommunicateWithServerAsync(registrationRequest);
                    runMenu = response.Command != CommandCode.Register;
                    break;
                case "2":
                    var loginRequest = AuthService.LoginRequest();
                    response = await CommunicateWithServerAsync(loginRequest);
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
        bool runMenu = true;

        while (runMenu)
        {
            Console.WriteLine("\n=== Main Menu ===");
            Console.WriteLine("1. Create flight");
            Console.WriteLine("2. Modify flight");
            Console.WriteLine("3. Delete flight");
            Console.WriteLine("4. List flights");
            Console.WriteLine("5. Search flight by code");
            Console.WriteLine("6. Buy ticket");
            Console.WriteLine("7. Cancel ticket");
            Console.WriteLine("8. Upload image");
            Console.WriteLine("9. View history");
            Console.WriteLine("10. Log out");
            Console.WriteLine("11. Download image");
            Console.WriteLine("0. Exit");
            Console.Write("\nOption: ");
            
            string? input = Console.ReadLine();

            switch (input?.Trim())
            {
                case "1":
                    var createFlightRequest = FlightService.CreateFlightRequest();
                    await CommunicateWithServerAsync(createFlightRequest);
                    break;
                case "2":
                    var editFlightRequest = FlightService.EditFlightRequest();
                    await CommunicateWithServerAsync(editFlightRequest);
                    break;
                case "3":
                    var deleteFlightRequest = FlightService.DeleteFlightRequest();
                    await CommunicateWithServerAsync(deleteFlightRequest);
                    break;
                case "4":
                    var filterFlightRequest = FlightService.FilterFlightsRequest();
                    await CommunicateWithServerAsync(filterFlightRequest);
                    break;
                case "5":
                    var getFlightRequest = FlightService.GetFlightRequest();
                    await CommunicateWithServerAsync(getFlightRequest);
                    break;
                case "6":
                    var buyTicketRequest = TicketService.BuyTicketRequest();
                    await CommunicateWithServerAsync(buyTicketRequest);
                    break;
                case "7":
                    var cancelTicketRequest = TicketService.CancelTicketrequest();
                    await CommunicateWithServerAsync(cancelTicketRequest);
                    break;
                case "8":
                    await HandleUploadImageAsync();
                    break;
                case "11":
                    await HandleDownloadImageAsync();
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

    private static async Task HandleUploadImageAsync()
    {
        FileInfo? imageFile = GetImagePath();

        if (imageFile == null)
            return;
        
        Console.Write("Enter plane code: ");
        var planeCode = Console.ReadLine()?.Trim();

        if (String.IsNullOrEmpty(planeCode))
            return;
        
        var uploadImageInfoRequest = ImageService.UploadImageInfoRequest(imageFile, planeCode);
        var serverResponse = await CommunicateWithServerAsync(uploadImageInfoRequest);

        if (serverResponse.Command == CommandCode.Error)
            return;
        
        foreach (var chunk in await ImageService.UploadImageDataRequestsAsync(imageFile))
        {
            serverResponse = await CommunicateWithServerAsync(chunk);
            if (serverResponse.Command == CommandCode.Error)
                break;
        }
    }

    private static async Task HandleDownloadImageAsync()
    {
        var planeCode = ReadPlaneCode();
        if (planeCode == null)
            return;

        var downloadInfo = await RequestDownloadInfoAsync(planeCode);
        if (downloadInfo == null)
            return;

        var (fileName, fileSize, totalChunks) = downloadInfo.Value;
        var filePath = PrepareDownloadLocation(fileName);

        Console.WriteLine($"Downloading {fileName} ({fileSize} bytes, {totalChunks} chunks)...");

        if (await DownloadAllChunksAsync(planeCode, totalChunks, filePath))
            VerifyDownload(filePath, fileSize);
        else
            File.Delete(filePath);

        PromptToContinue();
    }

    private static string? ReadPlaneCode()
    {
        Console.Write("Enter plane code: ");
        var input = Console.ReadLine()?.Trim();
        return string.IsNullOrEmpty(input) ? null : input;
    }

    private static async Task<(string FileName, long FileSize, int TotalChunks)?> RequestDownloadInfoAsync(string planeCode)
    {
        var request = ImageService.DownloadImageInfoRequest(planeCode);
        var response = await CommunicateWithServerAsync(request);

        if (response.Command == CommandCode.Error)
            return null;

        var parts = Encoding.UTF8.GetString(response.Data).Split('|');
        if (parts.Length != 3)
        {
            Console.WriteLine("Error: Invalid response format");
            return null;
        }

        return (Path.GetFileName(parts[0]), long.Parse(parts[1]), int.Parse(parts[2]));
    }

    private static string PrepareDownloadLocation(string fileName)
    {
        var downloadsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "downloads");
        Directory.CreateDirectory(downloadsPath);
        var filePath = Path.Combine(downloadsPath, fileName);
        if (File.Exists(filePath))
            File.Delete(filePath);
        return filePath;
    }

    private static async Task<bool> DownloadAllChunksAsync(string planeCode, int totalChunks, string filePath)
    {
        for (var i = 0; i < totalChunks; i++)
        {
            Console.Write($"Downloading chunk {i + 1}/{totalChunks}...");

            var request = ImageService.DownloadImageDataRequest(planeCode, i);
            await _protocolSocket.SendAsync(request);

            var response = await _protocolSocket.ReceiveAsync();
            if (response.Command == CommandCode.Error)
            {
                Console.WriteLine($" Error: {Encoding.UTF8.GetString(response.Data)}");
                return false;
            }

            await FileHelper.WriteAsync(filePath, response.Data);
            Console.WriteLine(" OK");
        }
        return true;
    }

    private static void VerifyDownload(string filePath, long expectedSize)
    {
        var fileInfo = new FileInfo(filePath);
        if (fileInfo.Length == expectedSize)
            Console.WriteLine($"Image downloaded successfully to {filePath}");
        else
            Console.WriteLine($"WARNING: Downloaded size ({fileInfo.Length}) differs from expected ({expectedSize})");
    }

    private static void PromptToContinue()
    {
        Console.WriteLine("Hit enter to continue...");
        Console.ReadLine();
    }

    static FileInfo? GetImagePath()
    {
        string[] validExtensions = [".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".tiff"];

        Console.Write("Enter absolute path to image (or press Enter to skip): ");
        string? input = Console.ReadLine()?.Trim();

        if (string.IsNullOrEmpty(input))
            return null;

        if (!Path.IsPathRooted(input))
            Console.WriteLine("Error: Path must be absolute.");

        else if (!File.Exists(input))
            Console.WriteLine("Error: File not found.");

        else if (!validExtensions.Contains(Path.GetExtension(input).ToLowerInvariant()))
            Console.WriteLine($"Error: Not a valid image file. Accepted formats: {string.Join(", ", validExtensions)}");

        else if (Path.GetFileName(input).Contains('|'))
            Console.WriteLine("Error: File name cannot contain '|'");
        
        else
            return new FileInfo(input);

        return GetImagePath();
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
