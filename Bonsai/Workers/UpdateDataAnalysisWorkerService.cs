using Bonsai.Services;

namespace Bonsai.Workers;

public class UpdateDataAnalysisWorkerService : BackgroundService
{
    private readonly IDataAnalysisService _dataAnalysisService;

    public UpdateDataAnalysisWorkerService(IDataAnalysisService dataAnalysisService)
    {
        _dataAnalysisService = dataAnalysisService;
    }

    private const int GeneralDelay = 1000 * 60 * 5;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Thread.Sleep(2000);
        while (!stoppingToken.IsCancellationRequested)
        {
            await DoBackupAsync().ConfigureAwait(false);
            await Task.Delay(GeneralDelay, stoppingToken);
        }
    }

    private async Task<string?> DoBackupAsync()
    {
        await _dataAnalysisService.UpdatePositions().ConfigureAwait(false);
        return null;
    }
}