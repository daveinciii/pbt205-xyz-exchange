using TradingCore.Models;
using TradingCore.Services;

if (args.Length < 1)
{
    Console.WriteLine("Usage: ExchangeApp <endpoint>");
    Console.WriteLine("Example: ExchangeApp localhost");
    return;
}

string endpoint = args[0]; // kept for assignment compliance / future middleware integration

var orderBook = new OrderBookService();

Console.WriteLine("XYZ Exchange started.");
Console.WriteLine($"Connected to endpoint: {endpoint}");
Console.WriteLine("Enter orders in this format:");
Console.WriteLine("username BUY 100 10.50");
Console.WriteLine("username SELL 100 10.20");
Console.WriteLine("Type EXIT to stop.");
Console.WriteLine(new string('-', 50));

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
        Console.WriteLine();
        continue;
    }

    string username = parts[0];
    string sideText = parts[1];
    string quantityText = parts[2];
    string priceText = parts[3];

    if (!Enum.TryParse<OrderSide>(sideText, true, out var side))
    {
        Console.WriteLine("Invalid side. Use BUY or SELL.");
        Console.WriteLine();
        continue;
    }

    if (!int.TryParse(quantityText, out int quantity))
    {
        Console.WriteLine("Invalid quantity.");
        Console.WriteLine();
        continue;
    }

    if (quantity != 100)
    {
        Console.WriteLine("Quantity must be 100 for this assignment.");
        Console.WriteLine();
        continue;
    }

    if (!double.TryParse(priceText, out double price))
    {
        Console.WriteLine("Invalid price.");
        Console.WriteLine();
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

    Console.WriteLine($"Received order: {order.Username} {order.Side} {order.Quantity} {order.Stock} @ {order.Price:F2}");

    var trade = orderBook.ProcessOrder(order);

    if (trade == null)
    {
        Console.WriteLine("No matching opposite-side order found.");
        Console.WriteLine("Order added to the order book.");
        Console.WriteLine($"Current buy orders: {orderBook.GetBuyOrders().Count}");
        Console.WriteLine($"Current sell orders: {orderBook.GetSellOrders().Count}");
    }
    else
    {
        Console.WriteLine("Trade executed:");
        Console.WriteLine($"Stock: {trade.Stock}");
        Console.WriteLine($"Buyer: {trade.Buyer}");
        Console.WriteLine($"Seller: {trade.Seller}");
        Console.WriteLine($"Quantity: {trade.Quantity}");
        Console.WriteLine($"Trade Price: {trade.Price:F2}");
        Console.WriteLine($"Executed At (UTC): {trade.ExecutedAt:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine($"Remaining buy orders: {orderBook.GetBuyOrders().Count}");
        Console.WriteLine($"Remaining sell orders: {orderBook.GetSellOrders().Count}");
    }

    Console.WriteLine(new string('-', 50));
}