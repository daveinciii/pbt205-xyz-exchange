namespace TradingCore.Models
{
    // A completed trade published by ExchangeApp to the 'trades' fanout exchange
    // whenever a buy and a sell order are matched.
    //
    // Stock no longer defaults to "XYZ" — every Trade is tagged with the stock
    // it relates to so subscribers (the GUI) can route per-stock updates.
    public class Trade
    {
        public string Stock     { get; set; } = "";
        public string Buyer     { get; set; } = "";
        public string Seller    { get; set; } = "";
        public int    Quantity  { get; set; } = 100;
        public double Price     { get; set; }
        public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
    }
}