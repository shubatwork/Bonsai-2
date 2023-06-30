using Bonsai.Services;

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

    private const int GeneralDelay = 1000 * 60;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        List<NotToTakePosition> positionsClosed = new List<NotToTakePosition>();
        while (!stoppingToken.IsCancellationRequested)
        {
            string result = await DoBackupAsync(positionsClosed).ConfigureAwait(false);
            if (result != null)
            {
                positionsClosed.Add(new NotToTakePosition { Symbol = result, ClosedTime = DateTime.Now });
            }
            foreach (var position in positionsClosed)
            {
                if(position.ClosedTime.AddMinutes(15) < DateTime.Now)
                {
                    positionsClosed.Remove(position);
                }
            }
            await Task.Delay(GeneralDelay, stoppingToken);
        }
    }

    private async Task<string> DoBackupAsync(List<NotToTakePosition> positionsClosed)
    {
        await _dataAnalysisService.CreatePositions().ConfigureAwait(false);
        Thread.Sleep(10000);
        var closedPosition = await _dataAnalysisService.ClosePositions().ConfigureAwait(false);
        return null;
    }
}