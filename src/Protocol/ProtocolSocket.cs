using System.Net.Sockets;
using System.Net;
using System.Text;

namespace Protocol;

/// <summary>
/// Handles low-level socket communication using the protocol message format.
/// Abstracts away buffering and serialization complexity.
/// </summary>
public class ProtocolSocket
{
    private readonly Socket _socket;

    public ProtocolSocket(Socket socket)
    {
        _socket = socket;
    }

    /// <summary>
    /// Sends a protocol message across the socket.
    /// </summary>
    public async Task SendAsync(ProtocolMessage message)
    {
        byte[] serialized = message.Serialize();
        await SendBytesAsync(serialized);
    }

    /// <summary>
    /// Receives a protocol message from the socket.
    /// </summary>
    public async Task<ProtocolMessage> ReceiveAsync()
    {
        byte[] header = await ReceiveBytesAsync(ProtocolMessage.HeaderLength);

        byte[] lengthBytes = new byte[ProtocolMessage.LengthFieldLength];
        Array.Copy(header, ProtocolMessage.DirectionLength + ProtocolMessage.CommandLength, 
                   lengthBytes, 0, ProtocolMessage.LengthFieldLength);

        int dataLength = int.Parse(Encoding.UTF8.GetString(lengthBytes));

        byte[] data = await ReceiveBytesAsync(dataLength);

        byte[] fullMessage = new byte[header.Length + data.Length];
        Array.Copy(header, fullMessage, header.Length);
        Array.Copy(data, 0, fullMessage, header.Length, data.Length);

        return ProtocolMessage.Deserialize(fullMessage);
    }

    private async Task SendBytesAsync(byte[] buffer)
    {
        int totalSent = 0;
        while (totalSent < buffer.Length)
        {
            int sent = await _socket.SendAsync(buffer.AsMemory(totalSent, buffer.Length - totalSent), SocketFlags.None);
            if (sent == 0)
            {
                _socket.Shutdown(SocketShutdown.Both);
                _socket.Close();
                return;
            }
            totalSent += sent;
        }
    }

    private async Task<byte[]> ReceiveBytesAsync(int count)
    {
        byte[] buffer = new byte[count];
        int totalReceived = 0;

        while (totalReceived < count)
        {
            int received = await _socket.ReceiveAsync(buffer.AsMemory(totalReceived, count - totalReceived), SocketFlags.None);
            if (received == 0) 
                throw new SocketException((int)SocketError.ConnectionReset);

            totalReceived += received;
        }

        return buffer;
    }

    public EndPoint? RemoteEndPoint => _socket.RemoteEndPoint;
}
