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
                     && !x.Symbol.ToLower().Contains("xem")
                    && !x.Symbol.ToLower().Contains("btc")).ToList();

            var finalList = new List<FinalResult>();
            foreach (var pos in positionsToBeAnalyzed)
            {
                var data = await _dataHistoryRepository.GetDataByInterval(pos.Symbol, _client, klineInterval).ConfigureAwait(false);
                if (data.Volume.Last() > 0)
                {
                    var rsiValue = GetRsiValue(data);
                    var adxValue = GetAdxValue(data);
                    finalList.Add(new FinalResult { OrderSide = rsiValue?.OrderSide, Position = pos, DataHistory = data, AdxValue = adxValue });
                }
            }

            #region Order Region
            var currentMaxAdx = finalList.Where(x => x.Position.Quantity != 0).MinBy(x => x.AdxValue);
            var orders = finalList.Where(x => x.OrderSide != null).OrderByDescending(x => Math.Abs(x.Position.Quantity)).ThenByDescending(x => x.AdxValue);
            var loss = positionsToBeAnalyzed.Sum(x => x.UnrealizedPnl);
            foreach (var order in orders)
            {
                
                if (order != null)
                {
                    if (order?.Position?.Quantity == 0 && order.AdxValue > currentMaxAdx?.AdxValue && currentMaxAdx?.Position?.UnrealizedPnl > 1M)
                    {
                        switch (currentMaxAdx?.Position?.Quantity)
                        {
                            case > 0:
                                await CreateOrdersLogic(currentMaxAdx!.Position!.Symbol, CommonOrderSide.Sell, currentMaxAdx!.Position!.Quantity, true);
                                break;
                            case < 0:
                                await CreateOrdersLogic(currentMaxAdx!.Position!.Symbol, CommonOrderSide.Buy, currentMaxAdx!.Position!.Quantity, true);
                                break;
                        }
                    }
                    if (order?.Position?.Quantity == 0 && order.OrderSide == CommonOrderSide.Buy)
                    {
                        await CreatePosition(new SymbolData
                        {
                            Mode = CommonOrderSide.Buy,
                            CurrentPrice = order!.Position!.MarkPrice!.Value,
                            Symbol = order!.Position!.Symbol,
                        }, 100M).ConfigureAwait(false);
                        return null;
                    }
                    if (order?.Position?.Quantity == 0 && order.OrderSide == CommonOrderSide.Sell)
                    {
                        await CreatePosition(new SymbolData
                        {
                            Mode = CommonOrderSide.Sell,
                            CurrentPrice = order!.Position!.MarkPrice!.Value,
                            Symbol = order!.Position!.Symbol,
                        }, 100M).ConfigureAwait(false);
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
            if ((dataAIndicator.Real[dataAIndicator.NBElement - 1] > 50 && dataAIndicator.Real[dataAIndicator.NBElement - 1] < 80) || dataAIndicator.Real[dataAIndicator.NBElement - 1] < 20)
            {
                return new RsiFinalResult { OrderSide = CommonOrderSide.Buy };
            }
            if ((dataAIndicator.Real[dataAIndicator.NBElement - 1] < 50 && dataAIndicator.Real[dataAIndicator.NBElement - 1] > 20) || dataAIndicator.Real[dataAIndicator.NBElement - 1] > 80)
            {
                return new RsiFinalResult { OrderSide = CommonOrderSide.Sell };
            }
            //if (dataAIndicator.Real[dataAIndicator.NBElement - 2] < dataAIndicator.Real[dataAIndicator.NBElement - 1] && dataAIndicator.Real[dataAIndicator.NBElement - 3] < dataAIndicator.Real[dataAIndicator.NBElement - 2])
            //{
            //    return new RsiFinalResult { OrderSide = CommonOrderSide.Sell };
            //}
            //if (dataAIndicator.Real[dataAIndicator.NBElement - 2] > dataAIndicator.Real[dataAIndicator.NBElement - 1] && dataAIndicator.Real[dataAIndicator.NBElement - 3] > dataAIndicator.Real[dataAIndicator.NBElement - 2])
            //{
            //    return new RsiFinalResult { OrderSide = CommonOrderSide.Buy };
            //}
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

            var unRealPL = positionsAvailableData.Data.Sum(x => x.UnrealizedPnl);
            Position? position = new Position();
            position = positionsAvailableData.Data.FirstOrDefault(x => x.UnrealizedPnl < -0.5M || x.UnrealizedPnl > 1M);

            switch (position?.Quantity)
            {
                case > 0:
                    await CreateOrdersLogic(position.Symbol, CommonOrderSide.Sell, position.Quantity, true);
                    if (position.UnrealizedPnl < 0)
                    {
                        await CreatePosition(new SymbolData
                        {
                            Mode = CommonOrderSide.Sell,
                            CurrentPrice = position!.MarkPrice!.Value,
                            Symbol = position!.Symbol,
                        }, 100M).ConfigureAwait(false);
                    }
                    break;
                case < 0:
                    await CreateOrdersLogic(position.Symbol, CommonOrderSide.Buy, position.Quantity, true);
                    if (position.UnrealizedPnl < 0)
                    {
                        await CreatePosition(new SymbolData
                        {
                            Mode = CommonOrderSide.Buy,
                            CurrentPrice = position!.MarkPrice!.Value,
                            Symbol = position!.Symbol,
                        }, 100M).ConfigureAwait(false);
                    }
                    break;
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
