using Bonsai.Services;

namespace Bonsai.Workers;

public class DataAnalysisSonaWorkerService : BackgroundService
{
    private readonly IDataAnalysisService _dataAnalysisService;

    public DataAnalysisSonaWorkerService(IDataAnalysisService dataAnalysisService)
    {
        _dataAnalysisService = dataAnalysisService;
    }

    private const int GeneralDelay = 1000 * 60;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await DoBackupAsync().ConfigureAwait(false);
        await Task.Delay(GeneralDelay, stoppingToken);
    }

    private async Task DoBackupAsync()
    {
        await _dataAnalysisService.CreatePositionsAdx().ConfigureAwait(false);
    }
}