namespace TradingCore.Models
{
    public class Trade
    {
        public string Stock { get; set; } = "XYZ";
        public string Buyer { get; set; } = "";
        public string Seller { get; set; } = "";
        public int Quantity { get; set; } = 100;
        public double Price { get; set; }
        public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
    }
}