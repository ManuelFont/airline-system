using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using UserReportContracts;

namespace UserReportServer.Services;

public class UserReportGrpcService : UserReportService.UserReportServiceBase
{
    //Writes to the same gRPC response stream must be serialized or else it will throw
    private readonly SemaphoreSlim _responseSemaphore = new(1, 1);
    private int _totalReportsNeeded;
    private int _reportsCreated;
    private IServerStreamWriter<GeneratePdfResponse> _responseStream = null!;

    public override Task<GeneratePdfResponse> CancelReport(
        Empty request,
        ServerCallContext context)
    {
        var message = "No report in progress";

        if (ReportService.TryCancelReport())
        {
            message = "report cancelled";
        }

        return Task.FromResult(new GeneratePdfResponse
        {
            Message = message
        });
    }

    public override async Task GeneratePDFs(
        GeneratePdfRequest request,
        IServerStreamWriter<GeneratePdfResponse> responseStream,
        ServerCallContext context)
    {
        var reports = GeneratePdfRequestMapper.Map(request);
        _totalReportsNeeded = reports.Count;
        _responseStream = responseStream;

        if (_totalReportsNeeded == 0)
        {
            await SendMessageAsync("There are no reports to be created");
            return;
        }

        if (!ReportService.TryBeginGeneration())
        {
            await SendMessageAsync("generation in progress (\"cancel\" to omit)");
            return;
        }

        try
        {
            await SendMessageAsync($"Progress: 0/{_totalReportsNeeded}");
        }
        catch
        {
            ReportService.ReleaseGeneration();
            throw;
        }

        await ReportService.GenerateAllPdfsAsync(reports, this);
    }

    public async Task SendProgressUpdate()
    {
        await _responseSemaphore.WaitAsync();

        try
        {
            _reportsCreated++;
            await WriteMessageAsync($"Progress: {_reportsCreated}/{_totalReportsNeeded}");
        }
        finally
        {
            _responseSemaphore.Release();
        }
    }

    private async Task SendMessageAsync(string message)
    {
        await _responseSemaphore.WaitAsync();

        try
        {
            await WriteMessageAsync(message);
        }
        finally
        {
            _responseSemaphore.Release();
        }
    }

    private Task WriteMessageAsync(string message)
    {
        return _responseStream.WriteAsync(new GeneratePdfResponse
        {
            Message = message
        });
    }
}
