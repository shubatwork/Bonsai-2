using Binance.Net.Clients;
using Binance.Net.Interfaces.Clients.UsdFuturesApi;
using Binance.Net.Objects;
using Binance.Net.Objects.Options;
using CryptoExchange.Net.Authentication;

namespace Bonsai
{
    public static class ClientDetails
    {
        public static IBinanceRestClientUsdFuturesApi GetClient()
        {
            return new BinanceRestClient(options =>
            {
                options.ApiCredentials = new ApiCredentials("odCingfD91PswsUuN7nY1twne2ztzHwSlAt08ZpapxsgXIow9NJ7JnUOtI6ftlUK", "FBVc9ZhYhwjI10VzlcvJ4osH9dIjjUDxbCxDKHwttxMZer4ICeyPHR9sszfn459D");
            options.AutoTimestamp= true;
            }).UsdFuturesApi;
        }
    }
}
