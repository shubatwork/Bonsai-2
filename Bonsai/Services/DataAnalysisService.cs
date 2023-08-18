using Binance.Net.Interfaces.Clients.UsdFuturesApi;
using MakeMeRich.Binance.Services.Interfaces;
using TechnicalAnalysis.Business;
using TechnicalAnalysis;
using CryptoExchange.Net.CommonObjects;
using Binance.Net.Enums;

namespace Bonsai.Services
{
    public class DataAnalysisService : IDataAnalysisService
    {
        private readonly IBinanceClientUsdFuturesApi _client;
        private readonly IBinanceClientUsdFuturesApi _clientSona;
        private readonly IDataHistoryRepository _dataHistoryRepository;

        public DataAnalysisService(IDataHistoryRepository dataHistoryRepository)
        {
            _client = ClientDetails.GetClient();
            _clientSona = ClientDetails.GetSonaClient();
            _dataHistoryRepository = dataHistoryRepository;
        }

        #region Create Position

        public async Task<string?> CreatePositions()
        {
            //await ClosePositions().ConfigureAwait(false);
            var positionsAvailableData =
               await _client.CommonFuturesClient.GetPositionsAsync().ConfigureAwait(false);

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
                    && !x.Symbol.ToLower().Contains("btc")).ToList();

            var adxList = new List<FinalResult>();
            foreach (var pos in positionsToBeAnalyzed)
            {
                var data = await _dataHistoryRepository.GetDataByInterval(pos.Symbol, _client, KlineInterval.FiveMinutes).ConfigureAwait(false);
                var adxValue = GetAdxValue(data);
                adxList.Add(new FinalResult { AdxValue = adxValue.AdxValue, Position = pos, DataHistory = data });
            }

            var longProfit = positionsAvailableData.Data.Where(x => x.Quantity > 0).Sum(x => x.UnrealizedPnl);
            var shortProfit = positionsAvailableData.Data.Where(x => x.Quantity < 0).Sum(x => x.UnrealizedPnl);

            #region GetTrending
            foreach (var position in adxList.OrderByDescending(x => x.AdxValue).Take(50))
            {
                var ema3 = GetEma(position.DataHistory, 2);
                position.DataHistory.Indicators.Remove(Indicator.Ema);
                var ema7 = GetEma(position.DataHistory, 5);
                if (ema3 > ema7  && shortProfit > longProfit && shortProfit > -1M)
                {
                    await CreatePosition(new SymbolData
                    {
                        Mode = CommonOrderSide.Sell,
                        CurrentPrice = position!.Position!.MarkPrice!.Value,
                        Symbol = position!.Position!.Symbol,
                    }, 6M).ConfigureAwait(false);
                }
                if (ema3 < ema7 && shortProfit < longProfit && longProfit > -1M)
                {
                    await CreatePosition(new SymbolData
                    {
                        Mode = CommonOrderSide.Buy,
                        CurrentPrice = position!.Position!.MarkPrice!.Value,
                        Symbol = position!.Position!.Symbol,
                    }, 6M).ConfigureAwait(false);
                }
            }

            #endregion
            return null;
        }

        public async Task<string?> CreateContinousPositions()
        {
            //await ClosePositions().ConfigureAwait(false);
            var positionsAvailableData =
               await _client.CommonFuturesClient.GetPositionsAsync().ConfigureAwait(false);

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
                    && !x.Symbol.ToLower().Contains("btc")).ToList();

            var adxList = new List<FinalResult>();
            foreach (var pos in positionsToBeAnalyzed)
            {
                var data = await _dataHistoryRepository.GetDataByInterval(pos.Symbol, _client, KlineInterval.FiveMinutes).ConfigureAwait(false);
                var adxValue = GetAdxValue(data);
                adxList.Add(new FinalResult { AdxValue = adxValue.AdxValue, Position = pos, DataHistory = data });
            }

            #region GetTrending
            foreach (var position in adxList.OrderByDescending(x => x.AdxValue).Take(50))
            {
                var ema3 = GetEma(position.DataHistory, 2);
                position.DataHistory.Indicators.Remove(Indicator.Ema);
                var ema7 = GetEma(position.DataHistory, 5);
                if (ema3 > ema7)
                {
                    await CreatePosition(new SymbolData
                    {
                        Mode = CommonOrderSide.Buy,
                        CurrentPrice = position!.Position!.MarkPrice!.Value,
                        Symbol = position!.Position!.Symbol,
                    }, 6M).ConfigureAwait(false);
                    return null;
                }
                if (ema3 < ema7)
                {
                    await CreatePosition(new SymbolData
                    {
                        Mode = CommonOrderSide.Sell,
                        CurrentPrice = position!.Position!.MarkPrice!.Value,
                        Symbol = position!.Position!.Symbol,
                    }, 6M).ConfigureAwait(false);
                    return null;
                }
            }

            #endregion
            return null;
        }


        public async Task<string?> UpdateLossPositions()
        {
            var positionsAvailableData =
               await _client.CommonFuturesClient.GetPositionsAsync().ConfigureAwait(false);

            var maxLoss = positionsAvailableData.Data.Where(x=> Math.Abs(x.Quantity * x.EntryPrice!.Value) < 7).MinBy(x => x.UnrealizedPnl);

            if (maxLoss?.Quantity > 0)
            {
                await CreatePosition(new SymbolData
                {
                    Mode = CommonOrderSide.Buy,
                    CurrentPrice = maxLoss!.MarkPrice!.Value,
                    Symbol = maxLoss!.Symbol,
                }, 6M).ConfigureAwait(false);
            }
            if (maxLoss?.Quantity < 0)
            {
                await CreatePosition(new SymbolData
                {
                    Mode = CommonOrderSide.Sell,
                    CurrentPrice = maxLoss.MarkPrice!.Value,
                    Symbol = maxLoss.Symbol,
                }, 6M).ConfigureAwait(false);
            }
            return null;
        }


        private double GetEma(DataHistory data, int timePeriod)
        {
            data.ComputeEma(timePeriod);
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

            if (!result.Success)
            {
                quantity = Math.Round(decimal.Divide(7, position.CurrentPrice));
                result = await _client.CommonFuturesClient.PlaceOrderAsync(
                    position.Symbol, position.Mode!.Value, CommonOrderType.Market, quantity);
            }
            if (!result.Success)
            {
                quantity = Math.Round(decimal.Divide(8, position.CurrentPrice));
                result = await _client.CommonFuturesClient.PlaceOrderAsync(
                    position.Symbol, position.Mode!.Value, CommonOrderType.Market, quantity);
            }
            if (!result.Success)
            {
                quantity = Math.Round(decimal.Divide(9, position.CurrentPrice));
                result = await _client.CommonFuturesClient.PlaceOrderAsync(
                    position.Symbol, position.Mode!.Value, CommonOrderType.Market, quantity);
            }
            if (!result.Success)
            {
                quantity = Math.Round(decimal.Divide(10, position.CurrentPrice));
                result = await _client.CommonFuturesClient.PlaceOrderAsync(
                    position.Symbol, position.Mode!.Value, CommonOrderType.Market, quantity);
            }

            Console.WriteLine(result.Error + position.Symbol);
        }

        #endregion

        #region Close Position
        public async Task<Position?> ClosePositions()
        {
            var positionsAvailableData =
               await _client.CommonFuturesClient.GetPositionsAsync().ConfigureAwait(false);
            var positionsToBeAnalyzed = positionsAvailableData.Data
               .Where(x =>
                   x != null
                   && !x.Symbol.ToLower().Contains("usdc")
                   && x.Quantity != 0).ToList();

            foreach (var position in positionsToBeAnalyzed)
            {
                if (position.UnrealizedPnl > 0.02M)
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
                if (numbers[i] <= numbers[i + 1])
                {
                    return false;
                }
            }
            return true;
        }

        public async Task<Position?> CloseSonaPositions()
        {
            var positionsAvailableData =
               await _clientSona.CommonFuturesClient.GetPositionsAsync().ConfigureAwait(false);
            var positionsToBeAnalyzed = positionsAvailableData.Data
               .Where(x =>
                   x != null
                   && !x.Symbol.ToLower().Contains("usdc")
                   && x.Quantity != 0).ToList();


            foreach (var position in positionsToBeAnalyzed.Where(x => x.UnrealizedPnl > .3M))
            {
                if (position != null)
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
            }
            return null;
        }
    }
}
