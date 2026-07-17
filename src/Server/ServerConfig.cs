namespace Server;

// ── Clase de configuración del servidor ─────────────────────────────
// Centraliza los nombres de las variables de entorno que el servidor
// necesita leer.
// Cada constante es la clave que se pasa a Environment.GetEnvironmentVariable().
internal static class ServerConfig
{
    // Puerto donde escuchará el servidor
    public const string ServerPortKey = "SERVER_PORT";
    public const string WebSocketPortKey = "WEBSOCKET_PORT";
    public const string UserReportHostKey = "USER_REPORT_HOST";
    public const string UserReportPortKey = "USER_REPORT_PORT";

    // Ruta donde se guardarán las imágenes subidas por los clientes
    public const string UploadsPathKey = "UPLOADS_PATH";
}
