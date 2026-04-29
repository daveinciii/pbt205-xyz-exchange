using TradingCore.Cli;
using TradingCore.Configuration;
using TradingCore.Models;
using TradingCore.Services;

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
    ConsoleUi.Error("Invalid side. Use BUY or SELL.");
    return;
}

if (!int.TryParse(quantityText, out int quantity))
{
    ConsoleUi.Error("Invalid quantity. Must be a whole number.");
    return;
}

// Per the assessment brief, all orders are fixed at 100 shares.
if (quantity != 100)
{
    ConsoleUi.Error("Quantity must be 100 for this assessment.");
    return;
}

if (!double.TryParse(priceText, out double price))
{
    ConsoleUi.Error("Invalid price. Must be a number e.g. 10.50");
    return;
}

// FR-01 validation hook: validate the stock symbol against the configured list
// before publishing. In Stage 1 the symbol is still hardcoded to "XYZ" —
// Stage 2.1 promotes it to a required positional argument. Either way the
// validation path is the same.
const string stockSymbol = "XYZ";
if (!StockConfig.IsValid(stockSymbol))
{
    ConsoleUi.Error($"Stock '{stockSymbol}' is not configured. Edit tradingsystem.config.json.");
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

ConsoleUi.Box("Order created",
    $"User:     {order.Username,-10}  Stock:    {order.Stock}",
    $"Side:     {order.Side,-10}  Qty:      {order.Quantity}",
    $"Price:    ${order.Price,-8:F2} Endpoint: {endpoint}",
    $"Time:     {order.CreatedAt:yyyy-MM-dd HH:mm:ss} UTC");

// Connect to RabbitMQ and publish the order to the 'orders' fanout exchange.
// ExchangeApp subscribes to this exchange and will receive the order via the
// broker — no direct connection between SendOrderApp and ExchangeApp.
// Per spec, this app exits immediately after publishing.
using var mq = new RabbitMQService(host, port);
mq.Publish(RabbitMQService.ORDERS_TOPIC, order);

ConsoleUi.Box("Submitted",
    "Order sent to XYZ Exchange via RabbitMQ.",
    "Exiting...");