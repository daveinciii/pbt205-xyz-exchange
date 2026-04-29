namespace TradingCore.Models
{
    // A buy or sell order published by SendOrderApp to the 'orders' fanout exchange.
    //
    // Stock no longer defaults to "XYZ" — multi-stock support (FR-01) means the
    // trader must always specify which stock the order applies to. SendOrderApp
    // validates the symbol against StockConfig before publishing.
    public class Order
    {
        public string Username { get; set; } = "";
        public string Stock    { get; set; } = "";
        public OrderSide Side  { get; set; }
        public int Quantity    { get; set; } = 100;
        public double Price    { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}