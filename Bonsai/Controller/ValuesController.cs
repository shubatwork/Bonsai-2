using CryptoExchange.Net.Interfaces;
using Kucoin.Net.Clients;
using Kucoin.Net.Objects;
using Kucoin.Net.Objects.Models.Futures;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Bonsai.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        private static KucoinRestClient? restClient;

        public async Task<IActionResult> GetAsync()
        {
            var result = await ProcessAccounts();

            return Ok(result);
        }

        private static async Task<string> ProcessAccounts()
        {
            var credentials1 = GetApiCredentials("API_KEY_1", "API_SECRET_1", "API_PASSPHRASE_1");
            var credentials2 = GetApiCredentials("API_KEY_2", "API_SECRET_2", "API_PASSPHRASE_2");

            var accountInfo1 = await GetAccountOverviewAsync(credentials1);
            var positions1 = await GetPositionsAsync(credentials1);

            var accountInfo2 = await GetAccountOverviewAsync(credentials2);
            var positions2 = await GetPositionsAsync(credentials2);

            return ($"{Math.Round(accountInfo1.MarginBalance + accountInfo2.MarginBalance, 2)} - " +
                              $"{Math.Round(accountInfo1.UnrealizedPnl + accountInfo2.UnrealizedPnl, 2)} - " +
                              $"{Math.Round(accountInfo1.RiskRatio!.Value, 2)} - {positions1.Count()} - {positions1.Sum(x=> Math.Abs(x.PositionValue))} - " +
                              $"{Math.Round(accountInfo2.RiskRatio!.Value, 2)} - {positions2.Count()} - {positions2.Sum(x => Math.Abs(x.PositionValue))}");
        }

        private static KucoinApiCredentials GetApiCredentials(string apiKey, string apiSecret, string apiPassphrase)
        {
            if (apiKey == "API_KEY_1")
            {
                return new KucoinApiCredentials("6792c43bc0a1b1000135cb65", "25ab9c72-17e6-4951-b7a8-6e2fce9c3026", "test1234");
            }
            if (apiKey == "API_KEY_2")
            {
                return new KucoinApiCredentials("679b7a366425d800012aca8f", "99cd2f9a-b4ed-4fe3-8f6e-69d70e03eb51", "test1234");
            }
            return new KucoinApiCredentials("", "", "");
        }
        private static async Task<KucoinAccountOverview> GetAccountOverviewAsync(KucoinApiCredentials credentials)
        {
            restClient = new KucoinRestClient();
            restClient.SetApiCredentials(credentials);
            var accountInfo = await restClient.FuturesApi.Account.GetAccountOverviewAsync("USDT");
            return accountInfo.Data;
        }

        private static async Task<IEnumerable<KucoinPosition>> GetPositionsAsync(KucoinApiCredentials credentials)
        {
            restClient = new KucoinRestClient();
            restClient.SetApiCredentials(credentials);
            var positions = await restClient.FuturesApi.Account.GetPositionsAsync();
            return positions.Data;
        }


    }
}
