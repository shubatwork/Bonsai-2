using Bonsai.Services;

namespace Bonsai.Workers;

public class DataAnalysisWorkerService01 : BackgroundService
{
    private readonly IDataAnalysisService _dataAnalysisService;

    public DataAnalysisWorkerService01(IDataAnalysisService dataAnalysisService)
    {
        _dataAnalysisService = dataAnalysisService;
    }

    private const int GeneralDelay = 1000 * 60 * 10;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            Thread.Sleep(1234);
            await DoBackupAsync().ConfigureAwait(false);
            await Task.Delay(GeneralDelay, stoppingToken);
        }
    }

    private async Task<string?> DoBackupAsync()
    {
        await _dataAnalysisService.CreatePositions(Binance.Net.Enums.KlineInterval.OneMinute).ConfigureAwait(false);
        return null;
    }
}