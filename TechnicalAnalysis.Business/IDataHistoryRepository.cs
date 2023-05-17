using Binance.Net.Interfaces.Clients.UsdFuturesApi;
using System.Threading.Tasks;
using TechnicalAnalysis.Business;

namespace MakeMeRich.Binance.Services.Interfaces
{
    public interface IDataHistoryRepository
    {
        Task<DataHistory> GetData(string symbol, IBinanceClientUsdFuturesApi _client);
    }
}
