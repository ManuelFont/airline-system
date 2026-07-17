using System.Net;
using System.Text.Json;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using Server.Models;
using UserReportContracts;

namespace Server.Services;

internal static class UserReportRequestService
{
    public static async Task GetReportAsync(string username)
    {
        try
        {
            //We need HTTP 2.0 because UserReportServer port is configured to use HTTP/2
            using var client = CreateHttp2Client();
            using var response = await client.GetAsync(
                $"{GetUserReportServerAddress()}/reports/{username}");

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Status: {response.StatusCode} error");
                return;
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            var completedReport = TryDeserializeCompletedReport(responseBody);
            var reportPath = await SaveReportFileService.SaveAsync(completedReport);

            Console.WriteLine($"Report saved to: {reportPath}");
        }
        catch (Exception exception)
        {
            Console.WriteLine($"User report retrieval request failed: {exception.Message}");
        }
    }

    public static async Task CancelAsync()
    {
        try
        {
            using var channel = CreateGrpcChannel();
            var client = new UserReportService.UserReportServiceClient(channel);
            var response = await client.CancelReportAsync(new Empty());

            Console.WriteLine(response.Message);
        }
        catch (Exception exception)
        {
            Console.WriteLine($"User report cancellation request failed: {exception.Message}");
        }
    }

    public static async Task GenerateAsync()
    {
        try
        {
            var request = GeneratePdfRequestMapper.Map(
                AuthService.GetUsersSnapshot(),
                FlightService.GetFlightsSnapshot());

            using var channel = CreateGrpcChannel();
            var client = new UserReportService.UserReportServiceClient(channel);
            using var call = client.GeneratePDFs(request);

            await foreach (var response in call.ResponseStream.ReadAllAsync())
            {
                Console.WriteLine(response.Message);
            }
        }
        catch (Exception exception)
        {
            Console.WriteLine($"User report request failed: {exception.Message}");
        }
    }

    private static GetReportResponse TryDeserializeCompletedReport(string responseBody)
    {
        var completedReport = JsonSerializer.Deserialize<GetReportResponse>(
            responseBody,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (completedReport == null
            || string.IsNullOrWhiteSpace(completedReport.FileName)
            || completedReport.Content == null
            || completedReport.Content.Length == 0)
        {
            throw new InvalidOperationException("The report response is incomplete.");
        }

        return completedReport;
    }

    private static HttpClient CreateHttp2Client()
    {
        return new HttpClient
        {
            DefaultRequestVersion = HttpVersion.Version20,
            DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact
        };
    }

    private static GrpcChannel CreateGrpcChannel()
    {
        return GrpcChannel.ForAddress(GetUserReportServerAddress());
    }

    private static string GetUserReportServerAddress()
    {
        var host = Environment.GetEnvironmentVariable(ServerConfig.UserReportHostKey) ?? "localhost";
        var port = int.Parse(Environment.GetEnvironmentVariable(ServerConfig.UserReportPortKey) ?? "5200");

        return $"http://{host}:{port}";
    }
}
