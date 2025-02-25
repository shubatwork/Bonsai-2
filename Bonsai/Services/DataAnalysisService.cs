using Kucoin.Net.Clients;
using Kucoin.Net.Enums;
using Kucoin.Net.Objects.Models.Futures;
using Kucoin.Net.Objects;
using CryptoExchange.Net.CommonObjects;

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


            foreach (var x in positionsList)
            {
                if (x != null && (x.UnrealizedPnl < -.3M || x.UnrealizedPnl > 1M))
                {
                    for (int i = 0; i < 100; i++)
                    {
                        var closeOrderResult = await restClientMain!.FuturesApi.Trading.PlaceOrderAsync(x!.Symbol, OrderSide.Sell, NewOrderType.Market,25, quantityInQuoteAsset: i, marginMode: FuturesMarginMode.Cross);
                        if (!closeOrderResult.Success)
                        {
                             closeOrderResult = await restClientMain!.FuturesApi.Trading.PlaceOrderAsync(x!.Symbol, OrderSide.Sell, NewOrderType.Market, 25, quantityInQuoteAsset: 25M, marginMode: FuturesMarginMode.Cross);

                        }
                        if (!closeOrderResult.Success)
                        {
                             closeOrderResult = await restClientMain!.FuturesApi.Trading.PlaceOrderAsync(x!.Symbol, OrderSide.Sell, NewOrderType.Market, 25, quantityInQuoteAsset: 50M, marginMode: FuturesMarginMode.Cross);

                        }
                        if (!closeOrderResult.Success)
                        {
                            await restClientMain!.FuturesApi.Trading.PlaceOrderAsync(x!.Symbol, OrderSide.Sell, NewOrderType.Market, 25, closeOrder: true, marginMode: FuturesMarginMode.Cross); ;
                        }
                    }
                }
            }

            foreach (var x in positionsListSub)
            {
                if (x != null && (x.UnrealizedPnl < -.3M || x.UnrealizedPnl > 1M))
                {
                    {
                        var closeOrderResult = await restClientSub!.FuturesApi.Trading.PlaceOrderAsync(x!.Symbol, OrderSide.Buy, NewOrderType.Market,25, quantityInQuoteAsset: 10M, marginMode: FuturesMarginMode.Cross);
                        if (!closeOrderResult.Success)
                        {
                             closeOrderResult = await restClientSub!.FuturesApi.Trading.PlaceOrderAsync(x!.Symbol, OrderSide.Buy, NewOrderType.Market, 25, quantityInQuoteAsset: 25M, marginMode: FuturesMarginMode.Cross);

                        }
                        if (!closeOrderResult.Success)
                        {
                             closeOrderResult = await restClientSub!.FuturesApi.Trading.PlaceOrderAsync(x!.Symbol, OrderSide.Buy, NewOrderType.Market, 25, quantityInQuoteAsset: 50M, marginMode: FuturesMarginMode.Cross);

                        }
                        if (!closeOrderResult.Success)
                        {
                            await restClientSub!.FuturesApi.Trading.PlaceOrderAsync(x!.Symbol, OrderSide.Sell, NewOrderType.Market, 25, closeOrder: true, marginMode: FuturesMarginMode.Cross); ;
                        }
                    }
                }
            }

            {
                //if (DateTime.UtcNow.Minute % 10 == 0)
                {
                    var tickerList = await restClientMain!.FuturesApi.ExchangeData.GetTickersAsync();
                    var result = new Dictionary<string, CommonOrderSide>();
                    foreach (var ticker in tickerList.Data.Take(30))
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
                            if(accountInfoMain.Data.RiskRatio < .2M)
                            {
                                if (side == CommonOrderSide.Buy && !positionsList.Any(x => x.Symbol == position.Symbol && Math.Abs(x.PositionValue) > 10) && !positionsListSub.Any(x => x.Symbol == position.Symbol))
                                {
                                    var success = await restClientMain.FuturesApi.Trading.PlaceOrderAsync(position.Symbol, OrderSide.Buy, NewOrderType.Market, 25, quantityInQuoteAsset: 100, marginMode: FuturesMarginMode.Cross);
                                    if (!success.Success)
                                    {
                                        success = await restClientMain.FuturesApi.Trading.PlaceOrderAsync(position.Symbol, OrderSide.Buy, NewOrderType.Market, 25, quantityInQuoteAsset: 100, marginMode: FuturesMarginMode.Cross);
                                        if (!success.Success)
                                        {
                                            success = await restClientMain.FuturesApi.Trading.PlaceOrderAsync(position.Symbol, OrderSide.Buy, NewOrderType.Market, 25, quantityInQuoteAsset: 100, marginMode: FuturesMarginMode.Cross);
                                        }
                                    }
                                    if (success.Success)
                                    {
                                        return;
                                    }
                                }
                            }
                            if (accountInfoSub.Data.RiskRatio < .2M)
                            {
                                if (side == CommonOrderSide.Sell && !positionsListSub.Any(x => x.Symbol == position.Symbol && Math.Abs(x.PositionValue) > 10) && !positionsList.Any(x => x.Symbol == position.Symbol))
                                {
                                    var success = await restClientSub.FuturesApi.Trading.PlaceOrderAsync(position.Symbol, OrderSide.Sell, NewOrderType.Market, 25, quantityInQuoteAsset: 100, marginMode: FuturesMarginMode.Cross);
                                    if (!success.Success)
                                    {
                                        success = await restClientSub.FuturesApi.Trading.PlaceOrderAsync(position.Symbol, OrderSide.Sell, NewOrderType.Market, 25, quantityInQuoteAsset: 100, marginMode: FuturesMarginMode.Cross);
                                        if (!success.Success)
                                        {
                                            success = await restClientSub.FuturesApi.Trading.PlaceOrderAsync(position.Symbol, OrderSide.Sell, NewOrderType.Market, 25, quantityInQuoteAsset: 100, marginMode: FuturesMarginMode.Cross);
                                        }
                                    }
                                    if (success.Success)
                                    {
                                        return;
                                    }
                                }
                                //if (side == CommonOrderSide.Buy && positionsListSub.Any(x => x.Symbol == position.Symbol && x.CurrentQuantity < 0))
                                //{
                                //    if (closeOrderResult.Success)
                                //    {
                                //        return;
                                //    }
                                //}
                            }
                        }
                    }
                }
            }

        }

        private static CommonOrderSide? GetDataForSymbol(string symbol)
        {
            var ticker = restClientMain!.FuturesApi.ExchangeData.GetKlinesAsync(symbol, FuturesKlineInterval.FiveMinutes, DateTime.UtcNow.AddDays(-2)).Result;
            var count = ticker.Data.Count();
            if (count > 0)
            {
                var ma7 = ticker.Data.Skip(count - 5).Average(x => (x.OpenPrice + x.ClosePrice) / 2);
                var ma25 = ticker.Data.Skip(count - 13).Average(x => (x.OpenPrice + x.ClosePrice) / 2);

                if (ma7 > ma25)
                {
                    return CommonOrderSide.Sell;
                }
                else if (ma7 < ma25)
                {
                    return CommonOrderSide.Buy;
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
