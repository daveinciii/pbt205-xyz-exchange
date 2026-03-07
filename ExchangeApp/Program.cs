using TradingCore.Models;
using TradingCore.Services;

var orderBook = new OrderBookService();

Console.WriteLine("Exchange started.");
Console.WriteLine("Enter orders in this format:");
Console.WriteLine("username BUY 100 10.50");
Console.WriteLine("username SELL 100 10.20");
Console.WriteLine("Type EXIT to stop.");
Console.WriteLine();

while (true)
{
    var input = Console.ReadLine();

    if (input == null)
        break;

    if (input.Trim().Equals("EXIT", StringComparison.OrdinalIgnoreCase))
        break;

    var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);

    if (parts.Length != 4)
    {
        Console.WriteLine("Invalid format. Use: username BUY|SELL 100 price");
        continue;
    }

    string username = parts[0];
    string sideText = parts[1];
    string quantityText = parts[2];
    string priceText = parts[3];

    if (!Enum.TryParse<OrderSide>(sideText, true, out var side))
    {
        Console.WriteLine("Invalid side. Use BUY or SELL.");
        continue;
    }

    if (!int.TryParse(quantityText, out int quantity) || quantity != 100)
    {
        Console.WriteLine("Quantity must be 100.");
        continue;
    }

    if (!double.TryParse(priceText, out double price))
    {
        Console.WriteLine("Invalid price.");
        continue;
    }

    var order = new Order
    {
        Username = username,
        Stock = "XYZ",
        Side = side,
        Quantity = quantity,
        Price = price,
        CreatedAt = DateTime.UtcNow
    };

    var trade = orderBook.ProcessOrder(order);

    if (trade == null)
    {
        Console.WriteLine("No match found. Order added to order book.");
    }
    else
    {
        Console.WriteLine("Trade executed:");
        Console.WriteLine($"Stock: {trade.Stock}");
        Console.WriteLine($"Buyer: {trade.Buyer}");
        Console.WriteLine($"Seller: {trade.Seller}");
        Console.WriteLine($"Quantity: {trade.Quantity}");
        Console.WriteLine($"Price: {trade.Price:F2}");
        Console.WriteLine($"ExecutedAt (UTC): {trade.ExecutedAt:yyyy-MM-dd HH:mm:ss}");
    }

    Console.WriteLine();
}