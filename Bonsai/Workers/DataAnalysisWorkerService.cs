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

    private const int GeneralDelay = 10;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var closed = new List<Position?>();
        while (!stoppingToken.IsCancellationRequested)
        {
           closed = await DoBackupAsync(closed).ConfigureAwait(false);
           await Task.Delay(GeneralDelay, stoppingToken);
        }
    }

    private async Task<List<Position?>> DoBackupAsync(List<Position?> notToBeCreated)
    {
        await _dataAnalysisService.CreatePositionsBuy(notToBeCreated).ConfigureAwait(false);
        //await _stopLossService.CreateOrdersForTrailingStopLoss().ConfigureAwait(false);
        var x = await _dataAnalysisService.ClosePositions().ConfigureAwait(false);

        if (x != null && !notToBeCreated.Select(x => x!.Symbol).Contains(x!.Symbol))
        {
            notToBeCreated.Add(x);
        }

        //if (notToBeCreated.Count > 100)
        //{
        //    notToBeCreated.Clear();
        //}
        return notToBeCreated;
    }
}