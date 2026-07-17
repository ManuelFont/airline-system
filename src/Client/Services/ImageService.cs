using System.Text;
using Common;
using Protocol;

namespace Client.Services;

internal static class ImageService
{
    public static ProtocolMessage UploadImageInfoRequest(FileInfo file, string planeCode)
    {
        var message = $"{planeCode}|{planeCode}.{file.Extension}|{file.Length}";
        var messageBytes = Encoding.UTF8.GetBytes(message);
        var request = new ProtocolMessage("REQ", CommandCode.UploadImageInfo, messageBytes);
        return request;
    }
    
    public static async Task<List<ProtocolMessage>> UploadImageDataRequestsAsync(FileInfo file)
    {
        List<ProtocolMessage> protocolMessages = [];
        long totalChunks = FileHelper.ChunkNumber(file.Length);
        int bytesRead = 0;
        
        Console.WriteLine($"We need to send {totalChunks} chunks");

        for (int chunkIndex = 1; chunkIndex <= totalChunks; chunkIndex++)
        {
            bool isLastChunk = chunkIndex == totalChunks;
            byte[] chunk;

            if (!isLastChunk)
            {
                chunk = await FileHelper.ReadAsync(file, FileHelper.ChunkSize, bytesRead);
                bytesRead += FileHelper.ChunkSize;
            }
            else
            {
                int remainingSize = (int)(file.Length - bytesRead);
                chunk = await FileHelper.ReadAsync(file, remainingSize, bytesRead);
                bytesRead += remainingSize;
            }
            
            protocolMessages.Add(new ProtocolMessage("REQ", CommandCode.UploadImageData, chunk));
        }
        
        return protocolMessages;
    }

    public static ProtocolMessage DownloadImageInfoRequest(string planeCode)
    {
        var messageBytes = Encoding.UTF8.GetBytes(planeCode);
        return new ProtocolMessage("REQ", CommandCode.DownloadImage, messageBytes);
    }

    public static ProtocolMessage DownloadImageDataRequest(string planeCode, int chunkIndex)
    {
        var message = $"{planeCode}|{chunkIndex}";
        var messageBytes = Encoding.UTF8.GetBytes(message);
        return new ProtocolMessage("REQ", CommandCode.DownloadImageData, messageBytes);
    }
}
