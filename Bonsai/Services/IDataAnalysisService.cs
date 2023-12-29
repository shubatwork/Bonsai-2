using Binance.Net.Enums;
using CryptoExchange.Net.CommonObjects;

namespace Bonsai.Services
{
    public interface IDataAnalysisService
    {
        Task<Position?> ClosePositions();
        Task<string?> CreatePositionsBuy(CommonOrderSide mode);
        Task<string?> IncreasePositions();
    }
}
