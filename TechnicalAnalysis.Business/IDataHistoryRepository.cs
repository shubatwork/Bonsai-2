using Binance.Net.Enums;
using Binance.Net.Interfaces.Clients.UsdFuturesApi;
using System.Threading.Tasks;
using TechnicalAnalysis.Business;

namespace MakeMeRich.Binance.Services.Interfaces
{
    public interface IDataHistoryRepository
    {
        Task<DataHistory> GetDataByInterval(string symbol, IBinanceClientUsdFuturesApi _client, KlineInterval klineInterval = KlineInterval.FiveMinutes);
    }
}
