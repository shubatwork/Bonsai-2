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


        public async Task<FinalResult?> GetMaxAdxValue()
        {
            var positionsAvailableData =
               await _client.CommonFuturesClient.GetPositionsAsync().ConfigureAwait(false);
            if (positionsAvailableData.Success)
            {
                var positionsToBeAnalyzed = positionsAvailableData.Data
                .Where(x =>
                    x != null
                    && x.MarkPrice > 0
                    && !x.Symbol.ToLower().Contains("usdc")
                    && !x.Symbol.ToLower().Contains("aave")
                    && !x.Symbol.ToLower().Contains("avax")
                    && !x.Symbol.ToLower().Contains("bch")
                    && !x.Symbol.ToLower().Contains("btcusdt")
                    && !x.Symbol.ToLower().Contains("bluebird")
                    && !x.Symbol.ToLower().Contains("btsusdt")
                    && !x.Symbol.ToLower().Contains("cocos")
                    && !x.Symbol.ToLower().Contains("ctk")
                    && !x.Symbol.ToLower().Contains("cvc")
                    && !x.Symbol.ToLower().Contains("antusdt")
                    ).ToList();

                var lsit = new List<FinalResult>();

                foreach (var position in positionsToBeAnalyzed.DistinctBy(x => x.Symbol).OrderBy(x => x.Symbol))
                {
                    var data = await _dataHistoryRepository.GetDataByInterval(position.Symbol, _client, KlineInterval.OneMinute).ConfigureAwait(false);
                    var adxValue = GetAdxValue(data);
                    var x = new FinalResult
                    {
                        Position = position,
                        AdxValue = adxValue
                    };
                    
                    lsit.Add(x);
                }

                return lsit.MaxBy(x => x.AdxValue);
            }

            return null;

        }

        public async Task<string?> CreatePositionsBuy()
        {
            
            var positionsAvailableData =
               await _client.CommonFuturesClient.GetPositionsAsync().ConfigureAwait(false);

           // await ClosePositionsLoss().ConfigureAwait(false);
           // await ClosePositionsProfit().ConfigureAwait(false);

            //var accountData = await _client.Account.GetAccountInfoAsync().ConfigureAwait(false);

            //if(accountData.Data.AvailableBalance < 10)
            //{
            //    Console.WriteLine(accountData.Data.TotalMarginBalance);
            //    return null;
            //}

            //var maxAdx = await GetMaxAdxValue();
            //string symbol = maxAdx!.Position!.Symbol;

            //var currentPosition = positionsAvailableData.Data.FirstOrDefault(x => x.Quantity != 0 && x.Symbol != symbol);

            //if (currentPosition != null && currentPosition.Symbol != symbol)
            //{
            //    if (currentPosition?.Quantity > 0)
            //    {
            //        await CreateOrdersLogic(currentPosition.Symbol, CommonOrderSide.Sell, currentPosition.Side, currentPosition.Quantity, currentPosition.MarkPrice).ConfigureAwait(false);
            //    }
            //    if (currentPosition?.Quantity < 0)
            //    {
            //        await CreateOrdersLogic(currentPosition.Symbol, CommonOrderSide.Buy, currentPosition.Side, currentPosition.Quantity, currentPosition.MarkPrice).ConfigureAwait(false);
            //    }
            //}

            if (positionsAvailableData.Success)
            {
                var positionsToBeAnalyzed = positionsAvailableData.Data
                .Where(x =>
                    x != null
                    && x.MarkPrice > 0
                    && !x.Symbol.ToLower().Contains("usdc")
                    && !x.Symbol.ToLower().Contains("aave")
                    && !x.Symbol.ToLower().Contains("avax")
                    && !x.Symbol.ToLower().Contains("bch")
                    && !x.Symbol.ToLower().Contains("btcusdt")
                    && !x.Symbol.ToLower().Contains("bluebird")
                    && !x.Symbol.ToLower().Contains("btsusdt")
                    && !x.Symbol.ToLower().Contains("cocos")
                    && !x.Symbol.ToLower().Contains("ctk")
                    && !x.Symbol.ToLower().Contains("cvc")
                    && !x.Symbol.ToLower().Contains("antusdt")
                    && !x.Symbol.ToLower().Contains("btcdom")
                    && !x.Symbol.ToLower().Contains("dgbusdt")
                    ).ToList();

                foreach (var position in positionsToBeAnalyzed.DistinctBy(x => x.Symbol).OrderBy(x=>x.Symbol).Take(300))
                {
                    var data = await _dataHistoryRepository.GetDataByInterval(position.Symbol, _client, KlineInterval.OneMinute).ConfigureAwait(false);
                    var bollingerResult = GetBollingerValue(data);

                    if (bollingerResult != null && bollingerResult.MiddleBand < (double)position!.MarkPrice!.Value)
                    {
                        var close = positionsAvailableData.Data.FirstOrDefault(x => x.Symbol.ToLower().Equals(position!.Symbol.ToLower()) && x.Quantity < 0);
                        if (close != null)
                        {
                            await CreateOrdersLogic(close.Symbol, CommonOrderSide.Buy, close.Side, close.Quantity, close.MarkPrice).ConfigureAwait(false);
                        }

                        if (!positionsAvailableData.Data.Any(x => x.Symbol.ToLower().Equals(position!.Symbol.ToLower()) && x.Quantity > 0))
                        {
                            var response = await CreatePosition(new SymbolData
                            {
                                Mode = CommonOrderSide.Buy,
                                CurrentPrice = position!.MarkPrice!.Value,
                                Symbol = position.Symbol,
                            }, 6M, PositionSide.Long).ConfigureAwait(false);
                        }
                    }

                    if (bollingerResult != null && bollingerResult.MiddleBand > (double)position!.MarkPrice!.Value)
                    {
                        var close = positionsAvailableData.Data.FirstOrDefault(x => x.Symbol.ToLower().Equals(position!.Symbol.ToLower()) && x.Quantity > 0);
                        if (close != null)
                        {
                            await CreateOrdersLogic(close.Symbol, CommonOrderSide.Sell, close.Side, close.Quantity, close.MarkPrice).ConfigureAwait(false);
                        }

                        if (!positionsAvailableData.Data.Any(x => x.Symbol.ToLower().Equals(position!.Symbol.ToLower()) && x.Quantity < 0))
                        {
                            var response = await CreatePosition(new SymbolData
                            {
                                Mode = CommonOrderSide.Sell,
                                CurrentPrice = position!.MarkPrice!.Value,
                                Symbol = position.Symbol,
                            }, 6M, PositionSide.Short).ConfigureAwait(false);
                        }
                    }

                }

            }

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

        private static BollingerResult GetBollingerValue(DataHistory data)
        {
            data.ComputeSma();
            SmaResult dataAIndicator = (SmaResult)data.Indicators[Indicator.Sma];
            var bollingerResult = new BollingerResult
            {
                MiddleBand = dataAIndicator.Real[dataAIndicator.NBElement - 1],
            };
            return bollingerResult;
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
            await ClosePositionsProfit().ConfigureAwait(false);
            return null;
        }

        public async Task<Position?> ClosePositionsProfit()
        {

            var positionsAvailableData =
               await _client.CommonFuturesClient.GetPositionsAsync().ConfigureAwait(false);

            var positionsToBeAnalyzed = positionsAvailableData.Data
               .Where(x =>
                   x != null
                   && !x.Symbol.ToLower().Contains("usdc")
                   && x.Quantity != 0).ToList();
            {
                foreach (var position in positionsToBeAnalyzed.Where(x => x.UnrealizedPnl > 1M))
                {
                    if (position?.Quantity > 0)
                    {
                        await CreateOrdersLogic(position.Symbol, CommonOrderSide.Sell, position.Side, position.Quantity, position.MarkPrice).ConfigureAwait(false);
                    }
                    if (position?.Quantity < 0)
                    {
                        await CreateOrdersLogic(position.Symbol, CommonOrderSide.Buy, position.Side, position.Quantity, position.MarkPrice).ConfigureAwait(false);
                    }
                    Console.WriteLine(position.Symbol);
                }
            }
            return null;
        }


        public async Task<Position?> ClosePositionsLoss()
        {

            var positionsAvailableData =
               await _client.CommonFuturesClient.GetPositionsAsync().ConfigureAwait(false);

            var positionsToBeAnalyzed = positionsAvailableData.Data
               .Where(x =>
                   x != null
                   && !x.Symbol.ToLower().Contains("usdc")
                   && x.Quantity != 0).ToList();
            {
                foreach (var position in positionsToBeAnalyzed.Where(x => x.UnrealizedPnl < -1M))
                {
                    if (position?.Quantity > 0)
                    {
                        await CreateOrdersLogic(position.Symbol, CommonOrderSide.Sell, position.Side, position.Quantity * .5M, position.MarkPrice).ConfigureAwait(false);
                    }
                    if (position?.Quantity < 0)
                    {
                        await CreateOrdersLogic(position.Symbol, CommonOrderSide.Buy, position.Side, position.Quantity * .5M, position.MarkPrice).ConfigureAwait(false);

                    }
                    Console.WriteLine(position.Symbol);
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
            else
            {
                Console.WriteLine("Closed : " + symbol);
            }
        }
        #endregion
    }
}
