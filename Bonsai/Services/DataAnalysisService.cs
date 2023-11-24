﻿using Binance.Net.Interfaces.Clients.UsdFuturesApi;
using MakeMeRich.Binance.Services.Interfaces;
using TechnicalAnalysis.Business;
using TechnicalAnalysis;
using CryptoExchange.Net.CommonObjects;
using Binance.Net.Enums;
using System.Threading.Tasks;

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

        public async Task<string?> CreatePositions()
        {
            await IncreasePositionsForProfit().ConfigureAwait(false);
            await IncreasePositionsForLoss().ConfigureAwait(false);

            var positionsAvailableData =
               await _client.CommonFuturesClient.GetPositionsAsync().ConfigureAwait(false);

            if (positionsAvailableData.Data.Count(x => x.Quantity != 0) > 9)
            {
                return null;
            }

            var shortSide = positionsAvailableData.Data.Count(x => x.Quantity < 0);
            var longSide = positionsAvailableData.Data.Count(x => x.Quantity > 0);

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

            var btcCandleData = await _client.ExchangeData.GetKlinesAsync("btcusdt", KlineInterval.OneHour, null, null, 1).ConfigureAwait(false);
            var btcCandle = btcCandleData.Data.FirstOrDefault();

            var hourlyResultList = new List<DailyResult>();
            foreach (var pos in positionsToBeAnalyzed)
            {
                var data = await _dataHistoryRepository.GetDataByInterval(pos.Symbol, _client, KlineInterval.OneMinute).ConfigureAwait(false);
                var hourlyCandleData = await _client.ExchangeData.GetKlinesAsync(pos.Symbol, KlineInterval.OneHour, null, null, 1).ConfigureAwait(false);
                var hourlyCandle = hourlyCandleData.Data.FirstOrDefault();
                if (data.Count > 31)
                {
                    var x = new DailyResult
                    {
                        RsiValue = GetRsiValue(data),
                        Position = pos,
                        CommonOrderSide = hourlyCandle?.OpenPrice < pos.MarkPrice ? CommonOrderSide.Buy : CommonOrderSide.Sell
                    };

                    hourlyResultList.Add(x);
                }
            }

            foreach (var positionByAdx in hourlyResultList.OrderBy(x => x.RsiValue))
            {
                if (positionByAdx?.RsiValue > 50 && positionByAdx?.RsiValue < 70 
                    && longSide <= shortSide
                    && positionByAdx.CommonOrderSide == CommonOrderSide.Buy
                    && btcCandle?.OpenPrice < btcCandle?.ClosePrice)
                {
                    var pos = positionByAdx?.Position;
                    var response = await CreatePosition(new SymbolData
                    {
                        Mode = CommonOrderSide.Buy,
                        CurrentPrice = pos!.MarkPrice!.Value,
                        Symbol = pos!.Symbol,
                    }, 6M).ConfigureAwait(false);
                    if (response)
                    {
                        return null;
                    }

                }
                else if (positionByAdx?.RsiValue < 20 && longSide <= shortSide && positionByAdx.CommonOrderSide == CommonOrderSide.Buy && btcCandle?.OpenPrice < btcCandle?.ClosePrice)
                {
                    var pos = positionByAdx?.Position;
                    var response = await CreatePosition(new SymbolData
                    {
                        Mode = CommonOrderSide.Buy,
                        CurrentPrice = pos!.MarkPrice!.Value,
                        Symbol = pos!.Symbol,
                    }, 6M).ConfigureAwait(false);
                    if (response)
                    {
                        return null;
                    }
                }
                else if (positionByAdx?.RsiValue > 80 && longSide >= shortSide && positionByAdx.CommonOrderSide == CommonOrderSide.Sell && btcCandle?.OpenPrice > btcCandle?.ClosePrice)
                {
                    var pos = positionByAdx?.Position;
                    var response = await CreatePosition(new SymbolData
                    {
                        Mode = CommonOrderSide.Sell,
                        CurrentPrice = pos!.MarkPrice!.Value,
                        Symbol = pos!.Symbol,
                    }, 6M).ConfigureAwait(false);
                    if (response)
                    {
                        return null;
                    }
                }
                else if (positionByAdx?.RsiValue < 50 && positionByAdx?.RsiValue > 30 && longSide >= shortSide && positionByAdx.CommonOrderSide == CommonOrderSide.Sell && btcCandle?.OpenPrice > btcCandle?.ClosePrice)
                {
                    var pos = positionByAdx?.Position;
                    var response = await CreatePosition(new SymbolData
                    {
                        Mode = CommonOrderSide.Sell,
                        CurrentPrice = pos!.MarkPrice!.Value,
                        Symbol = pos!.Symbol,
                    }, 6M).ConfigureAwait(false);
                    if (response)
                    {
                        return null;
                    }
                }
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

        public async Task<string?> IncreasePositionsForLoss()
        {
            var positionsAvailableData =
               await _client.CommonFuturesClient.GetPositionsAsync().ConfigureAwait(false);

            var positionsToBeAnalyzed = positionsAvailableData.Data
               .Where(x => x != null && x.Quantity != 0).ToList();

            var pos = positionsToBeAnalyzed.Where(x => x.UnrealizedPnl < -Math.Abs(x.Quantity * x.EntryPrice!.Value * 0.02M)).OrderBy(x => Math.Abs(x.Quantity * x.EntryPrice!.Value)).FirstOrDefault();

            if (pos?.Quantity > 0)
            {
                var response = await CreatePosition(new SymbolData
                {
                    Mode = CommonOrderSide.Buy,
                    CurrentPrice = pos!.MarkPrice!.Value,
                    Symbol = pos!.Symbol,
                }, 6M).ConfigureAwait(false);
                if (response)
                {
                    return null;
                }
            }
            else if (pos?.Quantity < 0)
            {
                var response = await CreatePosition(new SymbolData
                {
                    Mode = CommonOrderSide.Sell,
                    CurrentPrice = pos!.MarkPrice!.Value,
                    Symbol = pos!.Symbol,
                }, 6M).ConfigureAwait(false);
                if (response)
                {
                    return null;
                }
            }
            return null;
        }

        public async Task<string?> IncreasePositionsForProfit()
        {
            var positionsAvailableData =
               await _client.CommonFuturesClient.GetPositionsAsync().ConfigureAwait(false);

            var positionsToBeAnalyzed = positionsAvailableData.Data
               .Where(x => x != null && x.Quantity != 0).ToList();

            var pos = positionsToBeAnalyzed.Where(x => x.UnrealizedPnl > Math.Abs(x.Quantity * x.EntryPrice!.Value * 0.01M)).OrderBy(x => Math.Abs(x.Quantity * x.EntryPrice!.Value)).FirstOrDefault();

            if (pos?.Quantity > 0)
            {
                var response = await CreatePosition(new SymbolData
                {
                    Mode = CommonOrderSide.Buy,
                    CurrentPrice = pos!.MarkPrice!.Value,
                    Symbol = pos!.Symbol,
                }, 6M).ConfigureAwait(false);
                if (response)
                {
                    return null;
                }
            }
            else if (pos?.Quantity < 0)
            {
                var response = await CreatePosition(new SymbolData
                {
                    Mode = CommonOrderSide.Sell,
                    CurrentPrice = pos!.MarkPrice!.Value,
                    Symbol = pos!.Symbol,
                }, 6M).ConfigureAwait(false);
                if (response)
                {
                    return null;
                }
            }
            return null;
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

            foreach (var position in positionsToBeAnalyzed)
            {
                if (position!.Quantity > 0)
                {
                    if (position!.UnrealizedPnl > .015M)
                    {
                        await CreateOrdersLogic(position!.Symbol, CommonOrderSide.Sell, position!.Quantity, true).ConfigureAwait(false);
                    }

                }
                if (position!.Quantity < 0)
                {
                    if (position!.UnrealizedPnl > .015M)
                    {
                        await CreateOrdersLogic(position!.Symbol, CommonOrderSide.Buy, position!.Quantity, true).ConfigureAwait(false);
                    }

                }
            }

            var loss = positionsToBeAnalyzed.Sum(x => x.UnrealizedPnl);
            if (loss < -1M)
            {
                foreach (var position in positionsToBeAnalyzed)
                {
                    if (position!.Quantity > 0)
                    {
                        await CreateOrdersLogic(position!.Symbol, CommonOrderSide.Sell, position!.Quantity, true).ConfigureAwait(false);
                    }
                    if (position!.Quantity < 0)
                    {
                        await CreateOrdersLogic(position!.Symbol, CommonOrderSide.Buy, position!.Quantity, true).ConfigureAwait(false);

                    }
                }
            }

            return null;
        }

        public async Task<Position?> CloseOldPositions()
        {
            var positionsAvailableData =
               await _client.CommonFuturesClient.GetPositionsAsync().ConfigureAwait(false);

            var positionsToBeAnalyzed = positionsAvailableData.Data
               .Where(x =>
                   x != null
                   && !x.Symbol.ToLower().Contains("usdc")
                   && x.Quantity != 0).ToList();

            var position1 = positionsAvailableData.Data.Where(x => x.UnrealizedPnl < -0.1M).MinBy(x => x.UnrealizedPnl);
            {
                switch (position1?.Quantity)
                {
                    case > 0:
                        await CreateOrdersLogic(position1.Symbol, CommonOrderSide.Sell, position1.Quantity, true).ConfigureAwait(false);
                        break;
                    case < 0:
                        await CreateOrdersLogic(position1.Symbol, CommonOrderSide.Buy, position1.Quantity, true).ConfigureAwait(false);
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
    }
}
