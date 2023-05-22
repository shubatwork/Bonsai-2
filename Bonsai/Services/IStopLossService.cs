using CryptoExchange.Net.CommonObjects;

namespace Bonsai.Services
{
    public interface IStopLossService
    {
        Task CreateOrdersForTrailingStopLoss();
    }
}