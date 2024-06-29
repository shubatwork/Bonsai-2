﻿using Bonsai.Services;
using CryptoExchange.Net.CommonObjects;

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

    private const int GeneralDelay = 1000 * 2;

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
        return notToBeCreated;
    }
}