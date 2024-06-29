using Binance.Net.Interfaces.Clients.UsdFuturesApi;
using MakeMeRich.Binance.Services.Interfaces;
using TechnicalAnalysis.Business;
using TechnicalAnalysis;
using CryptoExchange.Net.CommonObjects;
using Binance.Net.Enums;
using System.Reflection.Metadata;
using System.Linq;
using CryptoExchange.Net.Interfaces;
using Twilio;
using Twilio.Types;
using Twilio.Rest.Api.V2010.Account;
using Microsoft.VisualBasic;

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

            if (positionsAvailableData.Success)
            {
                try
                {
                    bool canBuy = true;
                    bool canSell = false;

                    var account = await _client.Account.GetAccountInfoAsync().ConfigureAwait(false);

                    if (false)
                    {
                        const string accountSid = "ACa927509ad350bb6e37c6aa3dbb4bfc0f";
                        const string authToken = "2b43b16fbdb3b951d564d68e3d8a664d";
                        TwilioClient.Init(accountSid, authToken);
                        var from = new PhoneNumber("whatsapp:+14155238886");
                        var to = new PhoneNumber("whatsapp:+918754569602");
                        var message = await MessageResource.CreateAsync(
                            from: from,
                            to: to,
                            body: (account?.Data?.TotalMarginBalance * 80).ToString()).ConfigureAwait(false);
                    }

                    if (!account.Success || account.Data.TotalMaintMargin / account.Data.TotalMarginBalance > .2M)
                    {

                        Console.WriteLine("Teri Aukat :" + account?.Data?.TotalMarginBalance * 80);
                        return null;
                    }

                    //await IncreasePositions();


                    var data1 = await _client.ExchangeData.GetTickersAsync().ConfigureAwait(false);
                    foreach (var position in data1.Data.Where(x => !x.Symbol.ToLower().Contains("usdc")
                    && !x.Symbol.ToLower().Contains("btc")
                    && !x.Symbol.ToLower().Contains("crv")
                    && !x.Symbol.ToLower().Contains("tia"))
                        .Where(x=>x.PriceChangePercent > 0)
                        .DistinctBy(x => x.Symbol)
                        .OrderByDescending(x => x.PriceChangePercent).Take(10))
                    {

                        if (!positionsAvailableData.Data.Any(x => x.Symbol.ToLower().Equals(position!.Symbol.ToLower()) && x.Quantity != 0) && canBuy)
                        {
                            var markPrice = await _client.ExchangeData.GetMarkPriceAsync(position.Symbol).ConfigureAwait(false);
                            var minData = await _client.ExchangeData.GetKlinesAsync(position.Symbol, KlineInterval.OneMinute, null, null, 99);
                            var ma100 = (minData.Data.Sum(x => x.ClosePrice) / 99);
                            if (markPrice.Data.MarkPrice > ma100)
                            {

                                var response = await CreatePosition(new SymbolData
                                {
                                    Mode = CommonOrderSide.Buy,
                                    CurrentPrice = markPrice.Data.MarkPrice,
                                    Symbol = position.Symbol,
                                }, 10M, PositionSide.Long).ConfigureAwait(false);
                                if (response)
                                {
                                    Console.WriteLine(position.Symbol + "  Long");
                                    break;
                                }
                            }
                        }
                    }

                    foreach (var position in data1.Data.Where(x => !x.Symbol.ToLower().Contains("usdc")
                    && !x.Symbol.ToLower().Contains("btc")
                    && !x.Symbol.ToLower().Contains("crv")
                    && !x.Symbol.ToLower().Contains("tia"))
                        .Where(x => x.PriceChangePercent < 0)
                        .DistinctBy(x => x.Symbol)
                        .OrderByDescending(x => x.PriceChangePercent).Take(10))
                    {

                        if (!positionsAvailableData.Data.Any(x => x.Symbol.ToLower().Equals(position!.Symbol.ToLower()) && x.Quantity != 0) && canSell)
                        {
                            var markPrice = await _client.ExchangeData.GetMarkPriceAsync(position.Symbol).ConfigureAwait(false);
                            var minData = await _client.ExchangeData.GetKlinesAsync(position.Symbol, KlineInterval.OneMinute, null, null, 99);
                            var ma100 = (minData.Data.Sum(x => x.ClosePrice) / 99);
                            if (markPrice.Data.MarkPrice < ma100 && canSell)
                            {

                                var response = await CreatePosition(new SymbolData
                                {
                                    Mode = CommonOrderSide.Sell,
                                    CurrentPrice = markPrice.Data.MarkPrice,
                                    Symbol = position.Symbol,
                                }, 10M, PositionSide.Short).ConfigureAwait(false);
                                if (response)
                                {
                                    Console.WriteLine(position.Symbol + "  Short");
                                    break;
                                }
                            }
                        }
                    }

                    return null;
                }
                catch (Exception)
                {
                    return null;
                }
            }

            return null;
        }
        public async Task<bool> IncreasePositions()
        {
            var positionsAvailableData =
               await _client.CommonFuturesClient.GetPositionsAsync().ConfigureAwait(false);

            var positionsToBeAnalyzed = positionsAvailableData.Data
                .Where(x => x != null && x.Quantity != 0 && Math.Abs(x.Quantity * x.EntryPrice!.Value) < 4m);

            foreach (var positionByAdx in positionsToBeAnalyzed.OrderByDescending(x => x.UnrealizedPnl))
            {
                if (positionByAdx!.Quantity > 0)
                {
                    var markPrice = await _client.ExchangeData.GetMarkPriceAsync(positionByAdx.Symbol).ConfigureAwait(false);
                    var minData = await _client.ExchangeData.GetKlinesAsync(positionByAdx.Symbol, KlineInterval.FifteenMinutes, null, null, 4);
                    var ma100 = (minData.Data.Sum(x => x.ClosePrice) / 4);
                    if (markPrice.Data.MarkPrice > ma100)
                    {
                        var response = await CreatePosition(new SymbolData
                        {
                            Mode = CommonOrderSide.Buy,
                            CurrentPrice = positionByAdx!.MarkPrice!.Value,
                            Symbol = positionByAdx!.Symbol,
                        }, 6M, PositionSide.Long).ConfigureAwait(false);
                        return true;
                    }
                }
                if (positionByAdx!.Quantity < 0)
                {
                    var markPrice = await _client.ExchangeData.GetMarkPriceAsync(positionByAdx.Symbol).ConfigureAwait(false);
                    var minData = await _client.ExchangeData.GetKlinesAsync(positionByAdx.Symbol, KlineInterval.FifteenMinutes, null, null, 4);
                    var ma100 = (minData.Data.Sum(x => x.ClosePrice) / 4);
                    if (markPrice.Data.MarkPrice < ma100)
                    {
                        var response = await CreatePosition(new SymbolData
                        {
                            Mode = CommonOrderSide.Sell,
                            CurrentPrice = positionByAdx!.MarkPrice!.Value,
                            Symbol = positionByAdx!.Symbol,
                        }, 6M, PositionSide.Short).ConfigureAwait(false);
                        return true;
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

            var account = await _client.Account.GetAccountInfoAsync().ConfigureAwait(false);

            if (positionsAvailableData.Success)
            {
                var positionsToBeAnalyzed = positionsAvailableData.Data
               .Where(x =>
                   x != null
                   && x.Quantity != 0).ToList();
                {
                    foreach (var position in positionsToBeAnalyzed.Where(x => x.UnrealizedPnl > .1M))
                    {
                        if (position.Quantity > 0)
                        { 
                            var res = await CreateOrdersLogic(position.Symbol, CommonOrderSide.Sell, position.Side, position.Quantity, position.MarkPrice).ConfigureAwait(false);
                            if (!res)
                            {
                                await CreateOrdersLogic(position.Symbol, CommonOrderSide.Sell, position.Side, position.Quantity , position.MarkPrice).ConfigureAwait(false);
                             }
                        }
                        if (position.Quantity < 0)
                        {
                            var res = await CreateOrdersLogic(position.Symbol, CommonOrderSide.Buy, position.Side, position.Quantity, position.MarkPrice).ConfigureAwait(false);
                            if (!res)
                            {
                                await CreateOrdersLogic(position.Symbol, CommonOrderSide.Buy, position.Side, position.Quantity, position.MarkPrice).ConfigureAwait(false);
                                }

                        }
                    }
                    foreach (var position in positionsToBeAnalyzed.Where(x => x.UnrealizedPnl < -10M))
                    {
                        if (position.Quantity > 0)
                        {
                            var res = await CreateOrdersLogic(position.Symbol, CommonOrderSide.Sell, position.Side, position.Quantity, position.MarkPrice).ConfigureAwait(false);
                            if (!res)
                            {
                                await CreateOrdersLogic(position.Symbol, CommonOrderSide.Sell, position.Side, position.Quantity, position.MarkPrice).ConfigureAwait(false);
                            }
                        }
                        if (position.Quantity < 0)
                        {
                            var res = await CreateOrdersLogic(position.Symbol, CommonOrderSide.Buy, position.Side, position.Quantity, position.MarkPrice).ConfigureAwait(false);
                            if (!res)
                            {
                                await CreateOrdersLogic(position.Symbol, CommonOrderSide.Buy, position.Side, position.Quantity, position.MarkPrice).ConfigureAwait(false);
                            }

                        }
                    }
                }
            }

            return null;
        }

        public async Task<bool> CreateOrdersLogic(string symbol, CommonOrderSide orderSide, CommonPositionSide? positionSide, decimal quantity, decimal? markPrice)
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
                Console.WriteLine(result.Error + symbol);
            }
            else
            {
                Console.WriteLine("Closed : " + symbol);
            }

            return result.Success;
        }


        #endregion
    }
}
