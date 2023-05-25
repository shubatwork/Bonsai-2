using Binance.Net.Clients;
using Binance.Net.Interfaces.Clients.UsdFuturesApi;
using Binance.Net.Objects;
using CryptoExchange.Net.Authentication;

namespace Bonsai
{
    public static class ClientDetails
    {
        public static IBinanceClientUsdFuturesApi GetClient()
        {
            return new BinanceClient(new BinanceClientOptions()
            {
                UsdFuturesApiOptions = new BinanceApiClientOptions()
                {
                    ApiCredentials = new BinanceApiCredentials("W6EQa8qhq6RyEdPYKyrwxs8TDuwqpcQfN5kBMRy18dwMBdc3cGDQ0YKf9jStyYr8",
                        "hkm0Pih3Kppj8gZF4jvl44y118UZOMPM55hII7g49T5dapit8ZN63KV5C3Ik7bMR"),
                    AutoTimestamp = true
                }
            }).UsdFuturesApi;

        }
    }
}
