using MakeMeRich.Binance.Services.Interfaces;

namespace Bonsai.Workers;

public class ProfitWorkerService : BackgroundService
{
    private readonly IProfitService profit;

    public ProfitWorkerService(IProfitService profitService)
    {
        profit = profitService;
    }

    private const int GeneralDelay = 1000 * 60 * 1;

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
        Console.WriteLine("Started Execution For Creation");
        await profit.ClosePositionsForProfit().ConfigureAwait(false);
        Console.WriteLine("Ended Execution For Creation");
        return true;
    }
}