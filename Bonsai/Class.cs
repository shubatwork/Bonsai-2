using CryptoExchange.Net.CommonObjects;
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
    }
}

