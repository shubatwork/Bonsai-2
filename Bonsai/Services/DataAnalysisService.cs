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
            var isSellMode = false;
            var isBuyMode = false;
            await ClosePositions().ConfigureAwait(false);
            var positionsAvailableData =
               await _client.CommonFuturesClient.GetPositionsAsync().ConfigureAwait(false);

            if(positionsAvailableData.Data.Count(x=>x.UnrealizedPnl > -0.03M) > 0)
            {
                return null;
            }

            var sellProfit = positionsAvailableData.Data.Where(x => x.Quantity < 0).Sum(x => x.UnrealizedPnl);
            var buyProfit = positionsAvailableData.Data.Where(x => x.Quantity > 0).Sum(x => x.UnrealizedPnl);

            if (sellProfit <= buyProfit)
            {
                isBuyMode = true;
            }
            if (sellProfit >= buyProfit)
            {
                isSellMode = true;
            }

            var positionsToBeAnalyzed = positionsAvailableData.Data
                .Where(x =>
                    x != null
                    && x.MarkPrice > 0
                    && Math.Abs(x.Quantity * x.EntryPrice!.Value) < 95
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
                    && !x.Symbol.ToLower().Contains("eos")
                    && !x.Symbol.ToLower().Contains("btc")).ToList();



            var hourlyResultList = new List<DailyResult>();


            foreach (var pos in positionsToBeAnalyzed)
            {
                CommonOrderSide? hourTrend = null;
                var data = await _dataHistoryRepository.GetDataByInterval(pos.Symbol, _client, KlineInterval.FiveMinutes).ConfigureAwait(false);
                if (data.Volume.Last() > 0)
                {
                    var adxValue = GetAdxValue(data);
                    if (adxValue > 0)
                    {
                        var rsiValue = GetRsiValue(data);
                        if (rsiValue > 80)
                        {
                            hourTrend = CommonOrderSide.Sell;
                        }
                        if (rsiValue > 50 && rsiValue < 70)
                        {
                            hourTrend = CommonOrderSide.Buy;
                        }
                        if (rsiValue < 50 && rsiValue > 30)
                        {
                            hourTrend = CommonOrderSide.Sell;
                        }
                        if (rsiValue < 20)
                        {
                            hourTrend = CommonOrderSide.Buy;
                        }
                        if (hourTrend != null)
                        {
                            hourlyResultList.Add(new DailyResult { OrderSide = hourTrend, Symbol = pos.Symbol, AdxValue = adxValue, RsiValue = rsiValue });
                        }
                    }
                }
            }

            var finalList = new List<FinalResult>();
            if (hourlyResultList.Any())
            {
                foreach (var pos in hourlyResultList.OrderByDescending(x => x.AdxValue))
                {
                    finalList.Add(new FinalResult
                    {
                        OrderSide = pos.OrderSide,
                        Position = positionsAvailableData.Data.First(x => x.Symbol == pos.Symbol),
                        RsiValue = pos.RsiValue
                    });
                }
            }
            else
            {
                return null;
            }

            foreach (var order in finalList)
            {
                if (order != null)
                {
                    if ((order?.Position?.Quantity >= 0)
                        && order.OrderSide == CommonOrderSide.Buy && isBuyMode)
                    {
                            await CreatePosition(new SymbolData
                            {
                                Mode = CommonOrderSide.Buy,
                                CurrentPrice = order!.Position!.MarkPrice!.Value,
                                Symbol = order!.Position!.Symbol,
                            }, 10M).ConfigureAwait(false);
                        return null;
                    }

                    if ((order?.Position?.Quantity <= 0)
                        && order.OrderSide == CommonOrderSide.Sell && isSellMode)
                    {
                            await CreatePosition(new SymbolData
                            {
                                Mode = CommonOrderSide.Sell,
                                CurrentPrice = order!.Position!.MarkPrice!.Value,
                                Symbol = order!.Position!.Symbol,
                            }, 10M).ConfigureAwait(false);
                            return null;
                        }
                }
            }

            #endregion

            return null;
        }

        public async Task<string?> UpdatePositions()
        {
            var positionsAvailableData =
               await _client.CommonFuturesClient.GetPositionsAsync().ConfigureAwait(false);

            var maxLoss = positionsAvailableData.Data.Where(x => Math.Abs(x.Quantity * x.EntryPrice!.Value) < 95).MinBy(x => x.UnrealizedPnl);
            if (maxLoss?.Quantity > 0)
            {
                await CreatePosition(new SymbolData
                {
                    Mode = CommonOrderSide.Buy,
                    CurrentPrice = maxLoss!.MarkPrice!.Value,
                    Symbol = maxLoss.Symbol,
                }, 100M).ConfigureAwait(false);
            }
            if (maxLoss?.Quantity < 0)
            {
                await CreatePosition(new SymbolData
                {
                    Mode = CommonOrderSide.Sell,
                    CurrentPrice = maxLoss!.MarkPrice!.Value,
                    Symbol = maxLoss.Symbol,
                }, 100M).ConfigureAwait(false);

            }

            var maxProfit = positionsAvailableData.Data.Where(x => Math.Abs(x.Quantity * x.EntryPrice!.Value) < 95).MaxBy(x => x.UnrealizedPnl);
            if (maxProfit?.Quantity > 0)
            {
                await CreatePosition(new SymbolData
                {
                    Mode = CommonOrderSide.Buy,
                    CurrentPrice = maxProfit!.MarkPrice!.Value,
                    Symbol = maxProfit.Symbol,
                }, 6M).ConfigureAwait(false);
            }
            if (maxProfit?.Quantity < 0)
            {
                await CreatePosition(new SymbolData
                {
                    Mode = CommonOrderSide.Sell,
                    CurrentPrice = maxProfit!.MarkPrice!.Value,
                    Symbol = maxProfit.Symbol,
                }, 6M).ConfigureAwait(false);

            }

            return null;
        }

        private static double GetAdxValue(DataHistory data)
        {
            data.ComputeAdx();
            AdxResult dataAIndicator = (AdxResult)data.Indicators[Indicator.Adx];
            var currentAdx = dataAIndicator.Real[dataAIndicator.NBElement - 1];
            return currentAdx;
        }

        private static double? GetRsiValue(DataHistory data)
        {
            data.ComputeRsi();
            RsiResult dataAIndicator = (RsiResult)data.Indicators[Indicator.Rsi];
            return dataAIndicator.Real[dataAIndicator.NBElement - 1];
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
            var positionsToBeAnalyzed = positionsAvailableData.Data
               .Where(x =>
                   x != null
                   && !x.Symbol.ToLower().Contains("usdc")
                   && x.Quantity != 0).ToList();

            foreach (var position in positionsToBeAnalyzed.Where(x => x.UnrealizedPnl > .02M))
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

            foreach (var position in positionsToBeAnalyzed.Where(x => x.UnrealizedPnl < -1M))
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
                quantity *= 0.25M;
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
