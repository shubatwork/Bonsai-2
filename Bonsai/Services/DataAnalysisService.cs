using Binance.Net.Interfaces.Clients.UsdFuturesApi;
using MakeMeRich.Binance.Services.Interfaces;
using TechnicalAnalysis.Business;
using TechnicalAnalysis;
using CryptoExchange.Net.CommonObjects;
using Binance.Net.Enums;
using System.Runtime.Intrinsics.X86;

namespace Bonsai.Services
{
    public class DataAnalysisService : IDataAnalysisService
    {
        private readonly IBinanceClientUsdFuturesApi _client;
        private readonly IDataHistoryRepository _dataHistoryRepository;

        public DataAnalysisService(IDataHistoryRepository dataHistoryRepository)
        {
            _client = ClientDetails.GetClient();
            _dataHistoryRepository = dataHistoryRepository;
        }

        #region Create Position

        public async Task<string?> CreatePositions(List<string> notToBeTakenPosition)
        {
            //await ClosePositions().ConfigureAwait(false);
            //return null;
            //var positionsAvailableData =
            //   await _client.CommonFuturesClient.GetPositionsAsync().ConfigureAwait(false);
            //var accountInfo = await _client.Account.GetAccountInfoAsync().ConfigureAwait(false);
            //var balance = accountInfo.Data.TotalMaintMargin / accountInfo.Data.TotalMarginBalance;
            //var closedPositionSymbol = await ClosePositions(positionsAvailableData.Data).ConfigureAwait(false);
            //if (!string.IsNullOrEmpty(closedPositionSymbol) && balance < 0.25M)
            //{
            //    positionsAvailableData =
            //     await _client.CommonFuturesClient.GetPositionsAsync().ConfigureAwait(false);
            //    var increasePosition = positionsAvailableData.Data.Where(x => x.UnrealizedPnl > -0.03M && x.UnrealizedPnl < 0M).MaxBy(x => x.UnrealizedPnl);
            //    if (increasePosition != null)
            //    {
            //        var symbolData = new SymbolData
            //        {
            //            CurrentPrice = increasePosition!.MarkPrice!.Value,
            //            Symbol = increasePosition.Symbol,
            //            Mode = increasePosition.Quantity > 0 ? CommonOrderSide.Buy : CommonOrderSide.Sell
            //        };
            //        await CreatePosition(symbolData, 100).ConfigureAwait(false);
            //        return closedPositionSymbol;
            //    }
            //}

            //CommonOrderSide? btcSide = null;
            //var final = new List<FinalResult>();

            //if (positionsAvailableData.Data.Any(x => x.Quantity !=0 && x.UnrealizedPnl > -1M))
            //{
            //    return null;
            //}
            //var task1 = await _client.ExchangeData.GetKlinesAsync("btcusdt", KlineInterval.OneHour, null, null, 1).ConfigureAwait(false);
            //var btc = task1.Data.FirstOrDefault();
            //var btcCurrent = positionsAvailableData.Data.FirstOrDefault(x => x.Symbol.Equals("BTCUSDT"));
            //if (btc!.OpenPrice < btcCurrent!.MarkPrice!.Value)
            //{
            //    btcSide = CommonOrderSide.Buy;
            //}
            //if (btc!.OpenPrice > btcCurrent!.MarkPrice!.Value)
            //{
            //    btcSide = CommonOrderSide.Sell;
            //}

            var positionsAvailableData =
               await _client.CommonFuturesClient.GetPositionsAsync().ConfigureAwait(false);

            if (positionsAvailableData.Data.Sum(x => x.UnrealizedPnl) < -1M)
            {
                return null;
            }

            var positionsToBeAnalyzed = positionsAvailableData.Data
                .Where(x =>
                    x != null
                && x.MarkPrice > 0
                && x.Quantity == 0
                    && Math.Abs(x.Quantity * x.MarkPrice!.Value) < 95
                    && !x.Symbol.ToLower().Contains("bts")
                    && !x.Symbol.ToLower().Contains("hnt")
                    && x.Symbol.ToLower().Contains("usdt")
                    && !x.Symbol.ToLower().Contains("usdc")
                    && !x.Symbol.ToLower().Contains("btc")).ToList();

            var adxList = new List<AdxFinalResult>();
            foreach (var position in positionsToBeAnalyzed)
            {
                var data = await _dataHistoryRepository.GetDataByInterval(position.Symbol, _client, KlineInterval.FiveMinutes).ConfigureAwait(false);
                var hourlyAdx = GetAdxValue(data);
                var ema = GetEma(data);
                var rsi = GetRsiValue(data);
                if (hourlyAdx != null)
                {
                    hourlyAdx.Position = position;
                    hourlyAdx.EmaValue = ema;
                    hourlyAdx.RsiValue = rsi;
                    adxList.Add(hourlyAdx);
                }
            }

            foreach (var adx in adxList.Where(x=>x.RsiValue > 30 && x.RsiValue < 70).OrderByDescending(x => x.AdxValue).Take(1))
            {
                if (adx.EmaValue < (double)adx.Position!.MarkPrice!.Value && adx.Position.Quantity >= 0)
                {
                    await CreatePosition(new SymbolData
                    {
                        Mode = CommonOrderSide.Buy,
                        CurrentPrice = adx.Position!.MarkPrice!.Value,
                        Symbol = adx.Position.Symbol,
                    }, 100).ConfigureAwait(false);
                }

                if (adx.EmaValue > (double)adx.Position!.MarkPrice!.Value && adx.Position.Quantity <= 0)
                {
                    await CreatePosition(new SymbolData
                    {
                        Mode = CommonOrderSide.Sell,
                        CurrentPrice = adx.Position!.MarkPrice!.Value,
                        Symbol = adx.Position.Symbol,
                    }, 100).ConfigureAwait(false);

                }

                //if (adx.RsiValue < 30)
                //{
                //    await CreatePosition(new SymbolData
                //    {
                //        Mode = CommonOrderSide.Buy,
                //        CurrentPrice = adx.Position!.MarkPrice!.Value,
                //        Symbol = adx.Position.Symbol,
                //    }, 10).ConfigureAwait(false);
                //    return null;
                //}
                //if (adx.RsiValue > 70)
                //{
                //    await CreatePosition(new SymbolData
                //    {
                //        Mode = CommonOrderSide.Sell,
                //        CurrentPrice = adx.Position!.MarkPrice!.Value,
                //        Symbol = adx.Position.Symbol,
                //    }, 10).ConfigureAwait(false);
                //    return null;
                //}
                //var task1 = await _client.ExchangeData.GetKlinesAsync("btcusdt", KlineInterval.OneHour, null, null, 1).ConfigureAwait(false);
                //var btc = task1.Data.FirstOrDefault();
                //var btcCurrent = positionsAvailableData.Data.FirstOrDefault(x => x.Symbol.Equals("BTCUSDT"));
                //if (btc!.OpenPrice < btcCurrent!.MarkPrice!.Value)
                //{
                //    await CreatePosition(new SymbolData
                //    {
                //        Mode = CommonOrderSide.Buy,
                //        CurrentPrice = adx.Position!.MarkPrice!.Value,
                //        Symbol = adx.Position.Symbol,
                //    }, 100).ConfigureAwait(false);
                //}
                //if (btc!.OpenPrice > btcCurrent!.MarkPrice!.Value)
                //{
                //    await CreatePosition(new SymbolData
                //    {
                //        Mode = CommonOrderSide.Buy,
                //        CurrentPrice = adx.Position!.MarkPrice!.Value,
                //        Symbol = adx.Position.Symbol,
                //    }, 100).ConfigureAwait(false);
                //}
            }

            //await ClosePositions(positionsAvailableData.Data).ConfigureAwait(false);

            //foreach (var position in positionsToBeAnalyzed)
            //{
            //    var data = await _dataHistoryRepository.GetDataByInterval(position.Symbol, _client, Binance.Net.Enums.KlineInterval.OneHour).ConfigureAwait(false);
            //    var hourlyAdx = GetAdxValue(data);
            //    if (hourlyAdx.IsTrending && hourlyAdx.AdxValue > 25)
            //    {
            //        var hourlyMacd = GetMacdValue(data);
            //        if (hourlyMacd != null)
            //        {
            //            data.ComputeRsi();
            //            RsiResult dataAIndicator = (RsiResult)data.Indicators[Indicator.Rsi];
            //            var rsiValue = dataAIndicator.Real[dataAIndicator.NBElement - 1];
            //            if (hourlyMacd?.OrderSide == CommonOrderSide.Buy && rsiValue < 80)
            //            {
            //                final.Add(new FinalResult
            //                {
            //                    AdxValue = hourlyAdx.AdxValue,
            //                    Position = position,
            //                    OrderSide = CommonOrderSide.Buy
            //                });
            //            }
            //            if (hourlyMacd?.OrderSide == CommonOrderSide.Sell && rsiValue > 20)
            //            {
            //                final.Add(new FinalResult
            //                {
            //                    AdxValue = hourlyAdx.AdxValue,
            //                    Position = position,
            //                    OrderSide = CommonOrderSide.Sell
            //                });
            //            }
            //        }
            //    }
            //}
            //var finalPosition = final.Where(x => x.OrderSide == btcSide).MaxBy(x => x.AdxValue);
            //if (finalPosition != null)
            //{
            //    var symbolData = new SymbolData
            //    {
            //        CurrentPrice = finalPosition.Position!.MarkPrice!.Value,
            //        Symbol = finalPosition.Position.Symbol,
            //        Mode = finalPosition.OrderSide
            //    };
            //    await CreatePosition(symbolData, 100).ConfigureAwait(false);
            //}
            return null;
        }

        private double GetEma(DataHistory data)
        {
            data.ComputeEma();
            EmaResult result = (EmaResult)data.Indicators[Indicator.Ema];
            var ema = result.Real[result.NBElement - 1];
            return ema;
        }

        private static AdxFinalResult GetAdxValue(DataHistory data)
        {
            data.ComputeAdx();
            AdxResult dataAIndicator = (AdxResult)data.Indicators[Indicator.Adx];
            var currentAdx = dataAIndicator.Real[dataAIndicator.NBElement - 1];
            return new AdxFinalResult { AdxValue = currentAdx };
        }

        private static double GetRsiValue(DataHistory data)
        {
            data.ComputeRsi();
            RsiResult dataAIndicator = (RsiResult)data.Indicators[Indicator.Rsi];
            var currentRsi = dataAIndicator.Real[dataAIndicator.NBElement - 1];
            return currentRsi;
        }

        private static MacdFinalResult? GetMacdValue(DataHistory data)
        {
            data.ComputeMacd();
            MacdResult dataAIndicator = (MacdResult)data.Indicators[Indicator.Macd];
            var listOfMacdValue = new List<double>();
            for (int i = dataAIndicator.NBElement - 2; i <= dataAIndicator.NBElement - 1; i++)
            {
                listOfMacdValue.Add(dataAIndicator.MacdValue[i]);
            }
            var isIncerasing = IsStrictlyIncreasing(listOfMacdValue);
            if (isIncerasing == true)
            {
                return new MacdFinalResult { OrderSide = CommonOrderSide.Sell };
            }
            var isDecreasing = IsStrictlyDecreasing(listOfMacdValue);
            if (isDecreasing == true)
            {
                return new MacdFinalResult { OrderSide = CommonOrderSide.Buy };
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

        #endregion

        #region Close Position
        public async Task<string?> ClosePositions()
        {
            var positionsAvailableData =
               await _client.CommonFuturesClient.GetPositionsAsync().ConfigureAwait(false);
            var positionsToBeAnalyzed = positionsAvailableData.Data
               .Where(x =>
                   x != null
                   && !x.Symbol.ToLower().Contains("usdc")
                   && x.Quantity != 0).ToList();

            foreach (var position in positionsToBeAnalyzed.Where(x => x.Quantity != 0 && x.UnrealizedPnl > .3M))
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

            return null;
        }

        private async Task CreateOrdersLogic(string symbol, CommonOrderSide orderSide, decimal quantity, bool isGreaterThanMaxProfit)
        {
            if (!isGreaterThanMaxProfit)
            {
                quantity *= 0.5M;
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
                if (numbers[i] <= numbers[i + 1])
                {
                    return false;
                }
            }
            return true;
        }

    }
}
