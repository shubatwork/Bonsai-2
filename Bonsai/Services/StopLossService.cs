using Binance.Net.Enums;
using Binance.Net.Interfaces.Clients.UsdFuturesApi;

namespace Bonsai.Services
{
    public class StopLossService : IStopLossService
    {
        private readonly IBinanceClientUsdFuturesApi _client;

        public StopLossService()
        {
            _client = ClientDetails.GetClient();
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
                var slValue = 0.5M;
                if(position.UnrealizedPnl > 0.25M)
                {
                    slValue = 0.25M;
                }
                switch (position.Quantity)
                {
                    case > 0:
                        {
                            var spCost = (position.Quantity * position.MarkPrice - slValue) / position.Quantity;
                            var getOrderDetails =
                                await _client.Trading.GetOpenOrdersAsync(position.Symbol).ConfigureAwait(false);
                            var stopOrder = getOrderDetails.Data.FirstOrDefault(x => x.Type == FuturesOrderType.Stop);
                            if (stopOrder == null || stopOrder.Id == 0)
                            {
                                await CreateOrdersLogic(spCost!.Value, position.Symbol, position.Quantity, FuturesOrderType.Stop,
                                    OrderSide.Sell).ConfigureAwait(false);
                                break;
                            }

                            if (stopOrder.StopPrice < spCost)
                            {
                                await _client.Trading.CancelOrderAsync(position.Symbol, stopOrder.Id).ConfigureAwait(false);
                                await CreateOrdersLogic(spCost.Value, position.Symbol, position.Quantity, FuturesOrderType.Stop,
                                    OrderSide.Sell).ConfigureAwait(false);
                            }

                            break;
                        }
                    case < 0:
                        {
                            var spCost = (position.Quantity * position.MarkPrice - slValue) / position.Quantity;
                            var getOrderDetails =
                                await _client.Trading.GetOpenOrdersAsync(position.Symbol).ConfigureAwait(false);
                            var stopOrder = getOrderDetails.Data.FirstOrDefault(x => x.Type == FuturesOrderType.Stop);
                            if (stopOrder == null || stopOrder.Id == 0)
                            {
                                await CreateOrdersLogic(spCost!.Value, position.Symbol, position.Quantity, FuturesOrderType.Stop,
                                    OrderSide.Buy).ConfigureAwait(false);
                                break;
                            }

                            if (stopOrder.StopPrice > spCost)
                            {
                                await _client.Trading.CancelOrderAsync(position.Symbol, stopOrder.Id).ConfigureAwait(false);
                                await CreateOrdersLogic(spCost.Value, position.Symbol, position.Quantity, FuturesOrderType.Stop,
                                    OrderSide.Buy).ConfigureAwait(false);
                            }
                            break;
                        }
                }
            }
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
