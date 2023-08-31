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

            var balance = await _client.Account.GetAccountInfoAsync().ConfigureAwait(false);
            var totalMarginBalance = balance.Data.TotalMarginBalance;
            var totalMaintMargin = balance.Data.TotalMaintMargin;
            if ((totalMaintMargin / totalMarginBalance) > 0.25M)
            {
                return null;
            }

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

            var hourlyResultList = new List<DailyResult>();
            foreach (var pos in positionsToBeAnalyzed)
            {
                var data = await _dataHistoryRepository.GetDataByInterval(pos.Symbol, _client, KlineInterval.OneHour).ConfigureAwait(false);
                var task1 = await _client.ExchangeData.GetKlinesAsync(pos.Symbol, KlineInterval.OneHour, null, null, 1).ConfigureAwait(false);
                var hourtrend = task1.Data.First().OpenPrice < pos.MarkPrice ? CommonOrderSide.Buy : CommonOrderSide.Sell;
                if (data.Volume.Last() > 0 && data.Open.Count() > 30)
                {
                    var adxValue = GetAdxValue(data);
                    if(adxValue > 30)
                    {
                        var rsiValue = GetRsiValue(data);
                        if (rsiValue?.OrderSide != null && rsiValue.OrderSide == hourtrend)
                        {
                            hourlyResultList.Add(new DailyResult { OrderSide = rsiValue?.OrderSide, Symbol = pos.Symbol, MarkPrice = pos.MarkPrice });
                        }
                    }
                }
            }

            var fifteenMinResultList = new List<DailyResult>();

            foreach (var pos in hourlyResultList)
            {
                var data = await _dataHistoryRepository.GetDataByInterval(pos.Symbol, _client, KlineInterval.FifteenMinutes).ConfigureAwait(false);
                if (data.Volume.Last() > 0 && data.Open.Count() > 30)
                {
                    var adxValue = GetAdxValue(data);
                    if (adxValue > 30)
                    {
                        var task1 = await _client.ExchangeData.GetKlinesAsync(pos.Symbol, KlineInterval.FifteenMinutes, null, null, 1).ConfigureAwait(false);
                        var trend = task1.Data.First().OpenPrice < pos.MarkPrice ? CommonOrderSide.Buy : CommonOrderSide.Sell;
                        var rsiValue = GetRsiValue(data);
                        if (rsiValue?.OrderSide != null && rsiValue.OrderSide == pos.OrderSide && rsiValue.OrderSide == trend)
                        {
                            fifteenMinResultList.Add(new DailyResult { OrderSide = rsiValue?.OrderSide, Symbol = pos.Symbol });

                        }
                    }
                }
            }

            var fiveMinResultList = new List<DailyResult>();

            foreach (var pos in fifteenMinResultList)
            {
                var data = await _dataHistoryRepository.GetDataByInterval(pos.Symbol, _client, KlineInterval.FiveMinutes).ConfigureAwait(false);
                if (data.Volume.Last() > 0 && data.Open.Count() > 30)
                {
                    var adxValue = GetAdxValue(data);
                    if (adxValue > 30)
                    {
                        var rsiValue = GetRsiValue(data);
                        if (rsiValue?.OrderSide != null && rsiValue.OrderSide == pos.OrderSide)
                        {
                            fiveMinResultList.Add(new DailyResult { OrderSide = rsiValue?.OrderSide, Symbol = pos.Symbol });
                        }
                    }
                }
            }

            var finalList = new List<FinalResult>();
            if (fiveMinResultList.Any())
            {
                var pos = fiveMinResultList.First();
                finalList.Add(new FinalResult
                {
                    OrderSide = pos.OrderSide,
                    Position = positionsAvailableData.Data.First(x=> x.Symbol == pos.Symbol),
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
                    if (order?.Position?.Quantity == 0 
                        || (order!.Position!.UnrealizedPnl < -Math.Abs(order!.Position!.Quantity * order!.Position!.EntryPrice!.Value * 0.05M))
                        && order.OrderSide == CommonOrderSide.Buy)
                    {
                        await CreatePosition(new SymbolData
                        {
                            Mode = CommonOrderSide.Buy,
                            CurrentPrice = order!.Position!.MarkPrice!.Value,
                            Symbol = order!.Position!.Symbol,
                        }, 10M).ConfigureAwait(false);
                        return null;
                    }

                    if (order?.Position?.Quantity == 0 
                        || (order!.Position!.UnrealizedPnl > Math.Abs(order!.Position!.Quantity * order!.Position!.EntryPrice!.Value * 0.01M))
                        || (order!.Position!.UnrealizedPnl < -Math.Abs(order!.Position!.Quantity * order!.Position!.EntryPrice!.Value * 0.03M))
                        && order.OrderSide == CommonOrderSide.Sell)
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
            if ((dataAIndicator.Real[dataAIndicator.NBElement - 1] > 40 && dataAIndicator.Real[dataAIndicator.NBElement - 1] < 80) || dataAIndicator.Real[dataAIndicator.NBElement - 1] < 20)
            {
                return new RsiFinalResult { OrderSide = CommonOrderSide.Buy };
            }
            if ((dataAIndicator.Real[dataAIndicator.NBElement - 1] < 60 && dataAIndicator.Real[dataAIndicator.NBElement - 1] > 20) || dataAIndicator.Real[dataAIndicator.NBElement - 1] > 80)
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
            var positionsToBeAnalyzed = positionsAvailableData.Data
               .Where(x =>
                   x != null
                   && !x.Symbol.ToLower().Contains("usdc")
                   && x.Quantity != 0).ToList();

            var positions = positionsToBeAnalyzed.Where(x => x.UnrealizedPnl > .1M);
            foreach (var position in positions)
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

            var balance = await _client.Account.GetAccountInfoAsync().ConfigureAwait(false);
            var totalMarginBalance = balance.Data.TotalMarginBalance;
            var totalMaintMargin = balance.Data.TotalMaintMargin;
            if((totalMaintMargin / totalMarginBalance) > 0.3M) {
                var position = positionsToBeAnalyzed.MaxBy(x=>x.UnrealizedPnl);
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
