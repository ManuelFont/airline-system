namespace Server.Models;

internal class GetReportResponse
{
    public string FileName { get; set; } = string.Empty;
    public byte[] Content { get; set; } = [];
}
