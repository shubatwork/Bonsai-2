using Binance.Net.Interfaces.Clients.UsdFuturesApi;
using MakeMeRich.Binance.Services.Interfaces;
using TechnicalAnalysis.Business;
using TechnicalAnalysis;
using CryptoExchange.Net.CommonObjects;
using Binance.Net.Enums;
using System.Net;

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

        #region Create Position

        public async Task<string?> CreatePositionsRSI(KlineInterval klineInterval = KlineInterval.OneMinute)
        {
            var positionsAvailableData =
               await _client.CommonFuturesClient.GetPositionsAsync().ConfigureAwait(false);

            if (!positionsAvailableData.Success)
            {
                throw new Exception(positionsAvailableData.Error?.Message);
            }
            var balance = await _client.Account.GetAccountInfoAsync(10000).ConfigureAwait(false);
            var totalMarginBalance = balance.Data.TotalMarginBalance;
            var totalMaintMargin = balance.Data.TotalMaintMargin;

            if ((totalMaintMargin / totalMarginBalance) < 0.2M && (totalMaintMargin / totalMarginBalance) > 0.1M)
            {
                var maxProfit = positionsAvailableData.Data.Where(x => x.Quantity != 0
                && Math.Abs(x.EntryPrice!.Value * x.Quantity) < 95
                && Math.Abs(x.EntryPrice!.Value * x.Quantity * 0.01M) < x.UnrealizedPnl).MaxBy(x => x.UnrealizedPnl);
                {
                    if (maxProfit != null)
                    {
                        if (maxProfit?.Quantity >= 0)
                        {
                            await CreatePosition(new SymbolData
                            {
                                Mode = CommonOrderSide.Buy,
                                CurrentPrice = maxProfit!.MarkPrice!.Value,
                                Symbol = maxProfit!.Symbol,
                            }, 6M).ConfigureAwait(false);
                        }

                        if (maxProfit?.Quantity <= 0)
                        {
                            await CreatePosition(new SymbolData
                            {
                                Mode = CommonOrderSide.Sell,
                                CurrentPrice = maxProfit!.MarkPrice!.Value,
                                Symbol = maxProfit!.Symbol,
                            }, 6M).ConfigureAwait(false);
                        }
                    }
                }
            }
            if ((totalMaintMargin / totalMarginBalance) > 0.1M && (totalMaintMargin / totalMarginBalance) < 0.3M)
            {
                var minProfit = positionsAvailableData.Data.Where(x => x.Quantity != 0 
                && Math.Abs(x.EntryPrice!.Value * x.Quantity) < 95
                && -Math.Abs(x.EntryPrice!.Value * x.Quantity * 0.01M) > x.UnrealizedPnl).MinBy(x => x.UnrealizedPnl);
                {
                    if (minProfit != null)
                    {
                        if (minProfit?.Quantity >= 0)
                        {
                            await CreatePosition(new SymbolData
                            {
                                Mode = CommonOrderSide.Buy,
                                CurrentPrice = minProfit!.MarkPrice!.Value,
                                Symbol = minProfit!.Symbol,
                            }, 6M).ConfigureAwait(false);
                        }

                        if (minProfit?.Quantity <= 0)
                        {
                            await CreatePosition(new SymbolData
                            {
                                Mode = CommonOrderSide.Sell,
                                CurrentPrice = minProfit!.MarkPrice!.Value,
                                Symbol = minProfit!.Symbol,
                            }, 6M).ConfigureAwait(false);
                        }
                    }
                }
            }

            if ((totalMaintMargin / totalMarginBalance) > 0.3M)
            {
                return null;
            }



            var positionsToBeAnalyzed = positionsAvailableData.Data
                .Where(x =>
                    x != null
                    && x.MarkPrice > 0
                    && x.Quantity == 0
                    && !x.Symbol.ToLower().Contains("bts")
                    && !x.Symbol.ToLower().Contains("hnt")
                    && x.Symbol.ToLower().Contains("usdt")
                    && !x.Symbol.ToLower().Contains("usdc")
                    && !x.Symbol.ToLower().Contains("scusdt")
                    && !x.Symbol.ToLower().Contains("sol")
                    && !x.Symbol.ToLower().Contains("bnb")
                    && !x.Symbol.ToLower().Contains("foot")
                    && !x.Symbol.ToLower().Contains("ray")
                    && !x.Symbol.ToLower().Contains("xem")
                    && !x.Symbol.ToLower().Contains("btc")).ToList();

            var hourlyResultList = new List<DailyResult>();
            foreach (var pos in positionsToBeAnalyzed)
            {
                CommonOrderSide? hourTrend = null;
                var data = await _dataHistoryRepository.GetDataByInterval(pos.Symbol, _client, KlineInterval.FiveMinutes).ConfigureAwait(false);

                if (data.Volume.Last() > 0 && data.Open.Count() > 30)
                {
                    var adxValue = GetAdxValue(data);
                    if (adxValue > 30)
                    {
                        var task1 = await _client.ExchangeData.GetKlinesAsync(pos.Symbol, KlineInterval.OneHour, null, null, 1).ConfigureAwait(false);
                        var latestData = task1.Data.First();
                        if (pos.MarkPrice > latestData.OpenPrice)
                        {
                            hourTrend = CommonOrderSide.Buy;
                        }
                        if (pos.MarkPrice < latestData.OpenPrice)
                        {
                            hourTrend = CommonOrderSide.Sell;
                        }
                        if (hourTrend != null)
                        {
                            hourlyResultList.Add(new DailyResult { OrderSide = hourTrend, Symbol = pos.Symbol, MarkPrice = pos.MarkPrice, AdxValue = adxValue });
                        }
                    }
                }
            }

            //var fifteenMinResultList = new List<DailyResult>();

            //foreach (var pos in hourlyResultList)
            //{
            //    var data = await _dataHistoryRepository.GetDataByInterval(pos.Symbol, _client, KlineInterval.FifteenMinutes).ConfigureAwait(false);
            //    if (data.Volume.Last() > 0 && data.Open.Count() > 30)
            //    {
            //        var adxValue = GetAdxValue(data);
            //        if (adxValue > 30)
            //        {
            //            var task1 = await _client.ExchangeData.GetKlinesAsync(pos.Symbol, KlineInterval.FifteenMinutes, null, null, 1).ConfigureAwait(false);
            //            var trend = task1.Data.First().OpenPrice < pos.MarkPrice ? CommonOrderSide.Buy : CommonOrderSide.Sell;
            //            if (trend == pos.OrderSide)
            //            {
            //                fifteenMinResultList.Add(new DailyResult { OrderSide = trend, Symbol = pos.Symbol });

            //            }
            //        }
            //    }
            //}

            //var fiveMinResultList = new List<DailyResult>();

            //foreach (var pos in fifteenMinResultList)
            //{
            //    var data = await _dataHistoryRepository.GetDataByInterval(pos.Symbol, _client, KlineInterval.FiveMinutes).ConfigureAwait(false);
            //    if (data.Volume.Last() > 0 && data.Open.Count() > 30)
            //    {
            //        var adxValue = GetAdxValue(data);
            //        if (adxValue > 30)
            //        {
            //            var rsiValue = GetRsiValue(data);
            //            if (rsiValue?.OrderSide != null && rsiValue.OrderSide == pos.OrderSide)
            //            {
            //                fiveMinResultList.Add(new DailyResult { OrderSide = rsiValue?.OrderSide, Symbol = pos.Symbol, AdxValue = adxValue });
            //            }
            //        }
            //    }
            //}

            var finalList = new List<FinalResult>();
            if (hourlyResultList.Any())
            {
                var pos = hourlyResultList.OrderByDescending(x => x.AdxValue).First();
                finalList.Add(new FinalResult
                {
                    OrderSide = pos.OrderSide,
                    Position = positionsAvailableData.Data.First(x => x.Symbol == pos.Symbol),
                });
            }
            else
            {
                return null;
            }

            foreach (var order in finalList)
            {
                if (order != null)
                {
                    if (order?.Position?.Quantity >= 0
                        && order.OrderSide == CommonOrderSide.Buy)
                    {
                        await CreatePosition(new SymbolData
                        {
                            Mode = CommonOrderSide.Buy,
                            CurrentPrice = order!.Position!.MarkPrice!.Value,
                            Symbol = order!.Position!.Symbol,
                        }, 6M).ConfigureAwait(false);
                        return null;
                    }

                    if (order?.Position?.Quantity <= 0
                        && order.OrderSide == CommonOrderSide.Sell)
                    {
                        await CreatePosition(new SymbolData
                        {
                            Mode = CommonOrderSide.Sell,
                            CurrentPrice = order!.Position!.MarkPrice!.Value,
                            Symbol = order!.Position!.Symbol,
                        }, 6M).ConfigureAwait(false);
                        return null;
                    }
                }
            }

            #endregion

            return null;
        }
        private static double GetAdxValue(DataHistory data)
        {
            data.ComputeAdx();
            AdxResult dataAIndicator = (AdxResult)data.Indicators[Indicator.Adx];
            var currentAdx = dataAIndicator.Real[dataAIndicator.NBElement - 1];
            return currentAdx;
        }

        private static RsiFinalResult? GetRsiValue(DataHistory data)
        {
            data.ComputeRsi();
            RsiResult dataAIndicator = (RsiResult)data.Indicators[Indicator.Rsi];
            if (dataAIndicator.Real[dataAIndicator.NBElement - 1] < 80 && dataAIndicator.Real[dataAIndicator.NBElement - 1] > 50)
            {
                return new RsiFinalResult { OrderSide = CommonOrderSide.Buy };
            }
            if (dataAIndicator.Real[dataAIndicator.NBElement - 1] > 20 && dataAIndicator.Real[dataAIndicator.NBElement - 1] < 50)
            {
                return new RsiFinalResult { OrderSide = CommonOrderSide.Sell };
            }
            return null;
        }

        private async Task CreatePosition(SymbolData position, decimal quantityUsdt)
        {
            var quantity = Math.Round(decimal.Divide(quantityUsdt, position.CurrentPrice), 6);

            var result = await _client.CommonFuturesClient.PlaceOrderAsync(
                position.Symbol, position.Mode!.Value, CommonOrderType.Market, quantity);

            if (!result.Success)
            {
                quantity = Math.Round(decimal.Divide(quantityUsdt, position.CurrentPrice), 5);
                result = await _client.CommonFuturesClient.PlaceOrderAsync(
                    position.Symbol, position.Mode!.Value, CommonOrderType.Market, quantity);
            }
            if (!result.Success)
            {
                quantity = Math.Round(decimal.Divide(quantityUsdt, position.CurrentPrice), 4);
                result = await _client.CommonFuturesClient.PlaceOrderAsync(
                    position.Symbol, position.Mode!.Value, CommonOrderType.Market, quantity);
            }
            if (!result.Success)
            {
                quantity = Math.Round(decimal.Divide(quantityUsdt, position.CurrentPrice), 3);
                result = await _client.CommonFuturesClient.PlaceOrderAsync(
                    position.Symbol, position.Mode!.Value, CommonOrderType.Market, quantity);
            }
            if (!result.Success)
            {
                quantity = Math.Round(decimal.Divide(quantityUsdt, position.CurrentPrice), 2);
                result = await _client.CommonFuturesClient.PlaceOrderAsync(
                    position.Symbol, position.Mode!.Value, CommonOrderType.Market, quantity);
            }
            if (!result.Success)
            {
                quantity = Math.Round(decimal.Divide(quantityUsdt, position.CurrentPrice), 1);
                result = await _client.CommonFuturesClient.PlaceOrderAsync(
                    position.Symbol, position.Mode!.Value, CommonOrderType.Market, quantity);
            }
            if (!result.Success)
            {
                quantity = Math.Round(decimal.Divide(quantityUsdt, position.CurrentPrice));
                result = await _client.CommonFuturesClient.PlaceOrderAsync(
                    position.Symbol, position.Mode!.Value, CommonOrderType.Market, quantity);
            }

            Console.WriteLine(result.Error + position.Symbol);
        }


        #region Close Position
        public async Task<Position?> ClosePositions()
        {
            var positionsAvailableData =
               await _client.CommonFuturesClient.GetPositionsAsync().ConfigureAwait(false);
            var balance = await _client.Account.GetAccountInfoAsync().ConfigureAwait(false);
            var totalMarginBalance = balance.Data.TotalMarginBalance;
            var totalMaintMargin = balance.Data.TotalMaintMargin;

            var positionsToBeAnalyzed = positionsAvailableData.Data
               .Where(x =>
                   x != null
                   && !x.Symbol.ToLower().Contains("usdc")
                   && x.Quantity != 0).ToList();

            foreach (var position in positionsToBeAnalyzed.Where(x => x.UnrealizedPnl > .06M))
            {
                switch (position?.Quantity)
                {
                    case > 0:
                        await CreateOrdersLogic(position.Symbol, CommonOrderSide.Sell, position.Quantity, true);
                        break;
                    case < 0:
                        await CreateOrdersLogic(position.Symbol, CommonOrderSide.Buy, position.Quantity, true);
                        break;
                }
            }


            var marginPercent = totalMaintMargin / totalMarginBalance;
            if (marginPercent > 0.4M)
            {
                var position = positionsToBeAnalyzed.MaxBy(x => x.UnrealizedPnl);
                switch (position?.Quantity)
                {
                    case > 0:
                        await CreateOrdersLogic(position.Symbol, CommonOrderSide.Sell, position.Quantity, true);
                        break;
                    case < 0:
                        await CreateOrdersLogic(position.Symbol, CommonOrderSide.Buy, position.Quantity, true);
                        break;
                }

            }
            return null;

        }
        private async Task CreateOrdersLogic(string symbol, CommonOrderSide orderSide, decimal quantity, bool isGreaterThanMaxProfit)
        {
            if (!isGreaterThanMaxProfit)
            {
                quantity *= 0.8M;
            }
            quantity = Math.Abs(quantity);
            quantity = Math.Round(quantity, 6);
            var result = await _client.CommonFuturesClient.PlaceOrderAsync(symbol, orderSide, CommonOrderType.Market, quantity);

            if (!result.Success)
            {
                quantity = Math.Round(quantity, 5);
                result = await _client.CommonFuturesClient.PlaceOrderAsync(symbol, orderSide, CommonOrderType.Market, quantity);
            }
            if (!result.Success)
            {
                quantity = Math.Round(quantity, 4);
                result = await _client.CommonFuturesClient.PlaceOrderAsync(symbol, orderSide, CommonOrderType.Market, quantity);
            }
            if (!result.Success)
            {
                quantity = Math.Round(quantity, 3);
                result = await _client.CommonFuturesClient.PlaceOrderAsync(symbol, orderSide, CommonOrderType.Market, quantity);
            }
            if (!result.Success)
            {
                quantity = Math.Round(quantity, 2);
                result = await _client.CommonFuturesClient.PlaceOrderAsync(symbol, orderSide, CommonOrderType.Market, quantity);
            }
            if (!result.Success)
            {
                quantity = Math.Round(quantity, 1);
                result = await _client.CommonFuturesClient.PlaceOrderAsync(symbol, orderSide, CommonOrderType.Market, quantity);
            }
            if (!result.Success)
            {
                quantity = Math.Round(quantity, 0);
                result = await _client.CommonFuturesClient.PlaceOrderAsync(symbol, orderSide, CommonOrderType.Market, quantity);
            }

            if (!result.Success)
            {
                Console.WriteLine(result.Error);
            }
        }

        #endregion

        static bool IsStrictlyIncreasing(List<double> numbers)
        {
            for (int i = 0; i < numbers.Count - 1; i++)
            {
                if (double.IsNaN(numbers[i]))
                {
                    return false;
                }
                if (numbers[i] >= numbers[i + 1])
                {
                    return false;
                }
            }
            return true;
        }

        static bool IsStrictlyDecreasing(List<double> numbers)
        {
            for (int i = 0; i < numbers.Count - 1; i++)
            {
                if (double.IsNaN(numbers[i]))
                {
                    return false;
                }
                if (numbers[i] <= numbers[i + 1])
                {
                    return false;
                }
            }
            return true;
        }
    }
}
