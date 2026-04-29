using TradingCore.Configuration;
using TradingCore.Models;
using TradingCore.Services;

// Stage 1 scope: stock arg is added in Stage 2.1. For now SendOrderApp still
// uses the 5-arg signature but validates the (currently hardcoded) "XYZ" stock
// symbol against StockConfig — proving the validation wiring works end-to-end
// before the CLI surface changes.

if (args.Length < 5)
{
    Console.WriteLine("Usage: SendOrderApp <username> <endpoint> <BUY|SELL> <quantity> <price>");
    Console.WriteLine("Example: SendOrderApp David localhost BUY 100 10.50");
    return;
}

string username     = args[0];
string endpoint     = args[1];
string sideText     = args[2];
string quantityText = args[3];
string priceText    = args[4];

// Parse the endpoint into host and port. Supports "localhost" (defaults to
// 5672) and "localhost:5672" formats — useful when the broker isn't on the
// default port (e.g. a remote staging broker).
var parts = endpoint.Split(':');
string host = parts[0];
int port    = parts.Length == 2 && int.TryParse(parts[1], out int parsedPort) ? parsedPort : 5672;

if (!Enum.TryParse<OrderSide>(sideText, true, out var side))
{
    Console.WriteLine("┌─ ERROR ──────────────────────────────────────────────────┐");
    Console.WriteLine("│ Invalid side. Use BUY or SELL.                           │");
    Console.WriteLine("└──────────────────────────────────────────────────────────┘");
    return;
}

if (!int.TryParse(quantityText, out int quantity))
{
    Console.WriteLine("┌─ ERROR ──────────────────────────────────────────────────┐");
    Console.WriteLine("│ Invalid quantity. Must be a whole number.                │");
    Console.WriteLine("└──────────────────────────────────────────────────────────┘");
    return;
}

// Per the assessment brief, all orders are fixed at 100 shares.
if (quantity != 100)
{
    Console.WriteLine("┌─ ERROR ──────────────────────────────────────────────────┐");
    Console.WriteLine("│ Quantity must be 100 for this assignment.                │");
    Console.WriteLine("└──────────────────────────────────────────────────────────┘");
    return;
}

if (!double.TryParse(priceText, out double price))
{
    Console.WriteLine("┌─ ERROR ──────────────────────────────────────────────────┐");
    Console.WriteLine("│ Invalid price. Must be a number e.g. 10.50               │");
    Console.WriteLine("└──────────────────────────────────────────────────────────┘");
    return;
}

// FR-01 validation hook: validate the stock symbol against the configured list
// before publishing. In Stage 1 the symbol is still hardcoded to "XYZ" —
// Stage 2.1 promotes it to a required positional argument. Either way the
// validation path is the same, which is the whole point of doing this now.
const string stockSymbol = "XYZ";
if (!StockConfig.IsValid(stockSymbol))
{
    Console.WriteLine("┌─ ERROR ──────────────────────────────────────────────────┐");
    Console.WriteLine($"│ Stock '{stockSymbol}' is not configured.                       │");
    Console.WriteLine("│ Edit tradingsystem.config.json to add it.                │");
    Console.WriteLine("└──────────────────────────────────────────────────────────┘");
    return;
}

var order = new Order
{
    Username  = username,
    Stock     = StockConfig.Normalise(stockSymbol),
    Side      = side,
    Quantity  = quantity,
    Price     = price,
    CreatedAt = DateTime.UtcNow
};

Console.WriteLine($"┌─ ORDER CREATED ──────────────────────────┐");
Console.WriteLine($"│ User:     {order.Username,-10}  Stock:    {order.Stock,-10}");
Console.WriteLine($"│ Side:     {order.Side,-10}  Qty:      {order.Quantity,-10}");
Console.WriteLine($"│ Price:    ${order.Price,-9:F2}  Endpoint: {endpoint,-10}");
Console.WriteLine($"│ Time:     {order.CreatedAt:yyyy-MM-dd HH:mm:ss} UTC         ");
Console.WriteLine($"└──────────────────────────────────────────┘");

// Connect to RabbitMQ and publish the order to the 'orders' fanout exchange.
// ExchangeApp subscribes to this exchange and will receive the order via the
// broker — no direct connection between SendOrderApp and ExchangeApp.
// Per the assessment spec, this app exits immediately after publishing.
using var mq = new RabbitMQService(host, port);
mq.Publish(RabbitMQService.ORDERS_TOPIC, order);

Console.WriteLine($"┌─ SUBMITTED ──────────────────────────────┐");
Console.WriteLine($"│ Order sent to XYZ Exchange via RabbitMQ. │");
Console.WriteLine($"│ Exiting...                               │");
Console.WriteLine($"└──────────────────────────────────────────┘");