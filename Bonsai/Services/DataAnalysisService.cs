﻿using Binance.Net.Interfaces.Clients.UsdFuturesApi;
using MakeMeRich.Binance.Services.Interfaces;
using TechnicalAnalysis.Business;
using TechnicalAnalysis;
using CryptoExchange.Net.CommonObjects;
using Binance.Net.Enums;

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

        public async Task<string?> CreatePositionsBuy()
        {
            var positionsAvailableData =
               await _client.CommonFuturesClient.GetPositionsAsync().ConfigureAwait(false);
           
            if (positionsAvailableData.Data.Count(x=>x.Quantity != 0) >=1)
            {
                return null;
            }

            var positionsToBeAnalyzed = positionsAvailableData.Data
                .Where(x =>
                    x != null
                    && x.MarkPrice > 0
                    && !x.Symbol.ToLower().Contains("btc")
                    && !x.Symbol.ToLower().Contains("usdc")).ToList();

            var hourlyResultList = new List<DailyResult>();
            foreach (var pos in positionsToBeAnalyzed.DistinctBy(x => x.Symbol))
            {
                var percent = await _client.ExchangeData.GetTickerAsync(pos.Symbol).ConfigureAwait(false);
                var x = new DailyResult
                {
                    PercentChange = percent.Data.PriceChangePercent,
                    Position = pos,
                };

                hourlyResultList.Add(x);
            }

            var longPositionsSum = positionsAvailableData.Data.Where(x => x.Quantity > 0).Sum(x => Math.Abs(x.Quantity * x.EntryPrice!.Value));
            var shortPositions = positionsAvailableData.Data.Count(x => x.Quantity < 0);

            if (longPositionsSum / 10 <= shortPositions)
            {
                foreach (var positionByAdx in hourlyResultList.OrderBy(x => x.PercentChange))
                {
                    var pos = positionByAdx?.Position;

                    if (positionsToBeAnalyzed.Any(x => x.Symbol == pos!.Symbol && x.Quantity != 0))
                    {
                        continue;
                    }
                    else
                    {
                        var response = await CreatePosition(new SymbolData
                        {
                            Mode = CommonOrderSide.Buy,
                            CurrentPrice = pos!.MarkPrice!.Value,
                            Symbol = pos!.Symbol,
                        }, 10M, PositionSide.Long).ConfigureAwait(false);
                        if (response)
                        {
                            await CreatePosition(new SymbolData
                            {
                                Mode = CommonOrderSide.Sell,
                                CurrentPrice = pos!.MarkPrice!.Value,
                                Symbol = pos!.Symbol,
                            }, 10M, PositionSide.Short).ConfigureAwait(false);
                            return null;
                        }
                    }
                }

            }
            if (longPositionsSum / 10 >= shortPositions)
            {
                foreach (var positionByAdx in hourlyResultList.OrderByDescending(x => x.PercentChange))
                {
                    var pos = positionByAdx?.Position;

                    if (positionsToBeAnalyzed.Any(x => x.Symbol == pos!.Symbol && x.Quantity != 0))
                    {
                        continue;
                    }
                    else
                    {
                        var response = await CreatePosition(new SymbolData
                        {
                            Mode = CommonOrderSide.Sell,
                            CurrentPrice = pos!.MarkPrice!.Value,
                            Symbol = pos!.Symbol,
                        }, 10M, PositionSide.Short).ConfigureAwait(false);
                        if (response)
                        {
                            await CreatePosition(new SymbolData
                            {
                                Mode = CommonOrderSide.Buy,
                                CurrentPrice = pos!.MarkPrice!.Value,
                                Symbol = pos!.Symbol,
                            }, 10M, PositionSide.Long).ConfigureAwait(false);
                            return null;
                        }
                    }
                }

            }


            //foreach (var positionByAdx in hourlyResultList.OrderBy(x => x.RsiValue))
            //{
            //    var pos = positionByAdx?.Position;

            //    if (positionsToBeAnalyzed.Any(x => x.Symbol == pos!.Symbol && x.Quantity != 0))
            //    {
            //        continue;
            //    }
            //    else
            //    {
            //        var response = await CreatePosition(new SymbolData
            //        {
            //            Mode = CommonOrderSide.Sell,
            //            CurrentPrice = pos!.MarkPrice!.Value,
            //            Symbol = pos!.Symbol,
            //        }, 6M, PositionSide.Short).ConfigureAwait(false);
            //        if (response)
            //        {
            //            response = await CreatePosition(new SymbolData
            //            {
            //                Mode = CommonOrderSide.Buy,
            //                CurrentPrice = pos!.MarkPrice!.Value,
            //                Symbol = pos!.Symbol,
            //            }, 6M, PositionSide.Long).ConfigureAwait(false);
            //            if (response)
            //            {
            //                break;
            //            };
            //        };
            //    }
            //}

            return null;
        }

        public async Task<bool> IncreasePositions()
        {
            var positionsAvailableData =
               await _client.CommonFuturesClient.GetPositionsAsync().ConfigureAwait(false);

            var positionsToBeAnalyzed = positionsAvailableData.Data
                .Where(x => x != null && x.Quantity != 0).ToList();

            foreach (var positionByAdx in positionsToBeAnalyzed.OrderBy(x => x.UnrealizedPnl))
            {
                if (positionsAvailableData.Data.Any(x => x.Symbol == positionByAdx!.Symbol && x.Quantity > 0 && x.UnrealizedPnl > Math.Abs(x.Quantity * x.EntryPrice!.Value * 0.01M)))
                {
                    var response = await CreatePosition(new SymbolData
                    {
                        Mode = CommonOrderSide.Buy,
                        CurrentPrice = positionByAdx!.MarkPrice!.Value,
                        Symbol = positionByAdx!.Symbol,
                    }, 20M, PositionSide.Long).ConfigureAwait(false);

                    return true;
                }
                if (positionsAvailableData.Data.Any(x => x.Symbol == positionByAdx!.Symbol && x.Quantity < 0 && x.UnrealizedPnl > Math.Abs(x.Quantity * x.EntryPrice!.Value * 0.01M)))
                {
                    var response = await CreatePosition(new SymbolData
                    {
                        Mode = CommonOrderSide.Sell,
                        CurrentPrice = positionByAdx!.MarkPrice!.Value,
                        Symbol = positionByAdx!.Symbol,
                    }, 20M, PositionSide.Short).ConfigureAwait(false);
                    return true;
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

        private static double? GetRsiValue(DataHistory data)
        {
            data.ComputeRsi();
            RsiResult dataAIndicator = (RsiResult)data.Indicators[Indicator.Rsi];
            return dataAIndicator.Real[dataAIndicator.NBElement - 1];
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

            Console.WriteLine(result.Error + position.Symbol);
            return result.Success;
        }

        #region Close Position
        public async Task<Position?> ClosePositions()
        {
            await ClosePositionsSingle().ConfigureAwait(false);
            //var positionsAvailableData =
            //   await _client.CommonFuturesClient.GetPositionsAsync().ConfigureAwait(false);

            //var positionsToBeAnalyzed = positionsAvailableData.Data
            //   .Where(x =>
            //       x != null
            //       && !x.Symbol.ToLower().Contains("usdc")
            //       && x.Quantity != 0).ToList();

            //var listOfSymbols = positionsToBeAnalyzed.Select(x => x.Symbol).DistinctBy(x => x);
            //foreach (var symbol in listOfSymbols)
            //{
            //    var profit = positionsToBeAnalyzed.Where(x => x.Symbol == symbol).Sum(x => x.UnrealizedPnl);
            //    if (profit > .3M ||profit < -.1M)
            //    {
            //        var positionToBeClosed = positionsToBeAnalyzed.Where(x => x.Symbol == symbol);
            //        foreach (var position in positionToBeClosed)
            //        {
            //            if (position.Quantity > 0)
            //            {
            //                await CreateOrdersLogic(position.Symbol, CommonOrderSide.Sell, position.Side, position.Quantity, position.MarkPrice).ConfigureAwait(false);
            //            }
            //            if (position.Quantity < 0)
            //            {
            //                await CreateOrdersLogic(position.Symbol, CommonOrderSide.Buy, position.Side, position.Quantity, position.MarkPrice).ConfigureAwait(false);
            //            }
            //        }
            //    }
            //}

            return null;
        }

        public async Task<Position?> ClosePositionsSingle()
        {
            var positionsAvailableData =
               await _client.CommonFuturesClient.GetPositionsAsync().ConfigureAwait(false);

            var positionsToBeAnalyzed = positionsAvailableData.Data
               .Where(x =>
                   x != null
                   && !x.Symbol.ToLower().Contains("usdc")
                   && x.Quantity != 0).ToList();

            foreach (var position in positionsToBeAnalyzed.Where(x =>  x.UnrealizedPnl > .1M || x.UnrealizedPnl < -1M))
            {
                if (position.Quantity > 0)
                {
                    await CreateOrdersLogic(position.Symbol, CommonOrderSide.Sell, position.Side, position.Quantity, position.MarkPrice).ConfigureAwait(false);
                    var response = await CreatePosition(new SymbolData
                    {
                        Mode = CommonOrderSide.Buy,
                        CurrentPrice = position!.MarkPrice!.Value,
                        Symbol = position!.Symbol,
                    }, 10M, PositionSide.Long).ConfigureAwait(false);

                }
                if (position.Quantity < 0)
                {
                    await CreateOrdersLogic(position.Symbol, CommonOrderSide.Buy, position.Side, position.Quantity, position.MarkPrice).ConfigureAwait(false);
                    var response = await CreatePosition(new SymbolData
                    {
                        Mode = CommonOrderSide.Sell,
                        CurrentPrice = position!.MarkPrice!.Value,
                        Symbol = position!.Symbol,
                    }, 10M, PositionSide.Short).ConfigureAwait(false);

                }
            }

            return null;
        }

        public async Task CreateOrdersLogic(string symbol, CommonOrderSide orderSide, CommonPositionSide? positionSide, decimal quantity, decimal? markPrice)
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
                Console.WriteLine(result.Error);
            }
        }
        #endregion
    }
}
