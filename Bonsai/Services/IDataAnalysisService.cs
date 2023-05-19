namespace Bonsai.Services
{
    public interface IDataAnalysisService
    {
        Task CreatePositions();

        Task ClosePositions();
    }
}