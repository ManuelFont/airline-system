using System.Globalization;
using System.Text;
using PdfOrtReportService;
using UserReportServer.Models;

namespace UserReportServer.Services;

internal static class ReportService
{
    private static readonly Lock Locker = new();
    private static bool _isGenerating;
    private static CancellationTokenSource? _tokenSource;

    public static bool TryCancelReport()
    {
        lock (Locker)
        {
            if (!_isGenerating || _tokenSource == null)
            {
                return false;
            }

            _tokenSource.Cancel();
            return true;
        }
    }

    public static bool TryBeginGeneration()
    {
        lock (Locker)
        {
            if (_isGenerating)
            {
                return false;
            }

            _isGenerating = true;
            _tokenSource = new CancellationTokenSource();
            return true;
        }
    }

    public static void ReleaseGeneration()
    {
        CancellationTokenSource? cts;

        lock (Locker)
        {
            cts = _tokenSource;
        }

        if (cts != null)
        {
            CleanupGeneration(cts);
        }
    }

    public static async Task<byte[]?> GetCompletedReportAsync(string username)
    {
        var reportPath = GetReportPath(username);

        try
        {
            return await File.ReadAllBytesAsync(reportPath);
        }
        catch (FileNotFoundException)
        {
            return null;
        }
        catch (DirectoryNotFoundException)
        {
            return null;
        }
    }

    public static async Task GenerateAllPdfsAsync(
        IReadOnlyList<UserReportModel> reports,
        UserReportGrpcService grpcService)
    {
        CancellationTokenSource cts;

        lock (Locker)
        {
            cts = _tokenSource!;
        }

        var token = cts.Token;

        Console.WriteLine("Generating PDF reports... (type 'cancel' to abort)");

        try
        {
            var tasks = new List<Task>();

            foreach (var report in reports)
            {
                tasks.Add(Task.Run(
                    () => GeneratePdfAsync(report, token, grpcService),
                    token));
            }

            await Task.WhenAll(tasks);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Report generation cancelled.");
        }
        finally
        {
            CleanupGeneration(cts);
        }
    }

    private static async Task GeneratePdfAsync(
        UserReportModel report,
        CancellationToken token,
        UserReportGrpcService grpcService)
    {
        try
        {
            var html = BuildHtmlBody(report, token);
            var pdfService = new PdfReportService();
            var pdfBytes = await pdfService.GeneratePdfAsync(html, token);

            var outputPath = await SavePdfAsync(pdfBytes, report.Username, token);
            Console.WriteLine($"Report saved to: {outputPath}");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Report cancelled.");
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Report for user '{report.Username}' failed: {ex.Message}");
        }
        finally
        {
            await grpcService.SendProgressUpdate();
        }
    }

    private static string BuildHtmlBody(UserReportModel report, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        var content = GenerateUserReport(report);
        var htmlFragment = $"<pre>{content}</pre>";

        var body = new StringBuilder();
        AppendHeader(body);
        AppendMethodology(body);

        body.Append(htmlFragment);

        return BuildHtmlDocument(body.ToString());
    }

    private static void CleanupGeneration(CancellationTokenSource cts)
    {
        lock (Locker)
        {
            _tokenSource = null;
            _isGenerating = false;
        }

        cts.Dispose();
    }

    private static void AppendHeader(StringBuilder sb)
    {
        sb.Append("<pre>");
        sb.Append('=', 79);
        sb.AppendLine();
        sb.AppendLine($"  USERS REPORT — Generated: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
        sb.Append('=', 79);
        sb.AppendLine();
        sb.AppendLine("</pre>");
        sb.AppendLine();
    }

    private static void AppendMethodology(StringBuilder sb)
    {
        sb.AppendLine("<pre>");
        sb.AppendLine("--- Methodology ---");
        sb.AppendLine("  * All tickets (active and cancelled) are included in all calculations.");
        sb.AppendLine("  * Prices reflect the flight price at the moment of purchase (PriceAtPurchase), not the current flight price.");
        sb.AppendLine("  * Image sizes are read from disk at report time; a missing or inaccessible file is reported as 0 bytes.");
        sb.AppendLine("  * This report is a point-in-time snapshot generated at the date/time shown above.");
        sb.AppendLine();
        sb.Append('=', 79);
        sb.AppendLine();
        sb.AppendLine("</pre>");
        sb.AppendLine();
    }

    private static string BuildHtmlDocument(string bodyContent)
    {
        return $"<!DOCTYPE html>\n<html>\n<head>\n<meta charset=\"utf-8\">\n<style>\n  body {{ font-family: monospace; white-space: pre-wrap; font-size: 12px; }}\n</style>\n</head>\n<body>\n{bodyContent}\n</body>\n</html>";
    }

    private static async Task<string> SavePdfAsync(byte[] pdfBytes, string username, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        var reportsDir = Environment.GetEnvironmentVariable(UserReportServerConfig.ReportsPathKey) ?? Path.Combine(AppContext.BaseDirectory, "reports");
        Directory.CreateDirectory(reportsDir);
        var filePath = GetReportPath(username);
        await File.WriteAllBytesAsync(filePath, pdfBytes, token);
        return filePath;
    }

    private static string GetReportPath(string username)
    {
        var reportsDirectory = Environment.GetEnvironmentVariable(UserReportServerConfig.ReportsPathKey)
            ?? Path.Combine(AppContext.BaseDirectory, "reports");
        var fileName = $"report_{username}.pdf";

        return Path.Combine(reportsDirectory, fileName);
    }

    private static string GenerateUserReport(UserReportModel report)
    {
        var sb = new StringBuilder();

        sb.AppendLine(new string('=', 79));
        sb.AppendLine($"--- {report.Username} --- ({report.CreatedFlights.Count} flights created, {report.TicketStats.PurchasedCount} tickets purchased)");
        sb.AppendLine();

        AppendTicketStats(sb, report.TicketStats);
        AppendCreatedFlights(sb, report.CreatedFlights);
        AppendImageSummary(sb, report);

        sb.AppendLine(new string('=', 79));
        sb.AppendLine();

        return sb.ToString();
    }

    private static void AppendTicketStats(StringBuilder sb, TicketStatsModel stats)
    {
        sb.AppendLine("  [Ticket Stats]");

        if (stats.PurchasedCount == 0)
        {
            sb.AppendLine("    Tickets purchased: 0 (0 cancelled)");
        }
        else
        {
            sb.AppendLine($"    Tickets purchased: {stats.PurchasedCount} ({stats.CancelledCount} cancelled)");
            sb.AppendLine($"    Average ticket cost: {FormatPrice(stats.AveragePrice!.Value)}");
            sb.AppendLine($"    Cheapest ticket: {FormatPrice(stats.CheapestPrice!.Value)}");
            sb.AppendLine($"    Most expensive ticket: {FormatPrice(stats.MostExpensivePrice!.Value)}");
        }

        sb.AppendLine();
    }

    private static void AppendCreatedFlights(StringBuilder sb, IReadOnlyList<FlightReportModel> createdFlights)
    {
        sb.AppendLine("  [Created Flights]");

        if (createdFlights.Count == 0)
        {
            sb.AppendLine("    (none)");
        }
        else
        {
            foreach (var flight in createdFlights)
            {
                sb.AppendLine($"    [Flight {flight.Code}]");

                var displayLines = flight.ToDisplayString().Split('\n');
                foreach (var line in displayLines)
                {
                    sb.AppendLine($"      {line}");
                }

                AppendImageFileInfo(sb, flight.Image);
                AppendImageElement(sb, flight.Image);
                sb.AppendLine();
            }
        }

        sb.AppendLine();
    }

    private static void AppendImageFileInfo(StringBuilder sb, FlightImageReportModel? image)
    {
        if (image == null)
        {
            sb.AppendLine("      Image file: None");
            return;
        }

        sb.AppendLine($"      Image file: {image.FileName} ({FormatSize(image.Size)})");
    }

    private static void AppendImageElement(StringBuilder sb, FlightImageReportModel? image)
    {
        if (image == null || !image.HasContent)
            return;

        sb.AppendLine($"      <img src=\"{image.ToDataUri()}\" style=\"max-width:300px;display:block;margin:4px 0\">");
    }

    private static void AppendImageSummary(StringBuilder sb, UserReportModel report)
    {
        sb.AppendLine("  [Image Summary]");

        sb.AppendLine($"    Flights with images: {report.FlightsWithImages} of {report.CreatedFlights.Count}");
        sb.AppendLine($"    Total image size: {FormatSize(report.TotalImageSize)}");

        if (report.AverageImageSize != null)
            sb.AppendLine($"    Average image size: {FormatSize(report.AverageImageSize.Value)}");
        else
            sb.AppendLine("    Average image size: N/A");

        sb.AppendLine();
    }

    private static string FormatPrice(decimal price)
        => price.ToString("$#,##0.00", CultureInfo.InvariantCulture);

    private static string FormatSize(long bytes)
    {
        if (bytes < 1024)
            return $"{bytes} bytes";

        double kb = bytes / 1024.0;
        if (kb < 1024.0)
            return $"{kb:F2} KB";

        double mb = kb / 1024.0;
        if (mb < 1024.0)
            return $"{mb:F2} MB";

        double gb = mb / 1024.0;
        return $"{gb:F2} GB";
    }

}
