using Binance.Net.Enums;
using Binance.Net.Interfaces.Clients.UsdFuturesApi;
using CryptoExchange.Net.CommonObjects;
using MakeMeRich.Binance.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Bonsai
{
    [Route("api/account")]
    [ApiController]
    public class ValuesController : Controller
    {
        private readonly IBinanceRestClientUsdFuturesApi _client;
        private readonly IDataHistoryRepository _dataHistoryRepository;

        public ValuesController(IDataHistoryRepository dataHistoryRepository)
        {
            _client = ClientDetails.GetClient();
            _dataHistoryRepository = dataHistoryRepository;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var account = await _client.Account.GetAccountInfoV2Async().ConfigureAwait(false);
            return View(account.Data);
        }

    }
}
