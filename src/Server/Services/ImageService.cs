using System.Text;
using Common;
using Domain;
using Protocol;

namespace Server.Services;

internal static class ImageService
{
    private static readonly AsyncLocal<UploadState?> UploadState = new();
    
    public static ProtocolMessage UploadImageInfoResponse(ProtocolMessage request, User user)
    {
        try
        {
            var (code, name, size) = ParseInfo(Encoding.UTF8.GetString(request.Data));
            long sizeNumber = long.Parse(size);
            
            if (sizeNumber > 524288000)
                throw new ArgumentException("File size is too big");

            var flight = FlightService.FindFlight(code);
            if(flight == null)
                throw new ArgumentException("There is no plane with that code");
            
            if(flight.Owner != user)
                throw new ArgumentException("You did not create this flight");

            var uploadsPath = Environment.GetEnvironmentVariable(ServerConfig.UploadsPathKey) ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "uploads");
            Directory.CreateDirectory(uploadsPath);
            UploadState.Value = new UploadState { ImageName = name, ImageSize = sizeNumber };
            var filePath = Path.Combine(uploadsPath, name);
            flight.Update(flight.Origin, flight.Destination, flight.Date.ToString("dd/MM/yyyy"), flight.Seats, flight.Duration.ToString("hh\\:mm"), flight.Price, filePath);
            return BuildResponse(CommandCode.UploadImageInfo, "Image info uploaded");
        }
        catch (ArgumentException ex)
        {
            return BuildResponse(CommandCode.Error, ex.Message);
        }
    }

    public static async Task<ProtocolMessage> UploadImageDataResponseAsync(ProtocolMessage request)
    {
        try
        {
            var uploadsPath = Environment.GetEnvironmentVariable(ServerConfig.UploadsPathKey) ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "uploads");
            var state = UploadState.Value;
            if (state == null)
                return BuildResponse(CommandCode.Error, "No upload in progress");

            Directory.CreateDirectory(uploadsPath);
            var filePath = Path.Combine(uploadsPath, state.ImageName);

            if (state.ImageSizeReceived == 0 && File.Exists(filePath))
                File.Delete(filePath);

            state.ImageSizeReceived += request.Data.Length;
        
            await FileHelper.WriteAsync(filePath, request.Data);

            if (state.ImageSizeReceived == state.ImageSize)
            {
                UploadState.Value = null;
                return BuildResponse(CommandCode.UploadImageData, "Image received completely!");
            }
        
            return BuildResponse(CommandCode.UploadImageData, "Chunk received");
        }
        catch (Exception ex)
        {
            return BuildResponse(CommandCode.Error, ex.Message);
        }
    }

    public static ProtocolMessage DownloadImageInfoResponse(ProtocolMessage request)
    {
        try
        {
            var planeCode = Encoding.UTF8.GetString(request.Data).Trim();
            var flight = FlightService.FindFlight(planeCode);
            if (flight == null)
                throw new ArgumentException("There is no plane with that code");

            if (string.IsNullOrEmpty(flight.ImagePath))
                throw new ArgumentException("This flight has no image");

            var fileInfo = new FileInfo(flight.ImagePath);
            if (!fileInfo.Exists)
                throw new ArgumentException("Image file not found on server");

            var fileName = Path.GetFileName(flight.ImagePath);
            var fileSize = fileInfo.Length;
            var totalChunks = FileHelper.ChunkNumber(fileSize);

            var data = $"{fileName}|{fileSize}|{totalChunks}";
            return BuildResponse(CommandCode.DownloadImage, data);
        }
        catch (ArgumentException ex)
        {
            return BuildResponse(CommandCode.Error, ex.Message);
        }
    }

    public static async Task<ProtocolMessage> DownloadImageDataResponse(ProtocolMessage request)
    {
        try
        {
            var data = Encoding.UTF8.GetString(request.Data);
            var parts = data.Split('|');
            var planeCode = parts[0];
            var chunkIndex = int.Parse(parts[1]);

            var flight = FlightService.FindFlight(planeCode);
            if (flight == null)
                throw new ArgumentException("There is no plane with that code");

            if (string.IsNullOrEmpty(flight.ImagePath))
                throw new ArgumentException("This flight has no image");

            var fileInfo = new FileInfo(flight.ImagePath);
            if (!fileInfo.Exists)
                throw new ArgumentException("Image file not found on server");

            var totalChunks = FileHelper.ChunkNumber(fileInfo.Length);
            if (chunkIndex < 0 || chunkIndex >= totalChunks)
                throw new ArgumentException("Invalid chunk index");

            var byteOffset = (long)chunkIndex * FileHelper.ChunkSize;
            var remaining = fileInfo.Length - byteOffset;
            var chunkSize = (int)Math.Min(remaining, FileHelper.ChunkSize);

            var chunk = await FileHelper.ReadAsync(fileInfo, chunkSize, (int)byteOffset);

            return new ProtocolMessage("RES", CommandCode.DownloadImageData, chunk);
        }
        catch (ArgumentException ex)
        {
            return BuildResponse(CommandCode.Error, ex.Message);
        }
        catch (Exception ex)
        {
            return BuildResponse(CommandCode.Error, ex.Message);
        }
    }

    private static (string code, string name, string size) ParseInfo(string data)
    {
        var parts = data.Split('|');
        return (
            code: parts[0],
            name: parts[1],
            size: parts[2]
        );
    }
    
    private static ProtocolMessage BuildResponse(string code, string message)
        => new ProtocolMessage("RES", code, Encoding.UTF8.GetBytes(message));
}
