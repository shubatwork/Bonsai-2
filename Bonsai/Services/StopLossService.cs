using Binance.Net.Enums;
using Binance.Net.Interfaces.Clients.UsdFuturesApi;
using TechnicalAnalysis.Business;
using TechnicalAnalysis;
using MakeMeRich.Binance.Services.Interfaces;

namespace Bonsai.Services
{
    public class StopLossService : IStopLossService
    {
        private readonly IBinanceClientUsdFuturesApi _client;
        private readonly IDataHistoryRepository _dataHistoryRepository;

        public StopLossService(IDataHistoryRepository dataHistoryRepository)
        {
            _client = ClientDetails.GetClient();
            _dataHistoryRepository = dataHistoryRepository;
        }

        public async Task CreateOrdersForTrailingStopLoss()
        {
            var positionsToBeClosed =
               await _client.CommonFuturesClient.GetPositionsAsync().ConfigureAwait(false);

            var positions = positionsToBeClosed.Data.Where(x => x.Quantity == 0);
            foreach (var position in positions)
            {
                await _client.Trading.CancelAllOrdersAsync(position.Symbol).ConfigureAwait(false);
            }

            foreach (var position in positionsToBeClosed.Data.Where(x => x.Quantity != 0))
            {
                var data = await _dataHistoryRepository.GetDataByInterval(position.Symbol, _client, KlineInterval.FiveMinutes).ConfigureAwait(false);
                var ema = GetEmaValue(data);
                var atr = GetAtrValue(data);
                switch (position.Quantity)
                {
                    case > 0:
                        {
                            var spCost = (decimal)ema - (decimal)atr;
                            var getOrderDetails =
                                await _client.Trading.GetOpenOrdersAsync(position.Symbol).ConfigureAwait(false);
                            var stopOrder = getOrderDetails.Data.FirstOrDefault(x => x.Type == FuturesOrderType.Stop);
                            if (stopOrder == null || stopOrder.Id == 0)
                            {
                                await CreateOrdersLogic(spCost, position.Symbol, position.Quantity, FuturesOrderType.Stop,
                                    OrderSide.Sell).ConfigureAwait(false);
                                break;
                            }

                            if (stopOrder.StopPrice < spCost)
                            {
                                await _client.Trading.CancelOrderAsync(position.Symbol, stopOrder.Id).ConfigureAwait(false);
                                await CreateOrdersLogic(spCost, position.Symbol, position.Quantity, FuturesOrderType.Stop,
                                    OrderSide.Sell).ConfigureAwait(false);
                            }

                            break;
                        }
                    case < 0:
                        {
                            var spCost = (decimal)ema + (decimal)atr;
                            var getOrderDetails =
                                await _client.Trading.GetOpenOrdersAsync(position.Symbol).ConfigureAwait(false);
                            var stopOrder = getOrderDetails.Data.FirstOrDefault(x => x.Type == FuturesOrderType.Stop);
                            if (stopOrder == null || stopOrder.Id == 0)
                            {
                                await CreateOrdersLogic(spCost, position.Symbol, position.Quantity, FuturesOrderType.Stop,
                                    OrderSide.Buy).ConfigureAwait(false);
                                break;
                            }

                            if (stopOrder.StopPrice > spCost)
                            {
                                await _client.Trading.CancelOrderAsync(position.Symbol, stopOrder.Id).ConfigureAwait(false);
                                await CreateOrdersLogic(spCost, position.Symbol, position.Quantity, FuturesOrderType.Stop,
                                    OrderSide.Buy).ConfigureAwait(false);
                            }
                            break;
                        }
                }
            }
        }

        private static double GetEmaValue(DataHistory data)
        {
            data.ComputeEma();
            EmaResult dataAIndicator = (EmaResult)data.Indicators[Indicator.Ema];
            var currentEma = dataAIndicator.Real[dataAIndicator.NBElement - 1];
            return currentEma;
        }

        private static double GetAtrValue(DataHistory data)
        {
            data.ComputeAtr();
            AtrResult dataAIndicator = (AtrResult)data.Indicators[Indicator.Atr];
            var currentEma = dataAIndicator.Real[dataAIndicator.NBElement - 1];
            return currentEma;
        }

        public async Task CancelOpenOrdersNotPresent()
        {
            var positionsAvailableData =
                await _client.CommonFuturesClient.GetPositionsAsync().ConfigureAwait(false);
            Console.WriteLine("Test" + positionsAvailableData.Error);
            var positions = positionsAvailableData.Data.Where(x => x.Quantity == 0);
            foreach (var position in positions)
            {
                await _client.Trading.CancelAllOrdersAsync(position.Symbol).ConfigureAwait(false);
            }
        }

        private async Task CreateOrdersLogic(decimal spCost, string symbol, decimal quantity, FuturesOrderType orderType, OrderSide orderSide)
        {
            quantity = Math.Abs(quantity);
            spCost = Math.Round(spCost, 6);
            var result = await _client.Trading.PlaceOrderAsync(symbol, orderSide
                , orderType, quantity, spCost, null, null, null, null, spCost);

            if (!result.Success)
            {
                spCost = Math.Round(spCost, 5);
                result = await _client.Trading.PlaceOrderAsync(symbol, orderSide,
                    orderType, quantity, spCost, null, null, null, null, spCost);
            }
            if (!result.Success)
            {
                spCost = Math.Round(spCost, 4);
                result = await _client.Trading.PlaceOrderAsync(symbol, orderSide,
                    orderType, quantity, spCost, null, null, null, null, spCost);
            }
            if (!result.Success)
            {
                spCost = Math.Round(spCost, 3);
                result = await _client.Trading.PlaceOrderAsync(symbol, orderSide,
                    orderType, quantity, spCost, null, null, null, null, spCost);
            }
            if (!result.Success)
            {
                spCost = Math.Round(spCost, 2);
                result = await _client.Trading.PlaceOrderAsync(symbol, orderSide,
                    orderType, quantity, spCost, null, null, null, null, spCost);
            }
            if (!result.Success)
            {
                spCost = Math.Round(spCost, 1);
                result = await _client.Trading.PlaceOrderAsync(symbol, orderSide,
                    orderType, quantity, spCost, null, null, null, null, spCost);
            }
            if (!result.Success)
            {
                spCost = Math.Round(spCost, 0);
                result = await _client.Trading.PlaceOrderAsync(symbol, orderSide,
                    orderType, quantity, spCost, null, null, null, null, spCost);
            }

            if (!result.Success)
            {
                Console.WriteLine(result.Error);
            }
        }
    }
}
