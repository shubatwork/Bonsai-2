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
            var longProfit = positionsAvailableData.Data.Where(x => x.Quantity > 0).Sum(x => x.UnrealizedPnl);
            var shortProfit = positionsAvailableData.Data.Where(x => x.Quantity < 0).Sum(x => x.UnrealizedPnl);
            var orders = finalList.Where(x => x.OrderSide != null && x.AdxValue > 30).OrderByDescending(x => x.AdxValue);
            var minAdx = finalList.Where(x => x.Position.Quantity != 0).MinBy(x => x.AdxValue);
            foreach (var order in orders)
            {
                if (order != null)
                {
                    var hourData = await _client.ExchangeData.GetKlinesAsync(order!.Position!.Symbol, KlineInterval.OneHour, null, null, 1).ConfigureAwait(false);
                    var fifteenMinData = await _client.ExchangeData.GetKlinesAsync(order!.Position!.Symbol, KlineInterval.FifteenMinutes, null, null, 1).ConfigureAwait(false);
                    var hourtrend = hourData.Data.FirstOrDefault()?.OpenPrice < order!.Position!.MarkPrice!.Value ? CommonOrderSide.Buy : CommonOrderSide.Sell;
                    var fifteenMintrend = fifteenMinData.Data.FirstOrDefault()?.OpenPrice < order!.Position!.MarkPrice!.Value ? CommonOrderSide.Buy : CommonOrderSide.Sell;

                    if (order?.Position?.Quantity == 0 
                        && order.OrderSide == CommonOrderSide.Buy 
                        && longProfit >= -2
                        && hourtrend == CommonOrderSide.Buy
                        && fifteenMintrend == CommonOrderSide.Buy)
                    {
                        if(minAdx!.AdxValue < order.AdxValue && minAdx!.Position!.UnrealizedPnl > 0.03M)
                        {
                            switch (minAdx?.Position.Quantity)
                            {
                                case > 0:
                                    await CreateOrdersLogic(minAdx.Position.Symbol, CommonOrderSide.Sell, minAdx.Position.Quantity, true);
                                    break;
                                case < 0:
                                    await CreateOrdersLogic(minAdx.Position.Symbol, CommonOrderSide.Buy, minAdx.Position.Quantity, true);
                                    break;
                            }
                        }
                        await CreatePosition(new SymbolData
                        {
                            Mode = CommonOrderSide.Buy,
                            CurrentPrice = order!.Position!.MarkPrice!.Value,
                            Symbol = order!.Position!.Symbol,
                        }, 10M).ConfigureAwait(false);
                        return null;
                    }

                    if (order?.Position?.Quantity == 0
                        && order.OrderSide == CommonOrderSide.Sell
                        && shortProfit >= -2
                        && hourtrend == CommonOrderSide.Sell
                        && fifteenMintrend == CommonOrderSide.Sell
                        )
                    {
                        if (minAdx!.AdxValue < order.AdxValue && minAdx!.Position!.UnrealizedPnl > 0.03M)
                        {
                            switch (minAdx?.Position.Quantity)
                            {
                                case > 0:
                                    await CreateOrdersLogic(minAdx.Position.Symbol, CommonOrderSide.Sell, minAdx.Position.Quantity, true);
                                    break;
                                case < 0:
                                    await CreateOrdersLogic(minAdx.Position.Symbol, CommonOrderSide.Buy, minAdx.Position.Quantity, true);
                                    break;
                            }
                        }
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
            if (dataAIndicator.Real[dataAIndicator.NBElement - 1] > 40 && dataAIndicator.Real[dataAIndicator.NBElement - 1] < 70)
            {
                return new RsiFinalResult { OrderSide = CommonOrderSide.Buy };
            }
            if (dataAIndicator.Real[dataAIndicator.NBElement - 1] < 60 && dataAIndicator.Real[dataAIndicator.NBElement - 1] > 30)
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

            var position = positionsToBeAnalyzed.Where(x => x.UnrealizedPnl > 0.05M).FirstOrDefault();
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
