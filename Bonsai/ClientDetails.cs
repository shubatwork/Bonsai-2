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
                    ApiCredentials = new BinanceApiCredentials("CNtwmN60Mf9wzNAhZskCICEQBBRGKtbUXO37XbBien0tYAeqKZ79zFbLEqYJyBis",
                        "Esl5ineHyDSynPoJBeKgLcRujKw6xwi6kbaYes69TPczQwBQqyJRaozJCp68OSsq"),
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
