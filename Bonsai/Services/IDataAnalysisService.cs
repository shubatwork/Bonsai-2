namespace Bonsai.Services
{
    public interface IDataAnalysisService
    {
        Task<string?> CreatePositions(List<string> notToBeTakenPosition);
        Task<string?> ClosePositions();
    }
}
