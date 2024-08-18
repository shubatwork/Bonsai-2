using Bonsai.Services;
using CryptoExchange.Net.CommonObjects;

namespace Bonsai.Workers;

public class ProfitWorkerService : BackgroundService
{
    private readonly IDataAnalysisService profit;
    private readonly IStopLossService stopLoss;

    public ProfitWorkerService(IDataAnalysisService profitService, IStopLossService stopLossService)
    {
        profit = profitService;
        stopLoss = stopLossService;
    }

    private const int GeneralDelay = 1000 * 300;

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
        await stopLoss.CloseOrders().ConfigureAwait(false);
        return true;
    }
}