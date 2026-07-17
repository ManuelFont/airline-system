using Server.Models;

namespace Server.Services;

internal static class SaveReportFileService
{
    public static async Task<string> SaveAsync(GetReportResponse completedReport)
    {
        var fileName = GetValidFileName(completedReport.FileName);
        var reportsDirectory = Path.Combine(AppContext.BaseDirectory, "reports");
        Directory.CreateDirectory(reportsDirectory);

        var reportPath = Path.Combine(reportsDirectory, fileName);
        await File.WriteAllBytesAsync(reportPath, completedReport.Content);

        return reportPath;
    }

    private static string GetValidFileName(string responseFileName)
    {
        var fileName = Path.GetFileName(responseFileName);

        if (string.IsNullOrWhiteSpace(fileName) || fileName == "." || fileName == "..")
        {
            throw new InvalidOperationException("The report file name is invalid.");
        }

        return fileName;
    }
}
