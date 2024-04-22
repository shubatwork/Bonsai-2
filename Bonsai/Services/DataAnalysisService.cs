using Binance.Net.Interfaces.Clients.UsdFuturesApi;
using MakeMeRich.Binance.Services.Interfaces;
using TechnicalAnalysis.Business;
using TechnicalAnalysis;
using CryptoExchange.Net.CommonObjects;
using Binance.Net.Enums;
using System.Reflection.Metadata;
using System.Linq;

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


        public async Task<string?> CreatePositionsBuy(List<Position?> notToBeCreated)
        {
            
            var positionsAvailableData =
               await _client.CommonFuturesClient.GetPositionsAsync().ConfigureAwait(false);
            var bal = await _client.Account.GetAccountInfoAsync().ConfigureAwait(false);

            Console.Write(bal.Data.TotalMarginBalance + "\n");

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
                    && !x.Symbol.ToLower().Contains("dgbusdt")
                    ).ToList();


                if (notToBeCreated.Any())
                {
                    positionsToBeAnalyzed.RemoveAll(position => notToBeCreated.Any(notAllowed => notAllowed != null && notAllowed!.Symbol == position.Symbol));
                }

                foreach (var position in positionsToBeAnalyzed.DistinctBy(x => x.Symbol).OrderBy(x => x.Symbol).Take(300))
                {
                    var data = await _dataHistoryRepository.GetDataByInterval(position.Symbol, _client, KlineInterval.ThreeMinutes).ConfigureAwait(false);
                    Dictionary<string, double[]> bollingerBands = data.CalculateBollingerBands(21, 2);
                    double[] middleBand = bollingerBands["MiddleBand"];
                    double[] upperBand = bollingerBands["UpperBand"];
                    double[] lowerBand = bollingerBands["LowerBand"];

                    var bollingerResult = new BollingerResult
                    {
                        UpperBand = upperBand.Last(),
                        LowerBand = lowerBand.Last(),
                        MiddleBand = middleBand.Last(),
                    };


                    if (bollingerResult != null && bollingerResult.UpperBand < (double)position!.MarkPrice!.Value)
                    {
                        if (!positionsAvailableData.Data.Any(x => x.Symbol.ToLower().Equals(position!.Symbol.ToLower()) && x.Quantity > 0))
                        {
                            var response = await CreatePosition(new SymbolData
                            {
                                Mode = CommonOrderSide.Buy,
                                CurrentPrice = position!.MarkPrice!.Value,
                                Symbol = position.Symbol,
                            }, 6M, PositionSide.Long).ConfigureAwait(false);
                            if(response)
                            {
                                Console.WriteLine(position.Symbol);
                                break;
                            }
                           
                        }
                    }

                    if (bollingerResult != null && bollingerResult.LowerBand > (double)position!.MarkPrice!.Value)
                    {
                        if (!positionsAvailableData.Data.Any(x => x.Symbol.ToLower().Equals(position!.Symbol.ToLower()) && x.Quantity < 0))
                        {
                            var response = await CreatePosition(new SymbolData
                            {
                                Mode = CommonOrderSide.Sell,
                                CurrentPrice = position!.MarkPrice!.Value,
                                Symbol = position.Symbol,
                            }, 6M, PositionSide.Short).ConfigureAwait(false);
                            if (response)
                            {
                                Console.WriteLine(position.Symbol);
                                break;
                            }
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

            return result.Success;
        }

        #region Close Position
        public async Task<Position?> ClosePositions()
        {
            var x = await ClosePositionsProfit().ConfigureAwait(false);
            return x;
        }

        public async Task<Position?> ClosePositionsProfit()
        {
            var positionsAvailableData =
               await _client.CommonFuturesClient.GetPositionsAsync().ConfigureAwait(false);

            if(positionsAvailableData.Success) 
            {
                await ClosePositionsLoss().ConfigureAwait(false);
                var positionsToBeAnalyzed = positionsAvailableData.Data
               .Where(x =>
                   x != null
                   && !x.Symbol.ToLower().Contains("usdc")
                   && x.Quantity != 0).ToList();
                {
                    foreach (var position in positionsToBeAnalyzed.Where(x => x.UnrealizedPnl > 2M))
                    {
                        if (position?.Quantity > 0)
                        {
                            await CreateOrdersLogic(position.Symbol, CommonOrderSide.Sell, position.Side, position.Quantity, position.MarkPrice).ConfigureAwait(false);
                            return position;
                        }
                        if (position?.Quantity < 0)
                        {
                            await CreateOrdersLogic(position.Symbol, CommonOrderSide.Buy, position.Side, position.Quantity, position.MarkPrice).ConfigureAwait(false);
                            return position;
                        }
                    }
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
                foreach (var position in positionsToBeAnalyzed.Where(x => x.UnrealizedPnl < -2M))
                {
                    if (position?.Quantity > 0)
                    {
                        await CreateOrdersLogic(position.Symbol, CommonOrderSide.Sell, position.Side, position.Quantity * .25M, position.MarkPrice).ConfigureAwait(false);
                    }
                    if (position?.Quantity < 0)
                    {
                        await CreateOrdersLogic(position.Symbol, CommonOrderSide.Buy, position.Side, position.Quantity * .25M, position.MarkPrice).ConfigureAwait(false);

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
