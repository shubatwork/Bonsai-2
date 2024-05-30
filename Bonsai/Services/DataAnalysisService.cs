using Binance.Net.Interfaces.Clients.UsdFuturesApi;
using MakeMeRich.Binance.Services.Interfaces;
using TechnicalAnalysis.Business;
using TechnicalAnalysis;
using CryptoExchange.Net.CommonObjects;
using Binance.Net.Enums;
using System.Reflection.Metadata;
using System.Linq;
using CryptoExchange.Net.Interfaces;

namespace Bonsai.Services
{
    public class DataAnalysisService : IDataAnalysisService
    {
        private readonly IBinanceRestClientUsdFuturesApi _client;
        private readonly IDataHistoryRepository _dataHistoryRepository;

        public DataAnalysisService(IDataHistoryRepository dataHistoryRepository)
        {
            _client = ClientDetails.GetClient();
            _dataHistoryRepository = dataHistoryRepository;
        }


        public async Task<string?> CreatePositionsBuy(List<Position?> notToBeCreated)
        {

            var positionsAvailableData =
               await _client.CommonFuturesClient.GetPositionsAsync().ConfigureAwait(false);

            if (positionsAvailableData.Success)
            {
                try
                {
                    var account = await _client.Account.GetAccountInfoAsync().ConfigureAwait(false);

                    if (!account.Success || account.Data.TotalMaintMargin / account.Data.TotalMarginBalance > .3M)
                    {
                        return null;
                    }

                    var data1 = await _client.ExchangeData.GetTickersAsync().ConfigureAwait(false);

                    foreach (var position in data1.Data.Where(x => !x.Symbol.ToLower().Contains("usdc")
                    && !x.Symbol.ToLower().Contains("btc")
                    && !x.Symbol.ToLower().Contains("tia"))
                        .DistinctBy(x => x.Symbol)
                        .OrderByDescending(x => x.PriceChangePercent).Take(10))
                    {
                        var markPrice = await _client.ExchangeData.GetMarkPriceAsync(position.Symbol).ConfigureAwait(false);

                        if (position.PriceChangePercent > 0)
                        {
                            if (!positionsAvailableData.Data.Any(x => x.Symbol.ToLower().Equals(position!.Symbol.ToLower()) && x.Quantity > 0))
                            {
                                var response = await CreatePosition(new SymbolData
                                {
                                    Mode = CommonOrderSide.Buy,
                                    CurrentPrice = markPrice.Data.MarkPrice,
                                    Symbol = position.Symbol,
                                }, 20M, PositionSide.Long).ConfigureAwait(false);
                                if (response)
                                {
                                    Console.WriteLine(position.Symbol);
                                    break;
                                }
                            }
                        }
                    }

                    foreach (var position in data1.Data.Where(x => !x.Symbol.ToLower().Contains("usdc")
                    && !x.Symbol.ToLower().Contains("btc")
                    && !x.Symbol.ToLower().Contains("tia"))
                        .DistinctBy(x => x.Symbol)
                        .OrderBy(x => x.PriceChangePercent).Take(10))
                    {
                        if (position.PriceChangePercent < 0)
                        {
                            var markPrice = await _client.ExchangeData.GetMarkPriceAsync(position.Symbol).ConfigureAwait(false);

                            var dataHistory = await _client.ExchangeData.GetKlinesAsync(position.Symbol, KlineInterval.OneHour, null, null, 1).ConfigureAwait(false);
                            if (dataHistory.Data.FirstOrDefault()!.OpenPrice < markPrice.Data.MarkPrice)
                            {
                                continue;
                            }
                            if (!positionsAvailableData.Data.Any(x => x.Symbol.ToLower().Equals(position!.Symbol.ToLower()) && x.Quantity < 0))
                            {
                                var response = await CreatePosition(new SymbolData
                                {
                                    Mode = CommonOrderSide.Sell,
                                    CurrentPrice = markPrice.Data.MarkPrice,
                                    Symbol = position.Symbol,
                                }, 20M, PositionSide.Short).ConfigureAwait(false);
                                if (response)
                                {
                                    Console.WriteLine(position.Symbol);
                                    break;
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
        public async Task<bool> IncreasePositions()
        {
            var positionsAvailableData =
               await _client.CommonFuturesClient.GetPositionsAsync().ConfigureAwait(false);

            var positionsToBeAnalyzed = positionsAvailableData.Data
                .Where(x => x != null && x.Quantity != 0 && x.UnrealizedPnl > -1M);

            foreach (var positionByAdx in positionsToBeAnalyzed.OrderByDescending(x => x.UnrealizedPnl))
            {
                if (positionByAdx!.Quantity < 0)
                {
                    if (!positionsAvailableData.Data.Any(x => x.Symbol.ToLower().Equals(positionByAdx.Symbol.ToLower()) && x.Quantity > 0))
                    {
                        var response = await CreatePosition(new SymbolData
                        {
                            Mode = CommonOrderSide.Buy,
                            CurrentPrice = positionByAdx!.MarkPrice!.Value,
                            Symbol = positionByAdx!.Symbol,
                        }, 10M, PositionSide.Long).ConfigureAwait(false);
                    }
                }
                if (positionByAdx!.Quantity > 0)
                {
                    if (!positionsAvailableData.Data.Any(x => x.Symbol.ToLower().Equals(positionByAdx.Symbol.ToLower()) && x.Quantity > 0))
                    {
                        var response = await CreatePosition(new SymbolData
                        {
                            Mode = CommonOrderSide.Sell,
                            CurrentPrice = positionByAdx!.MarkPrice!.Value,
                            Symbol = positionByAdx!.Symbol,
                        }, 100M, PositionSide.Short).ConfigureAwait(false);
                    }
                }
            }

            return false;
        }


        private static double GetAdxValue(DataHistory data)
        {
            data.ComputeAdx();
            AdxResult dataAIndicator = (AdxResult)data.Indicators[Indicator.Adx];
            var currentAdx = dataAIndicator.Real[dataAIndicator.NBElement - 1];
            data.Indicators.Remove(Indicator.Adx);
            return currentAdx;
        }

        private static BollingerResult GetBollingerValue(DataHistory data)
        {
            data.ComputeSma();
            SmaResult dataAIndicator = (SmaResult)data.Indicators[Indicator.Sma];
            var bollingerResult = new BollingerResult
            {
                MiddleBand = dataAIndicator.Real[dataAIndicator.NBElement - 1],
            };
            return bollingerResult;
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
            var x = await ClosePositionsProfit().ConfigureAwait(false);
            return x;
        }

        public async Task<Position?> ClosePositionsProfit()
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
                    foreach (var position in positionsToBeAnalyzed)
                    {
                        if (position.Quantity > 0 && position.UnrealizedPnl > .4M)
                        {
                            await CreateOrdersLogic(position.Symbol, CommonOrderSide.Sell, position.Side, position.Quantity, position.MarkPrice).ConfigureAwait(false);
                        }
                        if (position.Quantity < 0 && position.UnrealizedPnl > .4M)
                        {
                            await CreateOrdersLogic(position.Symbol, CommonOrderSide.Buy, position.Side, position.Quantity, position.MarkPrice).ConfigureAwait(false);
                        }
                        if (position.Quantity > 0 && position.UnrealizedPnl < -.2M)
                        {
                            await CreateOrdersLogic(position.Symbol, CommonOrderSide.Sell, position.Side, position.Quantity * .5M, position.MarkPrice).ConfigureAwait(false);
                        }
                        if (position.Quantity < 0 && position.UnrealizedPnl < -.2M)
                        {
                            await CreateOrdersLogic(position.Symbol, CommonOrderSide.Buy, position.Side, position.Quantity * .5M, position.MarkPrice).ConfigureAwait(false);
                            
                        }
                    }
                }
            }
            return null;
        }

        private async Task CreateOrdersLogic(decimal spCost, string symbol, decimal quantity, FuturesOrderType orderType, OrderSide orderSide, PositionSide positionSide)
        {
            quantity = Math.Abs(quantity);
            spCost = Math.Round(spCost, 6);
            var result = await _client.Trading.PlaceOrderAsync(symbol, orderSide
                , orderType, quantity, spCost, positionSide, null, null, null, spCost);

            if (!result.Success)
            {
                spCost = Math.Round(spCost, 5);
                result = await _client.Trading.PlaceOrderAsync(symbol, orderSide,
                    orderType, quantity, spCost, positionSide, null, null, null, spCost);
            }
            if (!result.Success)
            {
                spCost = Math.Round(spCost, 4);
                result = await _client.Trading.PlaceOrderAsync(symbol, orderSide,
                    orderType, quantity, spCost, positionSide, null, null, null, spCost);
            }
            if (!result.Success)
            {
                spCost = Math.Round(spCost, 3);
                result = await _client.Trading.PlaceOrderAsync(symbol, orderSide,
                    orderType, quantity, spCost, positionSide, null, null, null, spCost);
            }
            if (!result.Success)
            {
                spCost = Math.Round(spCost, 2);
                result = await _client.Trading.PlaceOrderAsync(symbol, orderSide,
                    orderType, quantity, spCost, null, null, null, null, spCost);
            }
            if (!result.Success)
            {
                spCost = Math.Round(spCost, 1);
                result = await _client.Trading.PlaceOrderAsync(symbol, orderSide,
                    orderType, quantity, spCost, positionSide, null, null, null, spCost);
            }
            if (!result.Success)
            {
                spCost = Math.Round(spCost, 0);
                result = await _client.Trading.PlaceOrderAsync(symbol, orderSide,
                    orderType, quantity, spCost, positionSide, null, null, null, spCost);
            }

            if (!result.Success)
            {
                Console.WriteLine(result.Error);
            }
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
                Console.WriteLine("Closed : " + symbol);
            }
            
            return result.Success;
        }


        #endregion
    }
}
