using Kucoin.Net.Clients;
using Kucoin.Net.Enums;
using Kucoin.Net.Objects.Models.Futures;
using Kucoin.Net.Objects;
using CryptoExchange.Net.CommonObjects;

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
            if (accountInfo.RiskRatio > .2M)
            {
                var result = await restClient!.FuturesApi.Account.TransferToFuturesAccountAsync("USDT", 1, AccountType.Main);
            }
            
            var symbolList = await GetPositionsAsync(credentials);
            await CloseProfitablePosition(symbolList);
            var canCreate = await PlaceOrders(symbolList);
            if(!canCreate)
            {
                canCreate = await PlaceOrdersProfit(symbolList);
            }
            if (!canCreate && symbolList.Count() < 300)
            {
                //await OpenNewPosition(symbolList, CommonOrderSide.Sell);
            }
            Thread.Sleep(1000 * 60);
        }

        private static async Task CreateInSub()
        {
            var credentials = GetApiCredentials("API_KEY_2", "API_SECRET_2", "API_PASSPHRASE_2");
            var accountInfo = await GetAccountOverviewAsync(credentials);
            if (accountInfo.RiskRatio > .2M)
            {
                var result = await restClient!.FuturesApi.Account.TransferToFuturesAccountAsync("USDT", 1, AccountType.Main);
            }

            var symbolList = await GetPositionsAsync(credentials);
            await CloseProfitablePosition(symbolList);
            var canCreate = await PlaceOrders(symbolList);
            if (!canCreate)
            {
                canCreate = await PlaceOrdersProfit(symbolList);
            }
            if (symbolList.Count() < 300 && !canCreate)
            {
                //await OpenNewPosition(symbolList, CommonOrderSide.Buy);
            }

            Thread.Sleep(1000 * 60);
        }

        private static async Task<bool> CloseProfitablePosition(IEnumerable<KucoinPosition> symbolList)
        {
            foreach (KucoinPosition position in symbolList)
            {
                if (position.UnrealizedPnl > 0.1M || position.UnrealizedPnl < -1M)
                {
                    var closeOrderResult = await restClient!.FuturesApi.Trading.PlaceOrderAsync(
                    position.Symbol, OrderSide.Buy, NewOrderType.Market, 0, closeOrder: true, marginMode: FuturesMarginMode.Cross);
                }
            }

            return false;
        }

        private static async Task<bool> PlaceOrders(IEnumerable<KucoinPosition> symbolList)
        {
            foreach (var symbol in symbolList.OrderBy(x=>x.UnrealizedRoePercentage))
            {
                if (symbol != null && symbol.CurrentQuantity > 0 && symbol.UnrealizedRoePercentage < -2M)
                {
                    var placeOrderResult = await restClient!.FuturesApi.Trading.PlaceOrderAsync(
                        symbol.Symbol, OrderSide.Buy, NewOrderType.Market, 25, quantityInQuoteAsset: 1, marginMode: FuturesMarginMode.Cross);
                    if (placeOrderResult.Success)
                    {
                        return true;
                    }
                }
                if (symbol != null && symbol.CurrentQuantity < 0 && symbol.UnrealizedRoePercentage < -2M)
                {
                    var placeOrderResult = await restClient!.FuturesApi.Trading.PlaceOrderAsync(
                        symbol.Symbol, OrderSide.Sell, NewOrderType.Market, 25, quantityInQuoteAsset: 1, marginMode: FuturesMarginMode.Cross);
                    if (placeOrderResult.Success)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static async Task<bool> PlaceOrdersProfit(IEnumerable<KucoinPosition> symbolList)
        {
            foreach (var symbol in symbolList.OrderBy(x => x.UnrealizedRoePercentage))
            {
                if (symbol != null && symbol.CurrentQuantity > 0 && symbol.UnrealizedRoePercentage > 1M)
                {
                    var placeOrderResult = await restClient!.FuturesApi.Trading.PlaceOrderAsync(
                        symbol.Symbol, OrderSide.Buy, NewOrderType.Market, 25, quantityInQuoteAsset: 1, marginMode: FuturesMarginMode.Cross);
                    if (placeOrderResult.Success)
                    {
                        return true;
                    }
                }
                if (symbol != null && symbol.CurrentQuantity < 0 && symbol.UnrealizedRoePercentage > 1M)
                {
                    var placeOrderResult = await restClient!.FuturesApi.Trading.PlaceOrderAsync(
                        symbol.Symbol, OrderSide.Sell, NewOrderType.Market, 25, quantityInQuoteAsset: 1, marginMode: FuturesMarginMode.Cross);
                    if (placeOrderResult.Success)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static async Task OpenNewPosition(IEnumerable<KucoinPosition> symbolList, CommonOrderSide commonOrder)
        {
            OrderSide? mode = null;
            var tickerList = await restClient!.FuturesApi.ExchangeData.GetTickersAsync();
            foreach (var randomSymbol in tickerList.Data)
            {
                if (symbolList.Any(x => x.Symbol == randomSymbol.Symbol))
                {
                    continue;
                }

                var ticker = await restClient.FuturesApi.ExchangeData.GetKlinesAsync(randomSymbol.Symbol, FuturesKlineInterval.OneDay, DateTime.UtcNow.AddDays(-1));
                var current = ticker.Data.LastOrDefault();

                if (current?.OpenPrice < current?.ClosePrice && commonOrder == CommonOrderSide.Buy)
                {
                    mode = OrderSide.Buy;
                }
                else if (current?.OpenPrice > current?.ClosePrice && commonOrder == CommonOrderSide.Sell)
                {
                    mode = OrderSide.Sell;
                }

                if (mode == null)
                {
                    continue;
                }

                var placeOrderResult = await restClient.FuturesApi.Trading.PlaceOrderAsync(
                    randomSymbol.Symbol, mode.Value, NewOrderType.Market, 25, quantityInQuoteAsset: 1, marginMode: FuturesMarginMode.Cross);

                if (!placeOrderResult.Success)
                {
                    placeOrderResult = await restClient.FuturesApi.Trading.PlaceOrderAsync(
                    randomSymbol.Symbol, mode.Value, NewOrderType.Market, 25, quantityInQuoteAsset: 2, marginMode: FuturesMarginMode.Cross);

                }

                if (placeOrderResult.Success)
                {
                    continue;
                }

            }
        }

        private static async Task<KucoinAccountOverview> GetAccountOverviewAsync(KucoinApiCredentials credentials)
        {
            restClient = new KucoinRestClient();
            restClient.SetApiCredentials(credentials);
            var accountInfo = await restClient.FuturesApi.Account.GetAccountOverviewAsync("USDT");
            return accountInfo.Data;
        }

    }
}
