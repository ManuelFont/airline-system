using System.Text;
using Protocol;

namespace ClientAdmin.Services;

internal static class AuthService
{
    public static ProtocolMessage LoginRequest()
    {
        var (username, password) = AskCredentials();
        var data = $"{username}|{password}";
        var dataBytes = Encoding.UTF8.GetBytes(data);
        return new ProtocolMessage("REQ", CommandCode.Login, dataBytes);
    }

    private static (string username, string password) AskCredentials()
    {
        string? username;
        string? password;

        do
        {
            Console.Write("Username: ");
            username = Console.ReadLine()?.Trim();
        } while (string.IsNullOrWhiteSpace(username) || username.Contains('|'));

        do
        {
            Console.Write("Password: ");
            password = Console.ReadLine();
        } while (string.IsNullOrWhiteSpace(password) || password.Contains('|'));

        return (username, password);
    }
}
