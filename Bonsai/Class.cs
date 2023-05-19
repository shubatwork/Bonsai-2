using CryptoExchange.Net.CommonObjects;

namespace Bonsai
{
    public class MacdFinalResult
    {
        public CommonOrderSide OrderSide { get; set; }
    }

    public class AdxFinalResult
    {
        public double AdxValue { get; set; }
        public bool IsTrending { get; set; }
        public Position? Position { get; set; }
    }
}
