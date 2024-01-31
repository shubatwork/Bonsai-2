using Bonsai.Services;
using CryptoExchange.Net.CommonObjects;

namespace Bonsai.Workers;

public class ProfitWorkerService : BackgroundService
{
    private readonly IDataAnalysisService profit;

    public ProfitWorkerService(IDataAnalysisService profitService)
    {
        profit = profitService;
    }

    private const int GeneralDelay = 1000 * 60;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var result = await DoBackupAsync().ConfigureAwait(false);
            if (result)
            {
                await Task.Delay(GeneralDelay, stoppingToken);
            }
        }
    }

    private async Task<bool> DoBackupAsync()
    {
        await profit.ClosePositions().ConfigureAwait(false);
        //Thread.Sleep(1000 * 10);
        //await profit.IncreasePositions().ConfigureAwait(false);
        return true;
    }
}