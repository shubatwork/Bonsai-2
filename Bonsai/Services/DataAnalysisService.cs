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

        public async Task<string?> CreatePositionsRSI(KlineInterval klineInterval = KlineInterval.OneMinute, bool check = false)
        {
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
                    && !x.Symbol.ToLower().Contains("xem")
                    && !x.Symbol.ToLower().Contains("eos")
                    && !x.Symbol.ToLower().Contains("btc")).ToList();

            var hourlyResultList = new List<DailyResult>();

            foreach (var pos in positionsToBeAnalyzed)
            {
                var data = await _dataHistoryRepository.GetDataByInterval(pos.Symbol, _client, KlineInterval.OneHour).ConfigureAwait(false);
                var adxValue = GetAdxValue(data);
                CommonOrderSide? mode = null;
                var task1 = await _client.ExchangeData.GetKlinesAsync(pos.Symbol, KlineInterval.OneDay, null, null, 2).ConfigureAwait(false);
                var result = task1.Data.First();
                if (result.HighPrice < pos.MarkPrice)
                {
                    mode = CommonOrderSide.Buy;
                }
                if (result.LowPrice > pos.MarkPrice)
                {
                    mode = CommonOrderSide.Sell;
                }
                if (mode != null)
                {
                    hourlyResultList.Add(new DailyResult { OrderSide = mode, Position = pos, AdxValue = adxValue });
                }
            }

            var positionCount = 0;
            foreach (var order in hourlyResultList.Where(x=> x.AdxValue > 10).OrderByDescending(x => x.AdxValue))
            {
                if (order != null)
                {
                    if (order.OrderSide == CommonOrderSide.Buy && order!.Position!.Quantity == 0 && positionCount < 1)
                    {
                        var response = await CreatePosition(new SymbolData
                        {
                            Mode = CommonOrderSide.Buy,
                            CurrentPrice = order!.Position!.MarkPrice!.Value,
                            Symbol = order!.Position!.Symbol,
                        }, 10M).ConfigureAwait(false);
                        if(response)
                        {
                            positionCount++;
                        }
                    }

                    if ((order.OrderSide == CommonOrderSide.Buy || order.OrderSide == null)&& order!.Position!.Quantity < 0 && order!.Position!.UnrealizedPnl > 0.01M)
                    {
                        await CreateOrdersLogic(order!.Position!.Symbol, CommonOrderSide.Buy, order!.Position!.Quantity, true);
                    }

                    if (order.OrderSide == CommonOrderSide.Buy && order!.Position!.Quantity > 0)
                    {
                        continue;
                    }

                    if (order.OrderSide == CommonOrderSide.Sell && order!.Position!.Quantity == 0 && positionCount < 1)
                    {
                        var response = await CreatePosition(new SymbolData
                        {
                            Mode = CommonOrderSide.Sell,
                            CurrentPrice = order!.Position!.MarkPrice!.Value,
                            Symbol = order!.Position!.Symbol,
                        }, 10M).ConfigureAwait(false);
                        if (response)
                        {
                            positionCount++;
                        }
                    }

                    if ((order.OrderSide == CommonOrderSide.Sell || order.OrderSide == null) && order!.Position!.Quantity > 0 && order!.Position!.UnrealizedPnl > 0.01M)
                    {
                        await CreateOrdersLogic(order!.Position!.Symbol, CommonOrderSide.Sell, order!.Position!.Quantity, true);
                    }

                    if (order.OrderSide == CommonOrderSide.Sell && order!.Position!.Quantity < 0)
                    {
                        continue;
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
            data.Indicators.Remove(Indicator.Adx);
            return currentAdx;
        }

        private static double? GetRsiValue(DataHistory data)
        {
            data.ComputeRsi();
            RsiResult dataAIndicator = (RsiResult)data.Indicators[Indicator.Rsi];
            return dataAIndicator.Real[dataAIndicator.NBElement - 1];
        }

        private async Task<bool> CreatePosition(SymbolData position, decimal quantityUsdt)
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
            return result.Success;
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

                var position = positionsToBeAnalyzed.Where(x=>x.UnrealizedPnl > 0.03M).MaxBy(x => x.UnrealizedPnl);
                switch (position?.Quantity)
                {
                    case > 0:
                        await CreateOrdersLogic(position.Symbol, CommonOrderSide.Sell, position.Quantity, true);
                        break;
                    case < 0:
                        await CreateOrdersLogic(position.Symbol, CommonOrderSide.Buy, position.Quantity, true);
                        break;
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
