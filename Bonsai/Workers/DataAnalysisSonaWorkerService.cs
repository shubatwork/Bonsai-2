using Bonsai.Services;

namespace Bonsai.Workers;

public class DataAnalysisSonaWorkerService : BackgroundService
{
    private readonly IDataAnalysisService _dataAnalysisService;

    public DataAnalysisSonaWorkerService(IDataAnalysisService dataAnalysisService)
    {
        _dataAnalysisService = dataAnalysisService;
    }

    private const int GeneralDelay = 1000 * 60 * 5;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        List<NotToTakePosition> positionsClosed = new List<NotToTakePosition>();
        while (!stoppingToken.IsCancellationRequested)
        {
            string result = await DoBackupAsync(positionsClosed).ConfigureAwait(false);
            await Task.Delay(GeneralDelay, stoppingToken);
        }
    }

    private async Task<string> DoBackupAsync(List<NotToTakePosition> positionsClosed)
    {
        await _dataAnalysisService.CloseSonaPositions().ConfigureAwait(false);
        return null;
    }
}