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

        public async Task<string?> CreatePositions(List<string> notToBeTakenPosition)
        {
            CommonOrderSide? btcSide = null;
            var final = new List<FinalResult>();
            var positionsAvailableData =
               await _client.CommonFuturesClient.GetPositionsAsync().ConfigureAwait(false);
            var task1 = await _client.ExchangeData.GetKlinesAsync("btcusdt", KlineInterval.OneHour, null, null, 1).ConfigureAwait(false);
            var btc = task1.Data.FirstOrDefault();
            var btcCurrent = positionsAvailableData.Data.FirstOrDefault(x => x.Symbol.Equals("BTCUSDT"));
            if (btc!.OpenPrice < btcCurrent!.MarkPrice!.Value)
            {
                btcSide = CommonOrderSide.Buy;
            }
            if (btc!.OpenPrice > btcCurrent!.MarkPrice!.Value)
            {
                btcSide = CommonOrderSide.Sell;
            }

            var closedPositionSymbol = await ClosePositions(btcSide).ConfigureAwait(false);

            var accountInfo = await _client.Account.GetAccountInfoAsync().ConfigureAwait(false);

            var balance = accountInfo.Data.TotalMaintMargin / accountInfo.Data.TotalMarginBalance;
            if (balance > 0.15M)
            {
                return null ;
            }

            var positionsToBeAnalyzed = positionsAvailableData.Data
                .Where(x =>
                    x != null &&
                    x.Quantity == 0
                    && !notToBeTakenPosition.Contains(x.Symbol)
                    && x.MarkPrice > 0
                    && !x.Symbol.ToLower().Contains("bts")
                    && !x.Symbol.ToLower().Contains("hnt")
                    && x.Symbol.ToLower().Contains("usdt")
                    && !x.Symbol.ToLower().Contains("btc")).ToList();

            foreach (var position in positionsToBeAnalyzed)
            {
                var data = await _dataHistoryRepository.GetDataByInterval(position.Symbol, _client, Binance.Net.Enums.KlineInterval.FiveMinutes).ConfigureAwait(false);
                var hourlyAdx = GetAdxValue(data);
                if (hourlyAdx.IsTrending && hourlyAdx.AdxValue > 25)
                {
                    var hourlyMacd = GetMacdValue(data);
                    if (hourlyMacd != null)
                    {
                        data.ComputeRsi();
                        RsiResult dataAIndicator = (RsiResult)data.Indicators[Indicator.Rsi];
                        var rsiValue = dataAIndicator.Real[dataAIndicator.NBElement - 1];
                        if (hourlyMacd?.OrderSide == CommonOrderSide.Buy && rsiValue < 80)
                        {
                            final.Add(new FinalResult
                            {
                                AdxValue = hourlyAdx.AdxValue,
                                Position = position,
                                OrderSide = CommonOrderSide.Buy
                            });
                        }
                        if (hourlyMacd?.OrderSide == CommonOrderSide.Sell && rsiValue > 20)
                        {
                            final.Add(new FinalResult
                            {
                                AdxValue = hourlyAdx.AdxValue,
                                Position = position,
                                OrderSide = CommonOrderSide.Sell
                            });
                        }
                    }
                }
            }
            var finalPosition = final.Where(x => x.OrderSide == btcSide).MaxBy(x => x.AdxValue);
            if (finalPosition != null)
            {
                var symbolData = new SymbolData
                {
                    CurrentPrice = finalPosition.Position!.MarkPrice!.Value,
                    Symbol = finalPosition.Position.Symbol,
                    Mode = finalPosition.OrderSide
                };
                await CreatePosition(symbolData, 15).ConfigureAwait(false);
            }
            return closedPositionSymbol;
        }

        private static AdxFinalResult GetAdxValue(DataHistory data)
        {
            data.ComputeAdx();
            AdxResult dataAIndicator = (AdxResult)data.Indicators[Indicator.Adx];
            var currentAdx = dataAIndicator.Real[dataAIndicator.NBElement - 1];
            var listOfAdx = new List<double>();
            for (int i = dataAIndicator.NBElement - 3; i <= dataAIndicator.NBElement - 1; i++)
            {
                listOfAdx.Add(dataAIndicator.Real[i]);
            }
            var isTrending = IsStrictlyIncreasing(listOfAdx);

            return new AdxFinalResult { AdxValue = currentAdx, IsTrending = isTrending };
        }

        private static MacdFinalResult? GetMacdValue(DataHistory data)
        {
            data.ComputeMacd();
            MacdResult dataAIndicator = (MacdResult)data.Indicators[Indicator.Macd];
            var listOfMacdValue = new List<double>();
            for (int i = dataAIndicator.NBElement - 3; i <= dataAIndicator.NBElement - 1; i++)
            {
                listOfMacdValue.Add(dataAIndicator.MacdValue[i]);
            }
            var isIncerasing = IsStrictlyIncreasing(listOfMacdValue);
            if (isIncerasing == true)
            {
                return new MacdFinalResult { OrderSide = CommonOrderSide.Buy };
            }
            var isDecreasing = IsStrictlyDecreasing(listOfMacdValue);
            if (isDecreasing == true)
            {
                return new MacdFinalResult { OrderSide = CommonOrderSide.Sell };
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

            Console.WriteLine(result.Error);
        }

        #endregion

        #region Close Position
        public async Task<string?> ClosePositions(CommonOrderSide? btcMode)
        {
            Position? position = null;
            var positionsAvailableData =
               await _client.CommonFuturesClient.GetPositionsAsync().ConfigureAwait(false);
            var positionsToBeAnalyzed = positionsAvailableData.Data
               .Where(x =>
                   x != null &&
                   x.Quantity != 0).ToList();

            if (btcMode == CommonOrderSide.Buy)
            {
                position = positionsToBeAnalyzed.Where(x => x.Quantity < 0 && x.UnrealizedPnl > 0.03M).MaxBy(x => x.UnrealizedPnl);
            }
            if (btcMode == CommonOrderSide.Sell)
            {
                position = positionsToBeAnalyzed.Where(x => x.Quantity > 0 && x.UnrealizedPnl > 0.03M).MaxBy(x => x.UnrealizedPnl);
            }
            position ??= positionsToBeAnalyzed.Where(x => x.Quantity != 0 && x.UnrealizedPnl > 0.03M).MaxBy(x=>x.UnrealizedPnl);

            switch (position?.Quantity)
            {
                case > 0:
                    await CreateOrdersLogic(position.Symbol, CommonOrderSide.Sell, position.Quantity, true);
                    return position.Symbol;
                case < 0:
                    await CreateOrdersLogic(position.Symbol, CommonOrderSide.Buy, position.Quantity, true);
                    return position.Symbol;
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
