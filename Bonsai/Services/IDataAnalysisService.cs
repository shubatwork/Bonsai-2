using CryptoExchange.Net.CommonObjects;

namespace Bonsai.Services
{
    public interface IDataAnalysisService
    {
        Task<string?> CreatePositions(List<string> notToBeTakenPosition);
        Task<Position?> ClosePositions();
    }
}
