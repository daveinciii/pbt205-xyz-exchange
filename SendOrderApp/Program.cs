using TradingCore.Models;
using TradingCore.Services;

// Validate that all required arguments are provided at startup
// Required: username, endpoint, side (BUY/SELL), quantity, price
if (args.Length < 5)
{
    Console.WriteLine("Usage: SendOrderApp <username> <endpoint> <BUY|SELL> <quantity> $<price>");
    Console.WriteLine("Example: SendOrderApp David localhost BUY 100 $10.50");
    return;
}

string username = args[0];
string endpoint = args[1]; // kept for assignment compliance / future middleware integration
string sideText = args[2];
string quantityText = args[3];
string priceText = args[4];

// --- MIDDLEWARE ADDITION ---
// Parse the endpoint into host and port components.
// Supports both "localhost" (defaults to RabbitMQ's standard AMQP port 5672)
// and "localhost:5672" formats for flexibility.
var parts = endpoint.Split(':');
string host = parts[0];
int port = parts.Length == 2 && int.TryParse(parts[1], out int p) ? p : 5672;

// Validate the order side — must be BUY or SELL (case-insensitive)
if (!Enum.TryParse<OrderSide>(sideText, true, out var side))
{
    Console.WriteLine("┌─ ERROR ──────────────────────────────────────────────────┐");
    Console.WriteLine("│ Invalid side. Use BUY or SELL.                           │");
    Console.WriteLine("└──────────────────────────────────────────────────────────┘");
    return;
}
// Validate quantity is a valid integer
if (!int.TryParse(quantityText, out int quantity))
{
    Console.WriteLine("┌─ ERROR ──────────────────────────────────────────────────┐");
    Console.WriteLine("│ Invalid quantity. Must be a whole number.                │");
    Console.WriteLine("└──────────────────────────────────────────────────────────┘");
    return;
}
// Per assignment spec, all orders are fixed at 100 shares
if (quantity != 100)
{
    Console.WriteLine("┌─ ERROR ──────────────────────────────────────────────────┐");
    Console.WriteLine("│ Quantity must be 100 for this assignment.                │");
    Console.WriteLine("└──────────────────────────────────────────────────────────┘");
    return;
}
// Validate price is a valid decimal number
if (!double.TryParse(priceText, out double price))
{
    Console.WriteLine("┌─ ERROR ──────────────────────────────────────────────────┐");
    Console.WriteLine("│ Invalid price. Must be a number e.g. 10.50               │");
    Console.WriteLine("└──────────────────────────────────────────────────────────┘");
    return;
}

// Build the Order object using the validated arguments.
// Stock is hardcoded to "XYZ" as per the assignment spec (single stock exchange).
// CreatedAt is set in UTC for consistent timestamps across machines.
var order = new Order
{
    Username = username,
    Stock = "XYZ",
    Side = side,
    Quantity = quantity,
    Price = price,
    CreatedAt = DateTime.UtcNow
};

// Display a summary of the order before submission
Console.WriteLine($"┌─ ORDER CREATED ──────────────────────────┐");
Console.WriteLine($"│ User:     {order.Username,-10}  Stock:    {order.Stock,-10}");
Console.WriteLine($"│ Side:     {order.Side,-10}  Qty:      {order.Quantity,-10}");
Console.WriteLine($"│ Price:    ${order.Price,-9:F2}  Endpoint: {endpoint,-10}");
Console.WriteLine($"│ Time:     {order.CreatedAt:yyyy-MM-dd HH:mm:ss} UTC         ");
Console.WriteLine($"└──────────────────────────────────────────┘");

// --- MIDDLEWARE ADDITION ---
// Connect to RabbitMQ and publish the order to the 'orders' fanout exchange.
// The ExchangeApp subscribes to this exchange and will receive the order
// automatically — no direct connection between SendOrderApp and ExchangeApp.
// Per the assignment spec, this app exits immediately after publishing.
using var mq = new RabbitMQService(host, port);
mq.Publish(RabbitMQService.ORDERS_TOPIC, order);

Console.WriteLine($"┌─ SUBMITTED ──────────────────────────────┐");
Console.WriteLine($"│ Order sent to XYZ Exchange via RabbitMQ. │");
Console.WriteLine($"│ Exiting...                               │");
Console.WriteLine($"└──────────────────────────────────────────┘");