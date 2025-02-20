using Kucoin.Net.Clients;
using Kucoin.Net.Enums;
using Kucoin.Net.Objects.Models.Futures;
using Kucoin.Net.Objects;
using CryptoExchange.Net.CommonObjects;
using System.Net;
using CryptoExchange.Net.Interfaces;

namespace Bonsai.Services
{
    public class DataAnalysisService : IDataAnalysisService
    {
        private static KucoinRestClient? restClientMain;
        private static KucoinRestClient? restClientSub;

        public DataAnalysisService()
        {
            restClientMain = new KucoinRestClient();
            restClientSub = new KucoinRestClient();
        }

        public async Task ClosePositions()
        {
            var credentials = GetApiCredentials("API_KEY_1", "API_SECRET_1", "API_PASSPHRASE_1");
            restClientMain.SetApiCredentials(credentials);
            var positionsList = await GetPositionsAsync(restClientMain);
            var accountInfoMain = await restClientMain.FuturesApi.Account.GetAccountOverviewAsync("USDT");

            var credentials2 = GetApiCredentials("API_KEY_2", "API_SECRET_2", "API_PASSPHRASE_2");
            restClientSub.SetApiCredentials(credentials2);
            var positionsListSub = await GetPositionsAsync(restClientSub);
            var accountInfoSub = await restClientSub.FuturesApi.Account.GetAccountOverviewAsync("USDT");



            var tickerList = await restClientMain!.FuturesApi.ExchangeData.GetTickersAsync();

            var result = new Dictionary<string, CommonOrderSide>();
            foreach (var ticker in tickerList.Data)
            {
                var data = GetDataForSymbol(ticker.Symbol);
                if (data.HasValue)
                {
                    result.Add(ticker.Symbol, data.Value);
                }
            }
            foreach (var position in tickerList.Data)
            {
                if (result.TryGetValue(position.Symbol, out var side))
                {
                    if (accountInfoMain.Data.RiskRatio < 0.2M)
                    {
                        if (side == CommonOrderSide.Buy && !positionsList.Any(x => x.Symbol == position.Symbol))
                        {
                            var success = await restClientMain.FuturesApi.Trading.PlaceOrderAsync(position.Symbol, OrderSide.Buy, NewOrderType.Market, 25, quantityInQuoteAsset: 1, marginMode: FuturesMarginMode.Cross);
                            if (!success.Success)
                            {
                                success = await restClientMain.FuturesApi.Trading.PlaceOrderAsync(position.Symbol, OrderSide.Buy, NewOrderType.Market, 25, quantityInQuoteAsset: 2, marginMode: FuturesMarginMode.Cross);
                                if (!success.Success)
                                {
                                    success = await restClientMain.FuturesApi.Trading.PlaceOrderAsync(position.Symbol, OrderSide.Buy, NewOrderType.Market, 25, quantityInQuoteAsset: 3, marginMode: FuturesMarginMode.Cross);
                                }
                            }
                        }
                        if (side == CommonOrderSide.Buy && positionsList.Any(x => x.Symbol == position.Symbol && x.UnrealizedRoePercentage < -2M))
                        {
                            var success = await restClientMain.FuturesApi.Trading.PlaceOrderAsync(position.Symbol, OrderSide.Buy, NewOrderType.Market, 25, quantityInQuoteAsset: 1, marginMode: FuturesMarginMode.Cross);
                            if (!success.Success)
                            {
                                success = await restClientMain.FuturesApi.Trading.PlaceOrderAsync(position.Symbol, OrderSide.Buy, NewOrderType.Market, 25, quantityInQuoteAsset: 2, marginMode: FuturesMarginMode.Cross);
                                if (!success.Success)
                                {
                                    success = await restClientMain.FuturesApi.Trading.PlaceOrderAsync(position.Symbol, OrderSide.Buy, NewOrderType.Market, 25, quantityInQuoteAsset: 3, marginMode: FuturesMarginMode.Cross);
                                }
                            }
                        }
                    }
                    if (accountInfoSub.Data.RiskRatio < 0.2M)
                    {
                        if (side == CommonOrderSide.Sell && !positionsListSub.Any(x => x.Symbol == position.Symbol))
                        {
                            var success = await restClientSub.FuturesApi.Trading.PlaceOrderAsync(position.Symbol, OrderSide.Sell, NewOrderType.Market, 25, quantityInQuoteAsset: 1, marginMode: FuturesMarginMode.Cross);
                            if (!success.Success)
                            {
                                success = await restClientSub.FuturesApi.Trading.PlaceOrderAsync(position.Symbol, OrderSide.Sell, NewOrderType.Market, 25, quantityInQuoteAsset: 2, marginMode: FuturesMarginMode.Cross);
                                if (!success.Success)
                                {
                                    success = await restClientSub.FuturesApi.Trading.PlaceOrderAsync(position.Symbol, OrderSide.Sell, NewOrderType.Market, 25, quantityInQuoteAsset: 3, marginMode: FuturesMarginMode.Cross);
                                }
                            }
                        }
                        if (side == CommonOrderSide.Sell && positionsListSub.Any(x => x.Symbol == position.Symbol && x.UnrealizedRoePercentage < -2M))
                        {
                            var success = await restClientSub.FuturesApi.Trading.PlaceOrderAsync(position.Symbol, OrderSide.Sell, NewOrderType.Market, 25, quantityInQuoteAsset: 1, marginMode: FuturesMarginMode.Cross);
                            if (!success.Success)
                            {
                                success = await restClientSub.FuturesApi.Trading.PlaceOrderAsync(position.Symbol, OrderSide.Sell, NewOrderType.Market, 25, quantityInQuoteAsset: 2, marginMode: FuturesMarginMode.Cross);
                                if (!success.Success)
                                {
                                    success = await restClientSub.FuturesApi.Trading.PlaceOrderAsync(position.Symbol, OrderSide.Sell, NewOrderType.Market, 25, quantityInQuoteAsset: 3, marginMode: FuturesMarginMode.Cross);
                                }
                            }
                        }
                    }
                }
            }

            foreach (KucoinPosition position in positionsList)
            {
                if (position.UnrealizedPnlPercentage > 0.01M)
                {
                    var closeOrderResult = await restClientMain!.FuturesApi.Trading.PlaceOrderAsync(
                    position.Symbol, OrderSide.Buy, NewOrderType.Market, 0, closeOrder: true, marginMode: FuturesMarginMode.Cross);
                }
            }

            foreach (KucoinPosition position in positionsListSub)
            {
                if (position.UnrealizedPnlPercentage > 0.01M)
                {
                    var closeOrderResult = await restClientSub!.FuturesApi.Trading.PlaceOrderAsync(
                    position.Symbol, OrderSide.Buy, NewOrderType.Market, 0, closeOrder: true, marginMode: FuturesMarginMode.Cross);
                }
            }

            Thread.Sleep(10 * 1000);
        }

        private static CommonOrderSide? GetDataForSymbol(string symbol)
        {
            var ticker = restClientMain!.FuturesApi.ExchangeData.GetKlinesAsync(symbol, FuturesKlineInterval.OneMinute, DateTime.UtcNow.AddHours(-2)).Result;
            var count = ticker.Data.Count();
            if (count > 0)
            {
                var ma7 = ticker.Data.Skip(count - 7).Average(x => x.ClosePrice);
                var ma25 = ticker.Data.Skip(count - 25).Average(x => x.ClosePrice);

                if (ma7 > ma25)
                {
                    return CommonOrderSide.Buy;
                }
                else if (ma7 < ma25)
                {
                    return CommonOrderSide.Sell;
                }
            }
            return null;
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

        private static async Task<IEnumerable<KucoinPosition>> GetPositionsAsync(KucoinRestClient restClient)
        {
            var positions = await restClient.FuturesApi.Account.GetPositionsAsync();
            return positions.Data;
        }
    }
}
