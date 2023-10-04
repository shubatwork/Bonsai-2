﻿using CryptoExchange.Net.CommonObjects;
using TechnicalAnalysis.Business;

namespace Bonsai
{
    public class RsiFinalResult
    {
        public CommonOrderSide OrderSide { get; set; }
    }

    public class AdxFinalResult
    {
        public double AdxValue { get; set; }
        public Position? Position { get; set; }
    }

    public class FinalResult
    {
        public double AdxValue { get; set; }
        public Position? Position { get; set; }
        public DataHistory? DataHistory { get; set; }
        public CommonOrderSide? OrderSide { get; set; }
        public decimal? OpenPrice { get; set; }
        public double? RsiValue { get; set; }
    }

    public class DailyResult
    {
        public string Symbol { get; set; }
        public double AdxValue { get; set; }
        public CommonOrderSide? OrderSide { get; set; }
        public decimal? OpenPrice { get; set; }
        public double? RsiValue { get; set; }
        public Position? Position { get; set; }
    }
    public class AnalysisResult
    {
        public List<DailyResult>? DailyResult{ get; set; }
        public List<DailyResult>? HourlyResult { get; set; }
        public List<DailyResult>? FiveMinResult { get; set; }

    }
}

