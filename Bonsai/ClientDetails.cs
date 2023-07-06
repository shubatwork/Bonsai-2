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
        public static IBinanceClientUsdFuturesApi GetSonaClient()
        {
            return new BinanceClient(new BinanceClientOptions()
            {
                UsdFuturesApiOptions = new BinanceApiClientOptions()
                {
                    ApiCredentials = new BinanceApiCredentials("8j650Bo6TnSAUDAKklbLqTN3n8qJWGeQMpKuVeqwSDhWCQ8H4I5ivm5gw6t8y4Yn",
                        "M1xc7yscMah0WBQFJv6Snzu9pJFJaY2Vhkhm3x8k8NEReLHf0BPyJh855WSh2pQU"),
                    AutoTimestamp = true
                }
            }).UsdFuturesApi;

        }
    }
}
