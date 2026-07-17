using System.Text;

namespace Protocol;

/// <summary>
/// Represents a protocol message with Direction, Command, Length, and Data fields.
/// Protocol Format:
/// - Direction: 3 bytes (e.g., "001")
/// - Command: 3 bytes (e.g., "010") 
/// - Length: 5 bytes (e.g., "00142")
/// - Data: Variable length payload
/// </summary>
public class ProtocolMessage
{
    public const int DirectionLength = 3;
    public const int CommandLength = 3;
    public const int LengthFieldLength = 5;
    public const int HeaderLength = DirectionLength + CommandLength + LengthFieldLength;

    public string Direction { get; set; }
    public string Command { get; set; }
    public byte[] Data { get; set; }

    public ProtocolMessage(string direction, string command, byte[] data)
    {
        Direction = PadField(direction, DirectionLength);
        Command = PadField(command, CommandLength);
        Data = data;
    }

    /// <summary>
    /// Pads a field with leading zeros to reach the target length.
    /// </summary>
    private static string PadField(string value, int length)
    {
        return value.PadLeft(length, '0');
    }

    /// <summary>
    /// Serializes the message to bytes according to the protocol format.
    /// </summary>
    public byte[] Serialize()
    {
        byte[] dirBytes = Encoding.UTF8.GetBytes(Direction);
        byte[] cmdBytes = Encoding.UTF8.GetBytes(Command);
        byte[] lenBytes = Encoding.UTF8.GetBytes(Data.Length.ToString().PadLeft(LengthFieldLength, '0'));

        return dirBytes.Concat(cmdBytes).Concat(lenBytes).Concat(Data).ToArray();
    }

    /// <summary>
    /// Deserializes a protocol message from bytes.
    /// </summary>
    public static ProtocolMessage Deserialize(byte[] buffer)
    {
        string direction = Encoding.UTF8.GetString(buffer, 0, DirectionLength);
        string command   = Encoding.UTF8.GetString(buffer, DirectionLength, CommandLength);
        string lengthStr = Encoding.UTF8.GetString(buffer, DirectionLength + CommandLength, LengthFieldLength);

        int dataLength = int.Parse(lengthStr);
        byte[] data = buffer.Skip(HeaderLength).Take(dataLength).ToArray();

        return new ProtocolMessage(direction, command, data);
    }

    public override string ToString()
    {
        return $"[Dir: {Direction}, Cmd: {Command}, DataLen: {Data.Length}]";
    }
}
