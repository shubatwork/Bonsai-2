using Kucoin.Net.Clients;
using Kucoin.Net.Enums;
using Kucoin.Net.Objects.Models.Futures;
using Kucoin.Net.Objects;

namespace Bonsai.Services
{
    public class DataAnalysisService : IDataAnalysisService
    {
        private static KucoinRestClient? restClient;

        public DataAnalysisService()
        {
        }

        public async Task ClosePositions()
        {

            await CreateInMain();
            await CreateInSub();
        }

        private static KucoinApiCredentials GetApiCredentials(string apiKey, string apiSecret, string apiPassphrase)
        {
            if (apiKey == "API_KEY_1")
            {
                return new KucoinApiCredentials("6792c43bc0a1b1000135cb65", "25ab9c72-17e6-4951-b7a8-6e2fce9c3026", "test1234");
            }
            if (apiKey == "API_KEY_2")
            {
                return new KucoinApiCredentials("679b7a366425d800012aca8f", "99cd2f9a-b4ed-4fe3-8f6e-69d70e03eb51", "test1234");
            }
            return new KucoinApiCredentials("", "", "");
        }

        private static async Task<KucoinAccountOverview> GetAccountOverviewAsync(KucoinApiCredentials credentials)
        {
            restClient = new KucoinRestClient();
            restClient.SetApiCredentials(credentials);
            var accountInfo = await restClient.FuturesApi.Account.GetAccountOverviewAsync("USDT");
            return accountInfo.Data;
        }

        private static async Task<IEnumerable<KucoinPosition>> GetPositionsAsync(KucoinApiCredentials credentials)
        {
            restClient = new KucoinRestClient();
            restClient.SetApiCredentials(credentials);
            var positions = await restClient.FuturesApi.Account.GetPositionsAsync();
            return positions.Data;
        }

        private static async Task CreateInMain()
        {
            var credentials = GetApiCredentials("API_KEY_1", "API_SECRET_1", "API_PASSPHRASE_1");
            var accountInfo = await GetAccountOverviewAsync(credentials);

            bool canCreate = accountInfo.RiskRatio < .20M;
            bool canIncrease = accountInfo.RiskRatio < .30M;
            var symbolList = await GetPositionsAsync(credentials);
            await CloseProfitablePosition(symbolList);

            if (canIncrease)
            {
                await PlaceOrders(symbolList, OrderSide.Sell, .1m);
                await PlaceOrders(symbolList, OrderSide.Buy, .1m);
            }

            if (canCreate)
            {
                await OpenNewPosition(symbolList, OrderSide.Sell);
            }
        }

        private static async Task CreateInSub()
        {
            var credentials = GetApiCredentials("API_KEY_2", "API_SECRET_2", "API_PASSPHRASE_2");
            var accountInfo = await GetAccountOverviewAsync(credentials);
            Console.WriteLine($"{accountInfo.MarginBalance} - {accountInfo.UnrealizedPnl} - {accountInfo.RiskRatio}");

            bool canCreate = accountInfo.RiskRatio < .20M;
            bool canIncrease = accountInfo.RiskRatio < .30M;
            var symbolList = await GetPositionsAsync(credentials);
            await CloseProfitablePosition(symbolList);
            if (canIncrease)
            {
                await PlaceOrders(symbolList, OrderSide.Sell, .1m);
                await PlaceOrders(symbolList, OrderSide.Buy, .1m);
            }
            if (canCreate)
            {
                await OpenNewPosition(symbolList, OrderSide.Buy);
            }
        }

        private static async Task CloseProfitablePosition(IEnumerable<KucoinPosition> symbolList)
        {
            var kucoinPosition = symbolList.Where(x => x.UnrealizedPnl > 0.01M).MaxBy(x => x.UnrealizedPnl);
            if (kucoinPosition != null)
            {
                var closeOrderResult = await restClient!.FuturesApi.Trading.PlaceOrderAsync(
                    kucoinPosition.Symbol, OrderSide.Buy, NewOrderType.Market, 0, closeOrder: true, marginMode: FuturesMarginMode.Cross);

                if (closeOrderResult.Success)
                {
                    Console.WriteLine($"Closed {kucoinPosition.Symbol} - {kucoinPosition.UnrealizedPnl}");
                }
                else
                {
                    Console.WriteLine($"Failed to close {kucoinPosition.Symbol}: {closeOrderResult.Error}");
                }
            }
        }

        private static async Task PlaceOrders(IEnumerable<KucoinPosition> symbolList, OrderSide side, decimal roeThreshold)
        {
            foreach (var symbol in symbolList)
            {
                if (symbol != null && ((side == OrderSide.Sell && symbol.CurrentQuantity < 0) || (side == OrderSide.Buy && symbol.CurrentQuantity > 0)) && symbol.UnrealizedRoePercentage > roeThreshold)
                {
                    var placeOrderResult = await restClient!.FuturesApi.Trading.PlaceOrderAsync(
                        symbol.Symbol, side, NewOrderType.Market, 25, quantityInQuoteAsset: 1, marginMode: FuturesMarginMode.Cross);
                }
            }
        }

        private static async Task OpenNewPosition(IEnumerable<KucoinPosition> symbolList, OrderSide orderSide)
        {
            OrderSide? mode = null;
            var tickerList = await restClient!.FuturesApi.ExchangeData.GetTickersAsync();
            var random = new Random();
            int r = random.Next(tickerList.Data.Count());
            var randomSymbol = tickerList.Data.ElementAt(r);
            {
                if (symbolList.Any(x => x.Symbol == randomSymbol.Symbol))
                {
                    return;
                }

                var ticker = await restClient.FuturesApi.ExchangeData.GetKlinesAsync(randomSymbol.Symbol, FuturesKlineInterval.OneDay, DateTime.UtcNow.AddDays(-1));
                var current = ticker.Data.LastOrDefault();

                if (current?.OpenPrice < current?.ClosePrice && orderSide == OrderSide.Buy)
                {
                    mode = OrderSide.Buy;
                }
                else if (current?.OpenPrice > current?.ClosePrice && orderSide == OrderSide.Sell)
                {
                    mode = OrderSide.Sell;
                }

                if (mode == null)
                {
                    return;
                }

                var placeOrderResult = await restClient.FuturesApi.Trading.PlaceOrderAsync(
                    randomSymbol.Symbol, mode.Value, NewOrderType.Market, 25, quantityInQuoteAsset: 1, marginMode: FuturesMarginMode.Cross);

                if (placeOrderResult.Success)
                {
                    Console.WriteLine($"Opened position on {randomSymbol.Symbol}");
                    return;
                }
                else
                {
                    await RetryPlaceOrder(randomSymbol.Symbol, mode.Value);
                }
            }
        }

        private static async Task RetryPlaceOrder(string symbol, OrderSide mode)
        {
            for (int i = 2; i <= 4; i++)
            {
                var placeOrderResult = await restClient!.FuturesApi.Trading.PlaceOrderAsync(
                    symbol, mode, NewOrderType.Market, 25, quantityInQuoteAsset: i, marginMode: FuturesMarginMode.Cross);

                if (placeOrderResult.Success)
                {
                    Console.WriteLine($"Opened position on {symbol}");
                    break;
                }
                else if (i == 4)
                {
                    Console.WriteLine($"Failed to open position on {symbol} : {placeOrderResult.Error}");
                }
            }
        }
    }

}
