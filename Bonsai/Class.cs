using Binance.Net.Interfaces;
using CryptoExchange.Net.CommonObjects;
using TechnicalAnalysis.Business;

namespace Bonsai
{
    public class RsiFinalResult
    {
        public CommonOrderSide OrderSide { get; set; }
    }

    public class BollingerResult
    {
        public double UpperBand { get; set; }
        public double LowerBand { get; set; }
        public double MiddleBand { get; set; }
    }

    public class FinalResult
    {
        public decimal PercentRise { get; set; }
        public string? Symbol { get; set; }
    }

    public class DailyResult
    {
        public double AdxValue { get; set; }
        public IBinance24HPrice? DailyPrice { get; set; }
    }
    public class AnalysisResult
    {
        public List<DailyResult>? DailyResult{ get; set; }
        public List<DailyResult>? HourlyResult { get; set; }
        public List<DailyResult>? FiveMinResult { get; set; }

    }
}

