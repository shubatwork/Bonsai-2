using Bonsai.Services;
using CryptoExchange.Net.CommonObjects;

namespace Bonsai.Workers;

public class DataAnalysisWorkerService : BackgroundService
{
    private readonly IDataAnalysisService _dataAnalysisService;

    public DataAnalysisWorkerService(IDataAnalysisService dataAnalysisService)
    {
        _dataAnalysisService = dataAnalysisService;
    }

    private const int GeneralDelay = 1000 * 10;

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
        var x = await _dataAnalysisService.ClosePositions().ConfigureAwait(false);
        if(x == null)
        {
            notToBeCreated.Add(x);
            //await _dataAnalysisService.CreatePositionsBuy(notToBeCreated).ConfigureAwait(false);
        }
        
        if (notToBeCreated.Count > 50)
        {
            notToBeCreated.Clear();
        }
        return notToBeCreated;
    }
}