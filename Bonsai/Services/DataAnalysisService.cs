using Binance.Net.Interfaces.Clients.UsdFuturesApi;
using MakeMeRich.Binance.Services.Interfaces;
using CryptoExchange.Net.CommonObjects;
using Binance.Net.Enums;

namespace Bonsai.Services
{
    public class DataAnalysisService : IDataAnalysisService
    {
        private readonly IBinanceRestClientUsdFuturesApi _client;

        public DataAnalysisService(IDataHistoryRepository dataHistoryRepository)
        {
            _client = ClientDetails.GetClient();
        }


        public async Task<string?> CreatePositionsBuy()
        {

            var positionsAvailableData =
               await _client.CommonFuturesClient.GetPositionsAsync().ConfigureAwait(false);

            if (positionsAvailableData.Success)
            {

                if (positionsAvailableData.Data.Count(x => x.Quantity != 0) > 10)
                {
                    return null;
                }

                try
                {

                    var ticker =
               await _client.ExchangeData.GetTickersAsync().ConfigureAwait(false);

                    bool canBuy = true;
                    var list = new List<FinalResult>();


                    var list1 = ticker.Data.DistinctBy(x => x.Symbol).OrderByDescending(x => Math.Abs(x.PriceChangePercent)).Take(100);

                    foreach (var x in list1.Where(x => x.Symbol.ToLower().Contains("usdt")))
                    {
                        {
                            if (!positionsAvailableData.Data.Any(y => y.Symbol.ToLower().Equals(x!.Symbol.ToLower()) && y.Quantity != 0))
                            {
                                var data1 = await _client.ExchangeData.GetKlinesAsync(x.Symbol, KlineInterval.OneHour, null, null, 2).ConfigureAwait(false);
                                var markPrice = await _client.ExchangeData.GetMarkPriceAsync(x!.Symbol).ConfigureAwait(false);

                                if (data1.Data.First().Volume > 0)

                                    if (data1.Data.FirstOrDefault().HighPrice < markPrice.Data.MarkPrice)
                                    {
                                        var response = await CreatePosition(new SymbolData
                                        {
                                            Mode = CommonOrderSide.Buy,
                                            CurrentPrice = markPrice.Data.MarkPrice,
                                            Symbol = x!.Symbol,
                                        }, 10m, PositionSide.Long).ConfigureAwait(false);
                                        if (response)
                                        {


                                            break;
                                        }
                                    }
                            }

                            if (!positionsAvailableData.Data.Any(y => y.Symbol.ToLower().Equals(x!.Symbol.ToLower()) && y.Quantity != 0))
                            {
                                var data1 = await _client.ExchangeData.GetKlinesAsync(x.Symbol, KlineInterval.OneHour, null, null, 2).ConfigureAwait(false);
                                var markPrice = await _client.ExchangeData.GetMarkPriceAsync(x!.Symbol).ConfigureAwait(false);

                                if (data1.Data.First().Volume > 0)

                                    if (data1.Data.FirstOrDefault().LowPrice > markPrice.Data.MarkPrice)
                                    {
                                        var response = await CreatePosition(new SymbolData
                                        {
                                            Mode = CommonOrderSide.Buy,
                                            CurrentPrice = markPrice.Data.MarkPrice,
                                            Symbol = x!.Symbol,
                                        }, 10m, PositionSide.Long).ConfigureAwait(false);
                                        if (response)
                                        {
                                            break;
                                        }
                                    }
                            }

                        }
                    }

                    return null;
                }
                catch (Exception)
                {
                    return null;
                }
            }

            return null;
        }


        private async Task<bool> CreatePosition(SymbolData position, decimal quantityUsdt, PositionSide positionSide)
        {
            var quantity = Math.Round(decimal.Divide(quantityUsdt, position.CurrentPrice), 6);

            var result = await _client.Trading.PlaceOrderAsync(
                position.Symbol, (OrderSide)position.Mode!.Value, FuturesOrderType.Market, quantity, null, positionSide, null, null);

            if (!result.Success)
            {
                quantity = Math.Round(decimal.Divide(quantityUsdt, position.CurrentPrice), 5);
                result = await _client.Trading.PlaceOrderAsync(
                position.Symbol, (OrderSide)position.Mode!.Value, FuturesOrderType.Market, quantity, null, positionSide, null, null);
            }
            if (!result.Success)
            {
                quantity = Math.Round(decimal.Divide(quantityUsdt, position.CurrentPrice), 4);
                result = await _client.Trading.PlaceOrderAsync(
                    position.Symbol, (OrderSide)position.Mode!.Value, FuturesOrderType.Market, quantity, null, positionSide, null, null);
            }
            if (!result.Success)
            {
                quantity = Math.Round(decimal.Divide(quantityUsdt, position.CurrentPrice), 3);
                result = await _client.Trading.PlaceOrderAsync(position.Symbol, (OrderSide)position.Mode!.Value, FuturesOrderType.Market, quantity, null, positionSide, null, null);
            }
            if (!result.Success)
            {
                quantity = Math.Round(decimal.Divide(quantityUsdt, position.CurrentPrice), 2);
                result = await _client.Trading.PlaceOrderAsync(position.Symbol, (OrderSide)position.Mode!.Value, FuturesOrderType.Market, quantity, null, positionSide, null, null);
            }
            if (!result.Success)
            {
                quantity = Math.Round(decimal.Divide(quantityUsdt, position.CurrentPrice), 1);
                result = await _client.Trading.PlaceOrderAsync(position.Symbol, (OrderSide)position.Mode!.Value, FuturesOrderType.Market, quantity, null, positionSide, null, null);
            }
            if (!result.Success)
            {
                quantity = Math.Round(decimal.Divide(quantityUsdt, position.CurrentPrice));
                result = await _client.Trading.PlaceOrderAsync(position.Symbol, (OrderSide)position.Mode!.Value, FuturesOrderType.Market, quantity, null, positionSide, null, null);
            }

            return result.Success;
        }

        #region Close Position

        public async Task<Position?> ClosePositions()
        {
            var positionsAvailableData =
               await _client.CommonFuturesClient.GetPositionsAsync().ConfigureAwait(false);


            if (positionsAvailableData.Success)
            {
                var positionsToBeAnalyzed = positionsAvailableData.Data
               .Where(x =>
                   x != null
                   && x.Quantity != 0).ToList();
                {
                    var loss = positionsAvailableData.Data.Sum(x => x.UnrealizedPnl);
                    {
                        foreach (var position in positionsToBeAnalyzed.Where(x => x.UnrealizedPnl > .1M))
                        {
                            if (position.Quantity > 0)
                            {
                                var res = await CreateOrdersLogic(position.Symbol, CommonOrderSide.Sell, position.Side, position.Quantity, position.MarkPrice).ConfigureAwait(false);
                                if (!res)
                                {
                                    res = await CreateOrdersLogic(position.Symbol, CommonOrderSide.Sell, position.Side, position.Quantity, position.MarkPrice).ConfigureAwait(false);
                                }
                            }
                            if (position.Quantity < 0)
                            {
                                var res = await CreateOrdersLogic(position.Symbol, CommonOrderSide.Buy, position.Side, position.Quantity, position.MarkPrice).ConfigureAwait(false);
                                if (!res)
                                {
                                    await CreateOrdersLogic(position.Symbol, CommonOrderSide.Buy, position.Side, position.Quantity, position.MarkPrice).ConfigureAwait(false);
                                }
                            }
                        }

                        foreach (var position in positionsToBeAnalyzed.Where(x => x.UnrealizedPnl < -0.04M))
                        {
                            if (position.Quantity > 0)
                            {
                                var res = await CreateOrdersLogic(position.Symbol, CommonOrderSide.Sell, position.Side, position.Quantity * .25M, position.MarkPrice).ConfigureAwait(false);
                                if (!res)
                                {
                                    res = await CreateOrdersLogic(position.Symbol, CommonOrderSide.Sell, position.Side, position.Quantity, position.MarkPrice).ConfigureAwait(false);
                                }
                            }
                            if (position.Quantity < 0)
                            {
                                var res = await CreateOrdersLogic(position.Symbol, CommonOrderSide.Buy, position.Side, position.Quantity * .25M, position.MarkPrice).ConfigureAwait(false);
                                if (!res)
                                {
                                    await CreateOrdersLogic(position.Symbol, CommonOrderSide.Buy, position.Side, position.Quantity, position.MarkPrice).ConfigureAwait(false);
                                }
                            }
                        }

                        foreach (var position in positionsToBeAnalyzed.Where(x => x.Quantity != 0 && Math.Abs(x.Quantity * x.EntryPrice!.Value) <  2M))
                        {
                            if (position.Quantity > 0)
                            {
                                var res = await CreateOrdersLogic(position.Symbol, CommonOrderSide.Sell, position.Side, position.Quantity, position.MarkPrice).ConfigureAwait(false);
                                if (!res)
                                {
                                    res = await CreateOrdersLogic(position.Symbol, CommonOrderSide.Sell, position.Side, position.Quantity, position.MarkPrice).ConfigureAwait(false);
                                }
                            }
                            if (position.Quantity < 0)
                            {
                                var res = await CreateOrdersLogic(position.Symbol, CommonOrderSide.Buy, position.Side, position.Quantity, position.MarkPrice).ConfigureAwait(false);
                                if (!res)
                                {
                                    await CreateOrdersLogic(position.Symbol, CommonOrderSide.Buy, position.Side, position.Quantity, position.MarkPrice).ConfigureAwait(false);
                                }
                            }
                        }

                    }
                }
            }
            return null;
        }

        public async Task<bool> CreateOrdersLogic(string symbol, CommonOrderSide orderSide, CommonPositionSide? positionSide, decimal quantity, decimal? markPrice)
        {
            quantity = Math.Abs(quantity);
            quantity = Math.Round(quantity, 6);
            var positionSideCurrent = positionSide == CommonPositionSide.Short ? PositionSide.Short : PositionSide.Long;
            var result = await _client.Trading.PlaceOrderAsync(symbol, (OrderSide)orderSide, FuturesOrderType.Market, quantity, null, positionSideCurrent, null, null, null);

            if (!result.Success)
            {
                quantity = Math.Round(quantity, 5);
                result = await _client.Trading.PlaceOrderAsync(symbol, (OrderSide)orderSide, FuturesOrderType.Market, quantity, null, positionSideCurrent, null, null);
            }
            if (!result.Success)
            {
                quantity = Math.Round(quantity, 4);
                result = await _client.Trading.PlaceOrderAsync(symbol, (OrderSide)orderSide, FuturesOrderType.Market, quantity, null, positionSideCurrent, null, null);
            }
            if (!result.Success)
            {
                quantity = Math.Round(quantity, 3);
                result = await _client.Trading.PlaceOrderAsync(symbol, (OrderSide)orderSide, FuturesOrderType.Market, quantity, null, positionSideCurrent, null, null);
            }
            if (!result.Success)
            {
                quantity = Math.Round(quantity, 2);
                result = await _client.Trading.PlaceOrderAsync(symbol, (OrderSide)orderSide, FuturesOrderType.Market, quantity, null, positionSideCurrent, null, null);
            }
            if (!result.Success)
            {
                quantity = Math.Round(quantity, 1);
                result = await _client.Trading.PlaceOrderAsync(symbol, (OrderSide)orderSide, FuturesOrderType.Market, quantity, null, positionSideCurrent, null, null);
            }
            if (!result.Success)
            {
                quantity = Math.Round(quantity, 0);
                result = await _client.Trading.PlaceOrderAsync(symbol, (OrderSide)orderSide, FuturesOrderType.Market, quantity, null, positionSideCurrent, null, null);
            }

            if (!result.Success)
            {
                Console.WriteLine(result.Error + symbol);
            }
            else
            {
                Console.WriteLine("Closed : " + symbol + "     " + orderSide);
            }

            return result.Success;
        }


        #endregion
    }
}
