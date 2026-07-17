using System.Globalization;
using System.Text;
using FlightReportQueue;
using PdfOrtReportService;

namespace FlightReportServer.Services;

internal static class FlightReportService
{
    private const string ReportsPathKey = "REPORTS_PATH";

    public static async Task GenerateAsync(FlightCancellationReportRequest request)
    {
        var reportsDirectory = Environment.GetEnvironmentVariable(ReportsPathKey)
            ?? Path.Combine(AppContext.BaseDirectory, "reports");
        Directory.CreateDirectory(reportsDirectory);

        var reportPath = Path.Combine(
            reportsDirectory,
            $"flight-cancellation-{request.ReportId}.pdf");

        if (File.Exists(reportPath))
        {
            Console.WriteLine(
                $"Flight cancellation report will not be generated because it already exists in: {reportPath}");
            return;
        }

        var html = BuildHtml(request);
        var pdfService = new PdfReportService();
        var pdfBytes = await pdfService.GeneratePdfAsync(html);
        await File.WriteAllBytesAsync(reportPath, pdfBytes);

        Console.WriteLine($"Flight cancellation report saved to: {reportPath}");
        await NotifyWebhookAsync(request);
    }

    private static async Task NotifyWebhookAsync(FlightCancellationReportRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.WebhookUrl))
            return;

        try
        {
            var httpClient = new HttpClient();
            var jsonPayload = "{\"reportId\":\"" + request.ReportId + "\"}";
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            Console.WriteLine($"Sending report notification to webhook: {request.WebhookUrl}");
            var response = await httpClient.PostAsync(request.WebhookUrl, content);
            var responseText = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Webhook notification sent. Status: {response.StatusCode}");
                return;
            }

            Console.WriteLine(
                $"Webhook notification failed. Status: {response.StatusCode}. Response: {responseText}");
        }
        catch (Exception exception)
        {
            Console.WriteLine($"Webhook notification failed: {exception.Message}");
        }
    }

    private static string BuildHtml(FlightCancellationReportRequest request)
    {
        var cancelledAtUtcMinusThree = request.CancelledAt.AddHours(-3);
        var body = new StringBuilder();
        body.AppendLine("<pre>");
        body.AppendLine(new string('=', 79));
        body.AppendLine("FLIGHT CANCELLATION REPORT");
        body.AppendLine(new string('=', 79));
        body.AppendLine($"Report ID: {request.ReportId}");
        body.AppendLine();
        body.AppendLine("[Flight Information]");
        body.AppendLine($"Code: {request.FlightCode}");
        body.AppendLine($"Origin: {request.Origin}");
        body.AppendLine($"Destination: {request.Destination}");
        body.AppendLine($"Date: {request.FlightDate:dd/MM/yyyy}");
        body.AppendLine($"Seats: {request.Seats}");
        body.AppendLine($"Duration: {request.Duration:hh\\:mm}");
        body.AppendLine($"Price: {request.Price.ToString("$#,##0.00", CultureInfo.InvariantCulture)}");
        body.AppendLine($"Owner: {request.FlightOwnerUsername}");
        body.AppendLine();
        body.AppendLine("[Cancellation Information]");
        body.AppendLine(
            $"Cancelled at: {cancelledAtUtcMinusThree:dd/MM/yyyy HH:mm:ss} UTC-03:00");
        body.AppendLine($"Reason: {request.CancellationReason}");
        body.AppendLine($"Cancelled by: {request.CancelledByUsername}");
        body.AppendLine(new string('=', 79));
        body.AppendLine("</pre>");

        return $"<!DOCTYPE html>\n<html>\n<head>\n<meta charset=\"utf-8\">\n<style>\n  body {{ font-family: monospace; white-space: pre-wrap; font-size: 12px; }}\n</style>\n</head>\n<body>\n{body}\n</body>\n</html>";
    }
}
