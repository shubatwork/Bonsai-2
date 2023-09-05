using Bonsai.Services;

namespace Bonsai.Workers;

public class DataAnalysisWorkerService60 : BackgroundService
{
    private readonly IDataAnalysisService _dataAnalysisService;

    public DataAnalysisWorkerService60(IDataAnalysisService dataAnalysisService)
    {
        _dataAnalysisService = dataAnalysisService;
    }

    private const int GeneralDelay = 1000 * 60 * 1;

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
        await _dataAnalysisService.CreatePositionsRSI(Binance.Net.Enums.KlineInterval.OneHour).ConfigureAwait(false);
        return null;
    }
}