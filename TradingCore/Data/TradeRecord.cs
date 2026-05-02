namespace TradingCore.Data
{
    public class TradeRecord
    {
        public int Id { get; set; }

        public string Stock { get; set; } = "";
        public string Buyer { get; set; } = "";
        public string Seller { get; set; } = "";

        public int Quantity { get; set; }
        public double Price { get; set; }

        public DateTime ExecutedAt { get; set; }
    }
}