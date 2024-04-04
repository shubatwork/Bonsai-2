using Binance.Net.Interfaces.Clients.UsdFuturesApi;
using MakeMeRich.Binance.Services.Interfaces;
using TechnicalAnalysis.Business;
using TechnicalAnalysis;
using CryptoExchange.Net.CommonObjects;
using Binance.Net.Enums;
using static System.Runtime.InteropServices.JavaScript.JSType;
using CryptoExchange.Net.Interfaces;

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

        public async Task<string?> CreatePositionsBuy()
        {
            try
            {
                var positionsAvailableData =
                   await _client.CommonFuturesClient.GetPositionsAsync().ConfigureAwait(false);
               
                var positionsToBeAnalyzed = positionsAvailableData.Data
                .Where(x =>
                    x != null
                    && x.MarkPrice > 0
                    && !x.Symbol.ToLower().Contains("usdc")).ToList();

                var result = new List<FinalResult>();

                var maxPositionToBeCreated = 1;
                int i = 1;

                foreach (var position in positionsToBeAnalyzed.DistinctBy(x => x.Symbol))
                {
                    var historyData = await _client.ExchangeData.GetKlinesAsync(position.Symbol, KlineInterval.OneDay, null, null, 2).ConfigureAwait(false);
                    var previousDay = historyData.Data.FirstOrDefault();
                    if (previousDay != null)
                    {
                        if (previousDay.HighPrice < position.MarkPrice)
                        {
                            if (!positionsAvailableData.Data.Any(x => x.Symbol.ToLower().Equals(position!.Symbol.ToLower()) && x.Quantity > 0))
                            {
                                if (i < maxPositionToBeCreated)
                                {
                                    var response = await CreatePosition(new SymbolData
                                    {
                                        Mode = CommonOrderSide.Buy,
                                        CurrentPrice = position!.MarkPrice!.Value,
                                        Symbol = position.Symbol,
                                    }, 6M, PositionSide.Long).ConfigureAwait(false);
                                    if (response)
                                    {
                                        i++;
                                    }
                                }
                            }
                        }
                        if (previousDay.LowPrice > position.MarkPrice)
                        {
                            if (!positionsAvailableData.Data.Any(x => x.Symbol.ToLower().Equals(position!.Symbol.ToLower()) && x.Quantity < 0))
                            {
                                if (i < maxPositionToBeCreated)
                                {
                                    var response = await CreatePosition(new SymbolData
                                    {
                                        Mode = CommonOrderSide.Sell,
                                        CurrentPrice = position!.MarkPrice!.Value,
                                        Symbol = position.Symbol,
                                    }, 6M, PositionSide.Short).ConfigureAwait(false);
                                    if (response)
                                    {
                                        i++;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                return null;
            }
            Console.WriteLine("Ended.");
            return null;
        }

        public async Task<bool> IncreasePositions()
        {
            var positionsAvailableData =
               await _client.CommonFuturesClient.GetPositionsAsync().ConfigureAwait(false);

            var positionsToBeAnalyzed = positionsAvailableData.Data
                .Where(x => x != null && x.Quantity != 0 && x.UnrealizedPnl > -1M);

            foreach (var positionByAdx in positionsToBeAnalyzed.OrderByDescending(x => x.UnrealizedPnl))
            {
                if (positionByAdx!.Quantity < 0)
                {
                    if (!positionsAvailableData.Data.Any(x => x.Symbol.ToLower().Equals(positionByAdx.Symbol.ToLower()) && x.Quantity > 0))
                    {
                        var response = await CreatePosition(new SymbolData
                        {
                            Mode = CommonOrderSide.Buy,
                            CurrentPrice = positionByAdx!.MarkPrice!.Value,
                            Symbol = positionByAdx!.Symbol,
                        }, 10M, PositionSide.Long).ConfigureAwait(false);
                    }
                }
                if (positionByAdx!.Quantity > 0)
                {
                    if (!positionsAvailableData.Data.Any(x => x.Symbol.ToLower().Equals(positionByAdx.Symbol.ToLower()) && x.Quantity < 0))
                    {
                        var response = await CreatePosition(new SymbolData
                        {
                            Mode = CommonOrderSide.Sell,
                            CurrentPrice = positionByAdx!.MarkPrice!.Value,
                            Symbol = positionByAdx!.Symbol,
                        }, 10M, PositionSide.Short).ConfigureAwait(false);
                    }
                }
            }

            return false;
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

        private async Task<bool> CreatePosition(SymbolData position, decimal quantityUsdt, PositionSide positionSide)
        {
            var quantity = Math.Round(decimal.Divide(quantityUsdt, position.CurrentPrice), 6);

            var result = await _client.Trading.PlaceOrderAsync(
                position.Symbol, (OrderSide)position.Mode!.Value, FuturesOrderType.Market, quantity, null, positionSide, null, null);

            if (!result.Success)
            {
                quantity = Math.Round(decimal.Divide(quantityUsdt, position.CurrentPrice), 5);
                result = await _client.Trading.PlaceOrderAsync(
                position.Symbol, (OrderSide)position.Mode!.Value, FuturesOrderType.Market, quantity, null, positionSide, null, null);
            }
            if (!result.Success)
            {
                quantity = Math.Round(decimal.Divide(quantityUsdt, position.CurrentPrice), 4);
                result = await _client.Trading.PlaceOrderAsync(
                    position.Symbol, (OrderSide)position.Mode!.Value, FuturesOrderType.Market, quantity, null, positionSide, null, null);
            }
            if (!result.Success)
            {
                quantity = Math.Round(decimal.Divide(quantityUsdt, position.CurrentPrice), 3);
                result = await _client.Trading.PlaceOrderAsync(position.Symbol, (OrderSide)position.Mode!.Value, FuturesOrderType.Market, quantity, null, positionSide, null, null);
            }
            if (!result.Success)
            {
                quantity = Math.Round(decimal.Divide(quantityUsdt, position.CurrentPrice), 2);
                result = await _client.Trading.PlaceOrderAsync(position.Symbol, (OrderSide)position.Mode!.Value, FuturesOrderType.Market, quantity, null, positionSide, null, null);
            }
            if (!result.Success)
            {
                quantity = Math.Round(decimal.Divide(quantityUsdt, position.CurrentPrice), 1);
                result = await _client.Trading.PlaceOrderAsync(position.Symbol, (OrderSide)position.Mode!.Value, FuturesOrderType.Market, quantity, null, positionSide, null, null);
            }
            if (!result.Success)
            {
                quantity = Math.Round(decimal.Divide(quantityUsdt, position.CurrentPrice));
                result = await _client.Trading.PlaceOrderAsync(position.Symbol, (OrderSide)position.Mode!.Value, FuturesOrderType.Market, quantity, null, positionSide, null, null);
            }

            Console.WriteLine(result.Error + position.Symbol);
            return result.Success;
        }

        #region Close Position
        public async Task<Position?> ClosePositions()
        {
            await ClosePositionsCProfit().ConfigureAwait(false);
            return null;
        }

        public async Task<Position?> ClosePositionsSingleLong()
        {

            var positionsAvailableData =
               await _client.CommonFuturesClient.GetPositionsAsync().ConfigureAwait(false);

            var positionsToBeAnalyzed = positionsAvailableData.Data
               .Where(x =>
                   x != null
                   && !x.Symbol.ToLower().Contains("usdc")
                   && x.Quantity != 0).ToList();

            {
                foreach (var position in positionsToBeAnalyzed.Where(x => Math.Abs(x.Quantity * x.MarkPrice!.Value) > 7M))
                {
                    if (position?.Quantity > 0)
                    {
                        await CreateOrdersLogic(position.Symbol, CommonOrderSide.Sell, position.Side, position.Quantity * .9M, position.MarkPrice).ConfigureAwait(false);
                    }
                    if (position?.Quantity < 0)
                    {
                        await CreateOrdersLogic(position.Symbol, CommonOrderSide.Buy, position.Side, position.Quantity * .9M, position.MarkPrice).ConfigureAwait(false);
                    }
                }
            }
            return null;
        }

        public async Task<Position?> ClosePositionsCProfit()
        {

            var positionsAvailableData =
               await _client.CommonFuturesClient.GetPositionsAsync().ConfigureAwait(false);

            var positionsToBeAnalyzed = positionsAvailableData.Data
               .Where(x =>
                   x != null
                   && !x.Symbol.ToLower().Contains("usdc")
                   && x.Quantity != 0).ToList();
            {
                foreach (var position in positionsToBeAnalyzed.Where(x => x.UnrealizedPnl > .1M))
                {
                    if (position?.Quantity > 0)
                    {
                        await CreateOrdersLogic(position.Symbol, CommonOrderSide.Sell, position.Side, position.Quantity, position.MarkPrice).ConfigureAwait(false);
                    }
                    if (position?.Quantity < 0)
                    {
                        await CreateOrdersLogic(position.Symbol, CommonOrderSide.Buy, position.Side, position.Quantity, position.MarkPrice).ConfigureAwait(false);
                    }
                }
            }
            return null;
        }


        public async Task CreateOrdersLogic(string symbol, CommonOrderSide orderSide, CommonPositionSide? positionSide, decimal quantity, decimal? markPrice)
        {
            quantity = Math.Abs(quantity);
            quantity = Math.Round(quantity, 6);
            var positionSideCurrent = positionSide == CommonPositionSide.Short ? PositionSide.Short : PositionSide.Long;
            var result = await _client.Trading.PlaceOrderAsync(symbol, (OrderSide)orderSide, FuturesOrderType.Market, quantity, null, positionSideCurrent, null, null, null);

            if (!result.Success)
            {
                quantity = Math.Round(quantity, 5);
                result = await _client.Trading.PlaceOrderAsync(symbol, (OrderSide)orderSide, FuturesOrderType.Market, quantity, null, positionSideCurrent, null, null);
            }
            if (!result.Success)
            {
                quantity = Math.Round(quantity, 4);
                result = await _client.Trading.PlaceOrderAsync(symbol, (OrderSide)orderSide, FuturesOrderType.Market, quantity, null, positionSideCurrent, null, null);
            }
            if (!result.Success)
            {
                quantity = Math.Round(quantity, 3);
                result = await _client.Trading.PlaceOrderAsync(symbol, (OrderSide)orderSide, FuturesOrderType.Market, quantity, null, positionSideCurrent, null, null);
            }
            if (!result.Success)
            {
                quantity = Math.Round(quantity, 2);
                result = await _client.Trading.PlaceOrderAsync(symbol, (OrderSide)orderSide, FuturesOrderType.Market, quantity, null, positionSideCurrent, null, null);
            }
            if (!result.Success)
            {
                quantity = Math.Round(quantity, 1);
                result = await _client.Trading.PlaceOrderAsync(symbol, (OrderSide)orderSide, FuturesOrderType.Market, quantity, null, positionSideCurrent, null, null);
            }
            if (!result.Success)
            {
                quantity = Math.Round(quantity, 0);
                result = await _client.Trading.PlaceOrderAsync(symbol, (OrderSide)orderSide, FuturesOrderType.Market, quantity, null, positionSideCurrent, null, null);
            }

            if (!result.Success)
            {
                Console.WriteLine(result.Error);
            }
        }
        #endregion
    }
}
