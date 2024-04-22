﻿using Binance.Net.Enums;
using CryptoExchange.Net.CommonObjects;

namespace Bonsai.Services
{
    public interface IDataAnalysisService
    {
        Task<Position?> ClosePositions();
        Task<string?> CreatePositionsBuy(List<Position?> notToBeCreated);
        Task<bool> IncreasePositions();
    }
}
