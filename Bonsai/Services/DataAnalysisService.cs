using Kucoin.Net.Clients;
using Kucoin.Net.Enums;

namespace Bonsai.Services
{
    public class DataAnalysisService : IDataAnalysisService
    {
        public DataAnalysisService()
        {
        }


        public async Task ClosePositions()
        {
            bool canBuy = true;
            int profit = 0;
            int loss = 0;
            var mode = OrderSide.Buy;
            while (true)
            {
                Console.WriteLine("Start /n");
                var restClient = new KucoinRestClient();
                restClient.SetApiCredentials(new Kucoin.Net.Objects.KucoinApiCredentials
                    ("6792c43bc0a1b1000135cb65", "25ab9c72-17e6-4951-b7a8-6e2fce9c3026", "test1234"));

                var symbolList = await restClient.FuturesApi.Account.GetPositionsAsync();
                foreach (var symbol in symbolList.Data.Where(x => x.UnrealizedPnl > .003M))
                {
                    if (symbol != null && symbol.IsOpen)
                    {
                        var z = await restClient.FuturesApi.Trading.PlaceOrderAsync
                        (symbol.Symbol, Kucoin.Net.Enums.OrderSide.Buy, Kucoin.Net.Enums.NewOrderType.Market, 0, closeOrder: true, marginMode: Kucoin.Net.Enums.FuturesMarginMode.Cross);
                        Console.WriteLine("Closed " + symbol.Symbol + " - " + symbol.UnrealizedPnl);
                        if (z.Success && symbol.UnrealizedPnl > 0)
                        {
                            profit++;
                        }
                        if (z.Success && symbol.UnrealizedPnl < 0)
                        {
                            loss++;
                        }
                        continue;
                    }
                }
                canBuy = profit >= loss;
                mode = profit >= loss ? mode : (mode == OrderSide.Buy) ? OrderSide.Sell : OrderSide.Buy;
                if (canBuy)
                {
                    var tickerList = await restClient.FuturesApi.ExchangeData.GetTickersAsync();
                    {
                        var random = new Random();
                        int randomIndex = random.Next(tickerList.Data.Count());
                        var randomSymbol = tickerList.Data.ElementAt(randomIndex).Symbol;
                        {
                            var getPositions = await restClient.FuturesApi.Account.GetPositionAsync(randomSymbol);
                            if (getPositions != null && getPositions.Data.IsOpen)
                            {
                                continue;
                            }

                            var result = await restClient.FuturesApi.Trading.PlaceOrderAsync
                                (randomSymbol, mode, Kucoin.Net.Enums.NewOrderType.Market, 25, quantityInQuoteAsset: 1, marginMode: Kucoin.Net.Enums.FuturesMarginMode.Cross);

                            if (result.Success)
                            {
                                Console.WriteLine(randomSymbol);
                            }

                        }
                    }
                }
            }
        }

    }
}
