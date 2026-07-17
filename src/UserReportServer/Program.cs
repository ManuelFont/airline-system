using UserReportServer;
using UserReportServer.Services;

var port = int.Parse(Environment.GetEnvironmentVariable(UserReportServerConfig.UserReportPortKey) ?? "5200");
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddGrpc();

var application = builder.Build();

//Map gRPC
application.MapGrpcService<UserReportGrpcService>();

//Map REST API
application.MapGet("/reports/{username}", async (string username) =>
{
    var fileName = $"report_{username}.pdf";
    var content = await ReportService.GetCompletedReportAsync(username);

    if (content == null)
    {
        return Results.NotFound();
    }

    return Results.Ok(new { fileName, content });
});

//Run app
application.Run($"http://0.0.0.0:{port}");
