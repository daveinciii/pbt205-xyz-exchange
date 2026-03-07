namespace TradingCore.Models
{
    public class Order
    {
        public string Username { get; set; } = "";
        public string Stock { get; set; } = "XYZ";
        public OrderSide Side { get; set; }
        public int Quantity { get; set; } = 100;
        public double Price { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}