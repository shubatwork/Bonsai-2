using Bonsai.Services;
using Kucoin.Net.Clients;

namespace Bonsai.Workers;

public class DataAnalysisWorkerService : BackgroundService
{
    private readonly IDataAnalysisService _dataAnalysisService;
    private static KucoinRestClient? restClient;

    public DataAnalysisWorkerService(IDataAnalysisService dataAnalysisService)
    {
        _dataAnalysisService = dataAnalysisService;
    }

    private const int GeneralDelay = 1000;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await DoBackupAsync().ConfigureAwait(false);
            await Task.Delay(GeneralDelay, stoppingToken);
        }
    }

    private async Task DoBackupAsync()
    {
       await _dataAnalysisService.ClosePositions().ConfigureAwait(false);
    }
}