using TradingCore.Cli;
using TradingCore.Configuration;
using TradingCore.Models;
using TradingCore.Services;

// Stage 2.1: stock is now a required positional argument (FR-01).
// Argument order is: username, endpoint, stock, side, quantity, price.
// This is a breaking change from the A1 / Stage 1 5-arg signature —
// the old form is rejected with a clear usage message rather than
// silently misinterpreting the arguments.

if (args.Length < 6)
{
    ConsoleUi.Error($"Missing arguments. Got {args.Length}, need 6.");
    ConsoleUi.Box("Usage",
        "SendOrderApp <username> <endpoint> <stock> <side> <qty> <price>",
        "Example: SendOrderApp David localhost XYZ BUY 100 10.50",
        $"Stocks: {string.Join(", ", StockConfig.Stocks)}");
    return;
}

string username     = args[0];
string endpoint     = args[1];
string stockText    = args[2];
string sideText     = args[3];
string quantityText = args[4];
string priceText    = args[5];

// Parse the endpoint into host and port. Supports "localhost" (defaults to
// 5672) and "localhost:5672" formats — useful when the broker isn't on the
// default port (e.g. a remote staging broker).
var parts = endpoint.Split(':');
string host = parts[0];
int port    = parts.Length == 2 && int.TryParse(parts[1], out int parsedPort) ? parsedPort : 5672;

// FR-01: validate the stock symbol against the configured list before doing
// any other work. Catching a typo here means we never publish a poison
// message to the orders exchange.
if (!StockConfig.IsValid(stockText))
{
    ConsoleUi.Error(
        $"Unknown stock '{stockText}'. Configured: {string.Join(", ", StockConfig.Stocks)}");
    return;
}
string stockSymbol = StockConfig.Normalise(stockText);

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

var order = new Order
{
    Username  = username,
    Stock     = stockSymbol,
    Side      = side,
    Quantity  = quantity,
    Price     = price,
    CreatedAt = DateTime.UtcNow
};

ConsoleUi.Box("Order created",
    $"User:     {order.Username,-10}  Stock:    {order.Stock}",
    $"Side:     {order.Side,-10}  Qty:      {order.Quantity}",
    $"Price:    ${order.Price,-8:F2}  Endpoint: {endpoint}",
    $"Time:     {order.CreatedAt:yyyy-MM-dd HH:mm:ss} UTC");

// Connect to RabbitMQ and publish the order to the 'orders' fanout exchange.
// ExchangeApp subscribes to this exchange and will receive the order via the
// broker — no direct connection between SendOrderApp and ExchangeApp.
// Per spec, this app exits immediately after publishing.
using var mq = new RabbitMQService(host, port);
mq.Publish(RabbitMQService.ORDERS_TOPIC, order);

ConsoleUi.Box("Submitted",
    $"{order.Side} {order.Quantity} {order.Stock} @ ${order.Price:F2} sent to exchange.",
    "Exiting...");