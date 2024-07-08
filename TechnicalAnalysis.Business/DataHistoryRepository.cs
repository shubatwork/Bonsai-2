using Binance.Net.Enums;
using Binance.Net.Interfaces.Clients.UsdFuturesApi;
using MakeMeRich.Binance.Services.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TechnicalAnalysis.Business
{
    public class DataHistoryRepository : IDataHistoryRepository
    {
        public async Task<DataHistory?> GetDataByInterval(string symbol, IBinanceRestClientUsdFuturesApi _client, KlineInterval klineInterval = KlineInterval.FiveMinutes)
        {
            var task1 = await _client.ExchangeData.GetKlinesAsync(symbol, klineInterval, null, null, 60).ConfigureAwait(false);
            if(task1.Data != null) {
                var dataHistory = ParseJson(task1.Data);
                return dataHistory;
            }

            return null ;
        }

        private static DataHistory ParseJson(IEnumerable<Binance.Net.Interfaces.IBinanceKline> data)
        {
            var candles = new List<Candle>();

            foreach(var position in data)
            {
                Candle candle = new Candle()
                {
                    Close = ((double)position.ClosePrice),
                    Open = ((double)position.OpenPrice),
                    High = ((double)position.HighPrice),
                    Low = ((double)position.LowPrice),
                    Volumefrom = ((double)position.Volume),
                    Volumeto = ((double)position.Volume),
                };

                candles.Add(candle);
            }
            
            return new DataHistory(candles);
        }
    }
}
