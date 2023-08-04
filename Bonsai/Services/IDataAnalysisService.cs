using CryptoExchange.Net.CommonObjects;

namespace Bonsai.Services
{
    public interface IDataAnalysisService
    {
        Task<Position?> ClosePositions();
        Task<Position?> CloseSonaPositions();
        Task<string?> CreatePositions(bool isFromClosePosition);
    }
}
