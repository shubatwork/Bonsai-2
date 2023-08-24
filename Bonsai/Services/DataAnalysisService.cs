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

        public async Task<string?> CreatePositions(KlineInterval klineInterval = KlineInterval.FiveMinutes)
        {
            var positionsAvailableData =
               await _client.CommonFuturesClient.GetPositionsAsync().ConfigureAwait(false);

            var positionsToBeAnalyzed = positionsAvailableData.Data
                .Where(x =>
                    x != null
                    && x.MarkPrice > 0
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

            var finalList = new List<FinalResult>();
            foreach (var pos in positionsToBeAnalyzed)
            {
                var data = await _dataHistoryRepository.GetDataByInterval(pos.Symbol, _client, klineInterval).ConfigureAwait(false);
                if (data.Volume.Last() > 0)
                {
                    var rsiValue = GetRsiValue(data);
                    finalList.Add(new FinalResult { RsiValue = rsiValue, Position = pos, DataHistory = data });
                }
            }

            #region BuyRegion
            var buyRsiPositions = finalList.Where(x => x.RsiValue < 20 && x.Position!.Quantity >= 0);
            foreach (var buyRsiPosition in buyRsiPositions.OrderByDescending(x => x.RsiValue))
            {
                if (buyRsiPosition != null)
                {
                    await CreatePosition(new SymbolData
                    {
                        Mode = CommonOrderSide.Buy,
                        CurrentPrice = buyRsiPosition!.Position!.MarkPrice!.Value,
                        Symbol = buyRsiPosition!.Position!.Symbol,
                    }, 10M).ConfigureAwait(false);
                    return null;
                }
            }

            var sellRsiPositions = finalList.Where(x => x.RsiValue > 80 && x.Position!.Quantity <= 0);
            foreach (var sellRsiPosition in sellRsiPositions.OrderByDescending(x => x.RsiValue))
            {
                if (sellRsiPosition != null)
                {
                    await CreatePosition(new SymbolData
                    {
                        Mode = CommonOrderSide.Sell,
                        CurrentPrice = sellRsiPosition!.Position!.MarkPrice!.Value,
                        Symbol = sellRsiPosition!.Position!.Symbol,
                    }, 10M).ConfigureAwait(false);
                    return null;
                }
            }
            #endregion

            return null;
        }

        private double GetEma(DataHistory data, int timePeriod)
        {
            data.ComputeEma(timePeriod);
            EmaResult result = (EmaResult)data.Indicators[Indicator.Ema];
            var ema = result.Real[result.NBElement - 1];
            return ema;
        }

        private static double GetAdxValue(DataHistory data)
        {
            data.ComputeAdx();
            AdxResult dataAIndicator = (AdxResult)data.Indicators[Indicator.Adx];
            var currentAdx = dataAIndicator.Real[dataAIndicator.NBElement - 1];
            return currentAdx;
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

            var profit = positionsToBeAnalyzed.Sum(x => x.UnrealizedPnl);

            foreach (var position in positionsToBeAnalyzed)
            {
                if (position.UnrealizedPnl > 1M)
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
