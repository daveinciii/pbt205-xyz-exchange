using TradingCore.Models;

if (args.Length < 5)
{
    Console.WriteLine("Usage: SendOrderApp <username> <endpoint> <BUY|SELL> <quantity> <price>");
    Console.WriteLine("Example: SendOrderApp David localhost BUY 100 10.50");
    return;
}

string username = args[0];
string endpoint = args[1]; // kept for assignment compliance / future middleware integration
string sideText = args[2];
string quantityText = args[3];
string priceText = args[4];

if (!Enum.TryParse<OrderSide>(sideText, true, out var side))
{
    Console.WriteLine("Invalid side. Use BUY or SELL.");
    return;
}

if (!int.TryParse(quantityText, out int quantity))
{
    Console.WriteLine("Invalid quantity.");
    return;
}

if (quantity != 100)
{
    Console.WriteLine("Quantity must be 100 for this assignment.");
    return;
}

if (!double.TryParse(priceText, out double price))
{
    Console.WriteLine("Invalid price.");
    return;
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

Console.WriteLine("Order created successfully:");
Console.WriteLine($"User: {order.Username}");
Console.WriteLine($"Endpoint: {endpoint}");
Console.WriteLine($"Stock: {order.Stock}");
Console.WriteLine($"Side: {order.Side}");
Console.WriteLine($"Quantity: {order.Quantity}");
Console.WriteLine($"Price: {order.Price:F2}");
Console.WriteLine($"CreatedAt (UTC): {order.CreatedAt:yyyy-MM-dd HH:mm:ss}");
Console.WriteLine();
Console.WriteLine("This is the core app logic version. Middleware publishing will be added separately.");