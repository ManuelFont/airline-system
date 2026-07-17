using System.Text;
using Domain;
using Protocol;

namespace Server.Services;

internal static class AuthService
{
    private static List<User> _users = new();
    private static readonly object Locker = new object();

    public static List<User> GetUsersSnapshot()
    {
        lock (Locker)
        {
            return new List<User>(_users);
        }
    }

    public static (User?, ProtocolMessage) RegistrationResponse(ProtocolMessage request)
    {
        try
        {
            var (username, password) = ParseCredentials(Encoding.UTF8.GetString(request.Data));
            var user = CreateUser(username, password);
            return (user, BuildResponse(CommandCode.Register, "User created successfully"));
        }
        catch (ArgumentException ex)
        {
            return (null, BuildResponse(CommandCode.Error, ex.Message));
        }
    }

    public static (User?, ProtocolMessage) LoginResponse(ProtocolMessage request)
    {
        try
        {
            var (username, password) = ParseCredentials(Encoding.UTF8.GetString(request.Data));
            
            User user;
            lock(Locker)
            {
                if (!UserExists(username))
                {
                    throw new ArgumentException("User does not exist");
                }
                user = _users.First(u => u.Username == username);
            }
            if (user.Password != password)
            {
                throw new ArgumentException("Incorrect password");
            }
            return (user, BuildResponse(CommandCode.Login, "User logged in successfully"));
        }
        catch (ArgumentException ex)
        {
            return (null, BuildResponse(CommandCode.Error, ex.Message));
        }
    }

    private static (string username, string password) ParseCredentials(string data)
    {
        string[] parts = data.Split('|');

        if (parts.Length < 2 || string.IsNullOrEmpty(parts[0]) || string.IsNullOrEmpty(parts[1]))
            throw new ArgumentException("Incorrect credentials transmission");

        return (parts[0], parts[1]);
    }

    private static User CreateUser(string username, string password)
    {
        var user = new User(username, password);

        lock (Locker)
        {
            if (UserExists(username))
                throw new ArgumentException($"User '{username}' already exists");

            _users.Add(user);
        }

        return user;
    }

    private static bool UserExists(string username)
    {
        return _users.Any(u => u.Username == username);
    }
    

    private static ProtocolMessage BuildResponse(string code, string message) =>
        new("RES", code, Encoding.UTF8.GetBytes(message));
}
