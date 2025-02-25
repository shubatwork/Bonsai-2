using CryptoExchange.Net.CommonObjects;

namespace Bonsai.Services
{
    public interface IDataAnalysisService
    {
        Task ClosePositions();
        Task CreatePositions();
    }
}
