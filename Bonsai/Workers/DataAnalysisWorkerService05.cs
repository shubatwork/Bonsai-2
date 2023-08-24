using Bonsai.Services;

namespace Bonsai.Workers;

public class DataAnalysisWorkerService05 : BackgroundService
{
    private readonly IDataAnalysisService _dataAnalysisService;

    public DataAnalysisWorkerService05(IDataAnalysisService dataAnalysisService)
    {
        _dataAnalysisService = dataAnalysisService;
    }

    private const int GeneralDelay = 1000 * 60 * 5;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await DoBackupAsync().ConfigureAwait(false);
            await Task.Delay(GeneralDelay, stoppingToken);
        }
    }

    private async Task<string?> DoBackupAsync()
    {
        await _dataAnalysisService.CreatePositions(Binance.Net.Enums.KlineInterval.FiveMinutes).ConfigureAwait(false);
        return null;
    }
}