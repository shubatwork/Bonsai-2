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

    private const int GeneralDelay = 1000 * 60 * 5;

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
        CommonOrderSide[] allColors = (CommonOrderSide[])Enum.GetValues(typeof(CommonOrderSide));
        Random random = new Random();
        int randomIndex = random.Next(allColors.Length);
        CommonOrderSide randomColor = allColors[randomIndex];
        await profit.CreatePositions(randomColor).ConfigureAwait(false);
        return true;
    }
}