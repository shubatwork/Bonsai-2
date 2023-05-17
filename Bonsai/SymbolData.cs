using CryptoExchange.Net.CommonObjects;

namespace Bonsai
{
    public class SymbolData
    {
        public double? AdxValue { get; set; }
        public decimal CurrentPrice { get; set; }
        public string Symbol { get; set; } = "";
        public ModeData? BuyData { get; set; }
        public ModeData? SellData { get; set; }
        public CommonOrderSide? Mode { get; set; }
    }

    public class ModeData
    {
        public CommonOrderSide? Mode { get; set; }
        public decimal? ModeFactor { get; set; }
    }
}
