using Binance.Net.Enums;
using Binance.Net.Interfaces.Clients.UsdFuturesApi;
using Bonsai;
using CryptoExchange.Net.CommonObjects;

namespace Bonsai.Services
{
    public class ProfitService : IProfitService
    {
        private readonly IBinanceRestClientUsdFuturesApi _client;

        public ProfitService()
        {
            _client = ClientDetails.GetClient();
        }
        public async Task ClosePositionsForProfit()
        {
            var positionsAvailableData =
                await _client.CommonFuturesClient.GetPositionsAsync().ConfigureAwait(false);
            var profit = positionsAvailableData.Data.Where(x => x.Quantity != 0).Sum(x => x.UnrealizedPnl);
            if (profit > 1M)
            {
                var positionToClose = positionsAvailableData.Data.Where(x => x.Quantity != 0).MaxBy(x => x.UnrealizedPnl);

                switch (positionToClose?.Quantity)
                {
                    case > 0:
                        await CreateOrdersLogic(positionToClose.Symbol, CommonOrderSide.Sell, positionToClose.Quantity, true);
                        break;

                    case < 0:
                        await CreateOrdersLogic(positionToClose.Symbol, CommonOrderSide.Buy, positionToClose.Quantity, true);
                        break;
                }
            }
            if (profit < -1M)
            {
                var positionToClose = positionsAvailableData.Data.Where(x => x.Quantity != 0).MinBy(x => x.UnrealizedPnl);

                switch (positionToClose?.Quantity)
                {
                    case > 0:
                        await CreateOrdersLogic(positionToClose.Symbol, CommonOrderSide.Sell, positionToClose.Quantity, true);
                        break;

                    case < 0:
                        await CreateOrdersLogic(positionToClose.Symbol, CommonOrderSide.Buy, positionToClose.Quantity, true);
                        break;
                }
            }
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

            //if (result.Success && isSlRequired)
            //{
            //    await _stopLossService.CreateStopLossOrdersForEntryPrice(symbol).ConfigureAwait(false);
            //}

            //if (result.Success && isGreaterThanMaxProfit)
            //{
            //    await _stopLossService.CreateStopLossOrdersForEntryPrice(symbol).ConfigureAwait(false);
            //}
        }

    }
}
