using System.Text;
using Protocol;

namespace Client.Services;

internal static class AuthService
{
    public static ProtocolMessage RegistrationRequest()
    {
        var dataBytes = AskBytesCredentials();
        var request = new ProtocolMessage("REQ", CommandCode.Register, dataBytes);

        return request;
    }

    public static ProtocolMessage LoginRequest()
    {
        var dataBytes = AskBytesCredentials();
        var request = new ProtocolMessage("REQ", CommandCode.Login, dataBytes);

        return request;
    }

    private static byte[] AskBytesCredentials()
    {
        string username, password;
        (username, password) = AskCredentials();
        var data = $"{username}|{password}";
        return Encoding.UTF8.GetBytes(data);
    }

    private static (string username, string password) AskCredentials()
    {
        string? username, password;

        do
        {
            Console.Write("Username: ");
            username = Console.ReadLine();
        } while (string.IsNullOrWhiteSpace(username) || username.Contains('|'));

        do
        {
            Console.Write("Password: ");
            password = Console.ReadLine();
        } while (string.IsNullOrWhiteSpace(password) || password.Contains('|'));

        return (username, password);
    }
}
