using Bonsai.Services;
using CryptoExchange.Net.CommonObjects;

namespace Bonsai.Workers;

public class LossWorkerService : BackgroundService
{
    private readonly IDataAnalysisService profit;

    public LossWorkerService(IDataAnalysisService profitService)
    {
        profit = profitService;
    }

    private const int GeneralDelay = 1000 * 60 * 10;

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
        await profit.CloseOldPositions().ConfigureAwait(false);
        return true;
    }
}