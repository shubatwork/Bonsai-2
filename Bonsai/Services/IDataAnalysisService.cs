using Binance.Net.Enums;
using CryptoExchange.Net.CommonObjects;

namespace Bonsai.Services
{
    public interface IDataAnalysisService
    {
        Task<Position?> ClosePositions();
        Task<string?> CreatePositions(KlineInterval klineInterval);
        Task<string?> CreatePositionsRSI(KlineInterval klineInterval);
    }
}
