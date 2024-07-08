using Binance.Net.Interfaces.Clients.UsdFuturesApi;
using MakeMeRich.Binance.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Bonsai
{
    [Route("api/account")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        private readonly IBinanceRestClientUsdFuturesApi _client;

        public ValuesController(IDataHistoryRepository dataHistoryRepository)
        {
            _client = ClientDetails.GetClient();
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var account = await _client.Account.GetAccountInfoAsync().ConfigureAwait(false);
            //return Ok(account);
            return Ok("Balance :" + account?.Data?.TotalMarginBalance + "   Loss :" + account?.Data?.TotalUnrealizedProfit);
        }
    }
}
