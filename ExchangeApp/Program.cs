using Newtonsoft.Json.Linq;
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
int port = parts.Length > 1 && int.TryParse(parts[1], out var parsedPort) ? parsedPort : 5672;

ConsoleUi.Banner("XYZ Corp Exchange — Starting");

ConsoleUi.Box("Configured stocks", string.Join(", ", StockConfig.Stocks));

string databasePath = "trading.db";

try
{
    if (File.Exists("tradingsystem.config.json"))
    {
        var json = JObject.Parse(File.ReadAllText("tradingsystem.config.json"));
        databasePath = json["Database"]?["Path"]?.ToString() ?? "trading.db";
    }
}
catch
{
    databasePath = "trading.db";
}

var persistence = new TradePersistenceService(databasePath);
persistence.EnsureDatabase();

var historicalTrades = persistence.GetAllTrades();

ConsoleUi.Box("Database",
    $"SQLite path: {databasePath}",
    $"Historical trades loaded: {historicalTrades.Count}");

var orderBook = new OrderBookService();
using var rabbitMQ = new RabbitMQService(host, port);

rabbitMQ.Subscribe<Order>(RabbitMQService.ORDERS_TOPIC, order =>
{
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
        try
        {
            persistence.SaveTrade(trade);
        }
        catch (Exception ex)
        {
            ConsoleUi.Error($"Trade executed but database save failed: {ex.Message}");
        }

        ConsoleUi.Box("Trade executed",
            $"Buyer:    {trade.Buyer,-10}  Seller:  {trade.Seller}",
            $"Stock:    {trade.Stock,-10}  Qty:     {trade.Quantity}",
            $"Price:    ${trade.Price,-9:F2} Time:    {trade.ExecutedAt:HH:mm:ss}",
            $"Book:     {orderBook.GetBuyOrders().Count} buys / {orderBook.GetSellOrders().Count} sells remaining");

        rabbitMQ.Publish(RabbitMQService.TRADES_TOPIC, trade);
    }
});

ConsoleUi.Box("Exchange ready",
    "Waiting for orders...",
    "Press Ctrl+C to shut down.");

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
    ConsoleUi.Box("Shutting down", "XYZ Exchange closing connection...");
};

try
{
    await Task.Delay(Timeout.Infinite, cts.Token);
}
catch (TaskCanceledException)
{
}