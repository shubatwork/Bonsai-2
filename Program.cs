using KiteConnect;

namespace Bonsai
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var kite = AuthClient.GetClient();
            while (true)
            {
                var positions = kite.GetPositions();
                var orders = kite.GetOrders();
                foreach (var position in positions.Net.Where(x => x.Quantity > 0))
                {
                    var currentOrder = orders.FirstOrDefault(x => x.InstrumentToken == position.InstrumentToken && (x.Status == "OPEN" || x.Status == "TRIGGER PENDING"));
                    if (string.IsNullOrEmpty(currentOrder.OrderId))
                    {
                        var ltp = kite.GetLTP(new[] { $"{position.Exchange}:{position.TradingSymbol}" }).FirstOrDefault().Value;
                        var slPrice = ltp.LastPrice - 50;
                        if (slPrice < 10)
                        {
                            continue;
                        }
                        var triggerPrice = slPrice + 1;
                        kite.PlaceOrder(
                            position.Exchange,
                            position.TradingSymbol,
                            Constants.TRANSACTION_TYPE_SELL,
                            position.Quantity,
                            slPrice,
                            position.Product,
                            Constants.ORDER_TYPE_SL,
                            Constants.VALIDITY_DAY,
                            null,
                            triggerPrice,
                            null,
                            slPrice,
                            null
                            );
                    }

                    if (!string.IsNullOrEmpty(currentOrder.OrderId))
                    {
                        var ltp = kite.GetLTP(new[] { $"{position.Exchange}:{position.TradingSymbol}" }).FirstOrDefault().Value;
                        if (position.BuyPrice + 25 < ltp.LastPrice && currentOrder.Price < ltp.LastPrice - 50)
                        {
                            kite.CancelOrder(currentOrder.OrderId);
                            var slPrice = ltp.LastPrice - 50;
                            if (slPrice < 10)
                            {
                                continue;
                            }
                            var triggerPrice = slPrice + 1;
                            kite.PlaceOrder(
                                position.Exchange,
                                position.TradingSymbol,
                                Constants.TRANSACTION_TYPE_SELL,
                                position.Quantity,
                                slPrice,
                                position.Product,
                                Constants.ORDER_TYPE_SL,
                                Constants.VALIDITY_DAY,
                                null,
                                triggerPrice,
                                null,
                                slPrice
                            );
                        }

                        else if (currentOrder.Price < ltp.LastPrice - 50)
                        {
                            kite.CancelOrder(currentOrder.OrderId);
                            var slPrice = ltp.LastPrice - 50;
                            if (slPrice < 10)
                            {
                                continue;
                            }
                            var triggerPrice = slPrice + 1;
                            kite.PlaceOrder(
                                position.Exchange,
                                position.TradingSymbol,
                                Constants.TRANSACTION_TYPE_SELL,
                                position.Quantity,
                                slPrice,
                                position.Product,
                                Constants.ORDER_TYPE_SL,
                                Constants.VALIDITY_DAY,
                                null,
                                triggerPrice,
                                null,
                                slPrice
                                );
                        }
                    }
                }
                Thread.Sleep(60000);
            }
        }
    }
}