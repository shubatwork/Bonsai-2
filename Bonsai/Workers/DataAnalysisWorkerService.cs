using Bonsai.Services;
using CryptoExchange.Net.CommonObjects;

namespace Bonsai.Workers;

public class DataAnalysisWorkerService : BackgroundService
{
    private readonly IDataAnalysisService _dataAnalysisService;
    private readonly IStopLossService _stopLossService;

    public DataAnalysisWorkerService(IDataAnalysisService dataAnalysisService, IStopLossService stopLossService)
    {
        _dataAnalysisService = dataAnalysisService;
        _stopLossService = stopLossService;
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

    private async Task DoBackupAsync()
    {
       await _dataAnalysisService.CreatePositionsBuy().ConfigureAwait(false);
       // await _dataAnalysisService.ClosePositions
           // ().ConfigureAwait(false);
    }
}