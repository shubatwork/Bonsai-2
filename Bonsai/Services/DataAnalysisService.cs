using Binance.Net.Interfaces.Clients.UsdFuturesApi;
using MakeMeRich.Binance.Services.Interfaces;
using TechnicalAnalysis.Business;
using TechnicalAnalysis;
using CryptoExchange.Net.CommonObjects;

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

        public async Task GetData()
        {
            var positionsAvailableData =
               await _client.CommonFuturesClient.GetPositionsAsync().ConfigureAwait(false);

            var positionsToBeAnalyzed = positionsAvailableData.Data
                .Where(x =>
                    x != null &&
                    x.MarkPrice > 0
                    && x.Symbol.ToLower().Contains("usdt")
                    && !x.Symbol.ToLower().Contains("btc")).ToList();

            var adxResult = await GetAdxResultAsync(positionsToBeAnalyzed).ConfigureAwait(false);
            var macdResult = await GetMacdResultAsync(adxResult).ConfigureAwait(false);
            var rsiReult = await GetRsiResultAsync(macdResult).ConfigureAwait(false);

            foreach (var position in rsiReult)
            {
                var symbolData = new SymbolData
                {
                    CurrentPrice = position.Position!.MarkPrice!.Value,
                    Symbol = position.Position.Symbol,
                    Mode = position.OrderSide
                };

                await CreatePosition(symbolData, 10).ConfigureAwait(false);
            }
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

        private async Task<List<Position>> GetAdxResultAsync(List<Position> positionsToBeAnalyzed)
        {
            var adxResult = new List<Position>();
            foreach (var position in positionsToBeAnalyzed)
            {
                var data = await _dataHistoryRepository.GetData(position.Symbol, _client).ConfigureAwait(false);
                data.ComputeAdx();
                AdxResult dataAIndicator = (AdxResult)data.Indicators[Indicator.Adx];
                var currentAdx = dataAIndicator.Real[dataAIndicator.NBElement - 1];

                if (currentAdx > 25)
                {
                    var listOfAdx = new List<double>();
                    for (int i = dataAIndicator.NBElement - 6; i <= dataAIndicator.NBElement - 1; i++)
                    {
                        listOfAdx.Add(dataAIndicator.Real[i]);
                    }
                    var isofUse = IsStrictlyIncreasing(listOfAdx);
                    if (isofUse == true)
                    {
                        adxResult.Add(position);
                    }
                }
            }

            return adxResult;
        }

        private async Task<List<MacdFinalResult>> GetMacdResultAsync(List<Position> positionsToBeAnalyzed)
        {
            var macdResult = new List<MacdFinalResult>();
            foreach (var position in positionsToBeAnalyzed)
            {
                var data = await _dataHistoryRepository.GetData(position.Symbol, _client).ConfigureAwait(false);
                data.ComputeMacd();
                MacdResult dataAIndicator = (MacdResult)data.Indicators[Indicator.Macd];
                var macdValue = dataAIndicator.MacdValue[dataAIndicator.NBElement - 1];
                var macdSignalValue = dataAIndicator.MacdSignal[dataAIndicator.NBElement - 1];
                if (macdValue > macdSignalValue)
                {
                    var listOfMacdValue = new List<double>();
                    for (int i = dataAIndicator.NBElement - 11; i <= dataAIndicator.NBElement - 1; i++)
                    {
                        listOfMacdValue.Add(dataAIndicator.MacdValue[i]);
                    }
                    var isofUse = IsStrictlyIncreasing(listOfMacdValue);
                    if (isofUse == true)
                    {
                        macdResult.Add(new MacdFinalResult { Position = position, OrderSide = CommonOrderSide.Buy });
                    }
                }
                if (macdValue < macdSignalValue)
                {
                    var listOfMacdValue = new List<double>();
                    for (int i = dataAIndicator.NBElement - 11; i <= dataAIndicator.NBElement - 1; i++)
                    {
                        listOfMacdValue.Add(dataAIndicator.MacdValue[i]);
                    }
                    var isofUse = IsStrictlyDecreasing(listOfMacdValue);
                    if (isofUse == true)
                    {
                        macdResult.Add(new MacdFinalResult { Position = position, OrderSide = CommonOrderSide.Sell });
                    }
                }
            }

            return macdResult;
        }

        private async Task<List<MacdFinalResult>> GetRsiResultAsync(List<MacdFinalResult> positionsToBeAnalyzed)
        {
            var rsiResult = new List<MacdFinalResult>();
            foreach (var position in positionsToBeAnalyzed)
            {
                var data = await _dataHistoryRepository.GetData(position.Position!.Symbol, _client).ConfigureAwait(false);
                data.ComputeRsi();
                RsiResult dataAIndicator = (RsiResult)data.Indicators[Indicator.Rsi];
                var rsiValue = dataAIndicator.Real[dataAIndicator.NBElement - 1];
                if (rsiValue < 80 && position.OrderSide == CommonOrderSide.Buy)
                {
                    rsiResult.Add(new MacdFinalResult { Position = position.Position, OrderSide = CommonOrderSide.Buy });
                }
                if (rsiValue > 20 && position.OrderSide == CommonOrderSide.Sell)
                {
                    rsiResult.Add(new MacdFinalResult { Position = position.Position, OrderSide = CommonOrderSide.Sell });
                }
            }

            return rsiResult;
        }


        bool IsStrictlyIncreasing(List<double> numbers)
        {
            for (int i = 0; i < numbers.Count - 1; i++)
            {
                if (numbers[i] >= numbers[i + 1])
                {
                    return false;
                }
            }
            return true;
        }

        bool IsStrictlyDecreasing(List<double> numbers)
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
