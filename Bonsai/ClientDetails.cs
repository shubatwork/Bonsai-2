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
                options.ApiCredentials = new ApiCredentials("SubdJEhrO7AiXKUFhtFUAiCqiHCToV6QLf7VrH0HxK2iKBNbzPsWUJaxJRjYyyE5", "5yKPjUJq96QJNZ9HalocSOclvPkUbPVsx1LEJFqD0hH6UGBlbsTVnJXWLviANSmt");
            options.AutoTimestamp= true;
            }).UsdFuturesApi;
        }
    }
}
