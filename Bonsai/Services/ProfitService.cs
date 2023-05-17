using Binance.Net.Enums;
using Binance.Net.Interfaces.Clients.UsdFuturesApi;
using Bonsai;
using CryptoExchange.Net.CommonObjects;
using MakeMeRich.Binance.Services.Interfaces;

namespace MakeMeRich.Binance.Services
{
    public class ProfitService : IProfitService
    {
        private readonly IBinanceClientUsdFuturesApi _client;
        private readonly decimal maxProfit = .1M;

        public ProfitService()
        {
            _client = ClientDetails.GetClient();
        }
        public async Task ClosePositionsForProfit()
        {
            var positionsAvailableData =
                await _client.CommonFuturesClient.GetPositionsAsync().ConfigureAwait(false);
            foreach (var position in positionsAvailableData.Data.Where(x => x.Quantity != 0 && x.UnrealizedPnl > maxProfit).ToList())
            {
                switch (position.Quantity)
                {
                    case > 0:
                        await CreateOrdersLogic(position.Symbol, CommonOrderSide.Sell, position.Quantity, true);
                        break;

                    case < 0:
                        await CreateOrdersLogic(position.Symbol, CommonOrderSide.Buy, position.Quantity, true);
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
