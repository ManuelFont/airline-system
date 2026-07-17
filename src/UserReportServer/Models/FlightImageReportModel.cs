namespace UserReportServer.Models;

internal sealed class FlightImageReportModel
{
    public string FileName { get; }
    public byte[] Content => _content.ToArray();
    public long Size => _content.LongLength;
    public bool HasContent => _content.Length > 0;

    private readonly byte[] _content;

    public FlightImageReportModel(string fileName, byte[] content)
    {
        FileName = fileName;
        _content = content.ToArray();
    }

    public string ToDataUri()
    {
        var extension = Path.GetExtension(FileName).ToLowerInvariant();
        var mediaType = "image/jpeg";

        if (extension == ".png")
            mediaType = "image/png";

        return $"data:{mediaType};base64,{Convert.ToBase64String(_content)}";
    }
}
