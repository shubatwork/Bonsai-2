using Bonsai.Services;
using Bonsai.Workers;
using MakeMeRich.Binance.Services;
using MakeMeRich.Binance.Services.Interfaces;
using TechnicalAnalysis.Business;

namespace Bonsai
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            builder.Services.AddTransient<IDataAnalysisService, DataAnalysisService>();
            builder.Services.AddTransient<IDataHistoryRepository, DataHistoryRepository>();
            builder.Services.AddTransient<IStopLossService, StopLossService>();
            builder.Services.AddHostedService<DataAnalysisWorkerService05>();
            builder.Services.AddHostedService<DataAnalysisWorkerService03>();
            builder.Services.AddHostedService<DataAnalysisWorkerService15>();
            builder.Services.AddHostedService<DataAnalysisWorkerService30>();
            builder.Services.AddHostedService<DataAnalysisWorkerService60>();
            builder.Services.AddHostedService<DataAnalysisWorkerService1Day>();
            builder.Services.AddHostedService<DataAnalysisWorkerService01>();

            builder.Services.AddHostedService<ProfitWorkerService>();
           // builder.Services.AddHostedService<DataAnalysisSonaWorkerService>();


            var app = builder.Build();

            // Configure the HTTP request pipeline.

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}