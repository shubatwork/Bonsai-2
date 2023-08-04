using Bonsai.Services;

namespace Bonsai.Workers;

public class DataAnalysisWorkerService : BackgroundService
{
    private readonly IDataAnalysisService _dataAnalysisService;

    public DataAnalysisWorkerService(IDataAnalysisService dataAnalysisService)
    {
        _dataAnalysisService = dataAnalysisService;
    }

    private const int GeneralDelay = 1000 * 60 * 60;

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
        await _dataAnalysisService.CreatePositions(false).ConfigureAwait(false);
        return null;
    }
}