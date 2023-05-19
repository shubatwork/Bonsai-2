﻿using Bonsai.Services;
using MakeMeRich.Binance.Services;
using MakeMeRich.Binance.Services.Interfaces;

namespace Bonsai.Workers;

public class DataAnalysisWorkerService : BackgroundService
{
    private readonly IDataAnalysisService _dataAnalysisService;

    public DataAnalysisWorkerService(IDataAnalysisService dataAnalysisService, IProfitService profitService)
    {
        _dataAnalysisService = dataAnalysisService;
        _profitService = profitService;
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
        await _dataAnalysisService.CreatePositions().ConfigureAwait(false);
        await _dataAnalysisService.ClosePositions().ConfigureAwait(false);
        return true;
    }
}