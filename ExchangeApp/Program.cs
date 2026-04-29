using TradingCore.Cli;
using TradingCore.Configuration;
using TradingCore.Models;
using TradingCore.Services;

if (args.Length < 1)
{
    Console.WriteLine("Usage: ExchangeApp <endpoint>");
    Console.WriteLine("Example: ExchangeApp localhost");
    return;
}

string endpoint = args[0];
var parts = endpoint.Split(':');
string host = parts[0];
int port    = parts.Length > 1 && int.TryParse(parts[1], out var parsedPort) ? parsedPort : 5672;

ConsoleUi.Banner("XYZ Corp Exchange — Starting");

// Print the configured stocks at startup so it's obvious from the CLI alone
// which symbols the exchange will accept (FR-01, TR-07).
ConsoleUi.Box("Configured stocks", string.Join(", ", StockConfig.Stocks));

var orderBook = new OrderBookService();
using var rabbitMQ = new RabbitMQService(host, port);

// Subscribe to the 'orders' fanout exchange. The callback fires on a background
// thread each time a new order arrives.
rabbitMQ.Subscribe<Order>(RabbitMQService.ORDERS_TOPIC, order =>
{
    // Defensive validation — drop any order for an unknown stock so a
    // misconfigured trader can't poison the book. Logged loudly so the
    // problem is visible during a demo.
    if (!StockConfig.IsValid(order.Stock))
    {
        ConsoleUi.Error($"Rejected: order for unknown stock '{order.Stock}' from {order.Username}");
        return;
    }

    ConsoleUi.Box("Order received",
        $"{order.Username,-10} {order.Side,-4} {order.Quantity} {order.Stock} @ ${order.Price,-9:F2}");

    var trade = orderBook.ProcessOrder(order);

    if (trade == null)
    {
        ConsoleUi.Box("No match found",
            "Order added to the order book.",
            $"Buy orders:  {orderBook.GetBuyOrders().Count,-4} Sell orders: {orderBook.GetSellOrders().Count}");
    }
    else
    {
        ConsoleUi.Box("Trade executed",
            $"Buyer:    {trade.Buyer,-10}  Seller:  {trade.Seller}",
            $"Stock:    {trade.Stock,-10}  Qty:     {trade.Quantity}",
            $"Price:    ${trade.Price,-9:F2} Time:    {trade.ExecutedAt:HH:mm:ss}",
            $"Book:     {orderBook.GetBuyOrders().Count} buys / {orderBook.GetSellOrders().Count} sells remaining");

        // Broadcast the completed trade to the 'trades' fanout exchange so any
        // subscribed application (the GUI, a reporting tool) is notified.
        rabbitMQ.Publish(RabbitMQService.TRADES_TOPIC, trade);
    }
});

ConsoleUi.Box("Exchange ready",
    "Waiting for orders...",
    "Press Ctrl+C to shut down.");

// Keep the process alive until Ctrl+C. CancellationToken pattern lets us
// dispose the RabbitMQ connection cleanly rather than killing the process abruptly.
var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true; // prevent immediate termination
    cts.Cancel();
    ConsoleUi.Box("Shutting down", "XYZ Exchange closing connection...");
};

try
{
    await Task.Delay(Timeout.Infinite, cts.Token);
}
catch (TaskCanceledException)
{
    // Expected on Ctrl+C — flow continues to dispose the RabbitMQ connection.
}