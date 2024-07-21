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
                options.ApiCredentials = new ApiCredentials("b00EypXpxX6n0QLjr92fF4WdwPwnXlxc2vcJh4s5vKgprZRgIFPuwJFALWR3nHHS", "fOp4FfrZrOU7kG3G046oYivazhKzJYsrXA7ymZaV68aOAIFolsMs9xGpvbrKfT7x");
            options.AutoTimestamp= true;
            }).UsdFuturesApi;
        }
    }
}
