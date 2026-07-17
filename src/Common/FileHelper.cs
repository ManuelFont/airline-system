namespace Common;

public static class FileHelper
{
    public const int ChunkSize = 65536;

    public static async Task<byte[]> ReadAsync(FileInfo file, int size, int position)
    {
        byte[] buffer = new byte[size];
        using (FileStream fs = file.OpenRead())
        {
            fs.Position = position;
            int offset = 0;
            while (offset < size)
            {
                int bytesRead = await fs.ReadAsync(buffer, offset, size - offset);
                offset += bytesRead;
            }
        }

        return buffer;
    }
    
    public static async Task WriteAsync(string filename, byte[] buffer)
    {
        FileMode fileMode = File.Exists(filename) ? FileMode.Append : FileMode.Create;
        using (FileStream fs = new FileStream(filename, fileMode))
        {
            await fs.WriteAsync(buffer, 0, buffer.Length);
        }
    }

    public static long ChunkNumber(long filesize)
    {
        long chunkParts = filesize / ChunkSize;

        if (chunkParts * ChunkSize == filesize) return chunkParts;
        
        return chunkParts +1;
    }
}