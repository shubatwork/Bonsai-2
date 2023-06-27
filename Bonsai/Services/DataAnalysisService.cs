using Binance.Net.Interfaces.Clients.UsdFuturesApi;
using MakeMeRich.Binance.Services.Interfaces;
using TechnicalAnalysis.Business;
using TechnicalAnalysis;
using CryptoExchange.Net.CommonObjects;
using Binance.Net.Enums;
using System.Runtime.Intrinsics.X86;
using System.Collections.Generic;

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

        public async Task<string?> CreatePositions()
        {
            var positionsAvailableData =
               await _client.CommonFuturesClient.GetPositionsAsync().ConfigureAwait(false);

            var loss = positionsAvailableData.Data.Sum(x => x.UnrealizedPnl);
            if(loss < -1M)
            {
                return null;
            }

            foreach(var position in positionsAvailableData.Data.Where(x=>x.Quantity != 0))
            {
                var value = Math.Abs(position.Quantity * position.EntryPrice!.Value);
                if(position.UnrealizedPnl < value * -0.02M)
                {
                    if(position.Quantity > 0)
                    {
                        await CreatePosition(new SymbolData
                        {
                            Mode = CommonOrderSide.Buy,
                            CurrentPrice = position!.MarkPrice!.Value,
                            Symbol = position!.Symbol,
                        }, 6).ConfigureAwait(false);
                        return null;
                    }
                    if (position.Quantity < 0)
                    {
                        await CreatePosition(new SymbolData
                        {
                            Mode = CommonOrderSide.Sell,
                            CurrentPrice = position!.MarkPrice!.Value,
                            Symbol = position!.Symbol,
                        }, 6).ConfigureAwait(false);
                        return null;
                    }
                }
                

            }

            var positionsToBeAnalyzed = positionsAvailableData.Data
                .Where(x =>
                    x != null
                    && x.Quantity == 0
                    && x.MarkPrice > 0
                    && Math.Abs(x.Quantity * x.MarkPrice!.Value) < 95
                    && !x.Symbol.ToLower().Contains("bts")
                    && !x.Symbol.ToLower().Contains("hnt")
                    && x.Symbol.ToLower().Contains("usdt")
                    && !x.Symbol.ToLower().Contains("usdc")
                    && !x.Symbol.ToLower().Contains("scusdt")
                    && !x.Symbol.ToLower().Contains("btc")).ToList();

            var adxList = new List<AdxFinalResult>();
            foreach (var position in positionsToBeAnalyzed)
            {
                var data = await _dataHistoryRepository.GetDataByInterval(position.Symbol, _client, KlineInterval.OneHour).ConfigureAwait(false);
                var hourlyAdx = GetAdxValue(data);
                var rsi = GetRsiValue(data);
                if (hourlyAdx != null && hourlyAdx.AdxValue > 30)
                {
                    hourlyAdx.Position = position;
                    adxList.Add(hourlyAdx);
                }
            }

            var positions = adxList.OrderBy(x=> Guid.NewGuid()).FirstOrDefault();
            var sellLoss = positionsAvailableData.Data.Where(x => x.Quantity < 0).Sum(x => x.UnrealizedPnl);
            var buyLoss = positionsAvailableData.Data.Where(x => x.Quantity > 0).Sum(x => x.UnrealizedPnl);

            if (buyLoss > sellLoss)
            {
                await CreatePosition(new SymbolData
                {
                    Mode = CommonOrderSide.Buy,
                    CurrentPrice = positions!.Position!.MarkPrice!.Value,
                    Symbol = positions!.Position!.Symbol,
                }, 6).ConfigureAwait(false);
                return null;
            }
            else
            {
                await CreatePosition(new SymbolData
                {
                    Mode = CommonOrderSide.Sell,
                    CurrentPrice = positions!.Position!.MarkPrice!.Value,
                    Symbol = positions!.Position!.Symbol,
                }, 6).ConfigureAwait(false);
                return null;
            }
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
        public async Task<Position?> ClosePositions()
        {
            var positionsAvailableData =
               await _client.CommonFuturesClient.GetPositionsAsync().ConfigureAwait(false);
            var positionsToBeAnalyzed = positionsAvailableData.Data
               .Where(x =>
                   x != null
                   && !x.Symbol.ToLower().Contains("usdc")
                   && x.Quantity != 0).ToList();

            foreach (var position in positionsToBeAnalyzed.Where(x => x.UnrealizedPnl > .03M))
            {
                if (position != null)
                {
                    switch (position?.Quantity)
                    {
                        case > 0:
                            await CreateOrdersLogic(position.Symbol, CommonOrderSide.Sell, position.Quantity, true);
                            return position;
                        case < 0:
                            await CreateOrdersLogic(position.Symbol, CommonOrderSide.Buy, position.Quantity, true);
                            return position;
                    }
                }
            }
            return null;
        }

        private async Task CreateOrdersLogic(string symbol, CommonOrderSide orderSide, decimal quantity, bool isGreaterThanMaxProfit)
        {
            if (!isGreaterThanMaxProfit)
            {
                quantity *= 0.9M;
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
