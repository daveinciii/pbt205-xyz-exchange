using TradingCore.Models;
using TradingCore.Services;

// Validate that a middleware endpoint argument was provided at startup
if (args.Length < 1)
{
    Console.WriteLine("Usage: ExchangeApp <endpoint>");
    Console.WriteLine("Example: ExchangeApp localhost");
    return;
}

string endpoint = args[0];

// --- MIDDLEWARE ADDITION ---
// Parse the endpoint into host and port components.
// Supports both "localhost" (defaults to port 5672) and "localhost:5672" formats.
// Port 5672 is the default AMQP port used by RabbitMQ.
var parts = endpoint.Split(':');
string host = parts[0];
int port = parts.Length > 1 && int.TryParse(parts[1], out var p) ? p : 5672;

// Initialise the order book that holds unmatched buy and sell orders
var orderBook = new OrderBookService();


Console.WriteLine("╔══════════════════════════════════════════╗");
Console.WriteLine("║       XYZ Corp Exchange — Starting       ║");
Console.WriteLine("╚══════════════════════════════════════════╝");

// --- MIDDLEWARE ADDITION ---
// Create a connection to RabbitMQ using the parsed host and port.
// RabbitMQService declares both the 'orders' and 'trades' fanout exchanges
// on startup so they are ready before any messages are sent or received.
using var rabbitMQ = new RabbitMQService(host, port);

// --- MIDDLEWARE ADDITION ---
// Subscribe to the 'orders' fanout exchange.
// Instead of reading orders from the console (previous version),
// the exchange now receives orders automatically over RabbitMQ
// whenever a SendOrderApp instance publishes to the 'orders' topic.
// The callback below fires on a background thread each time a new order arrives.
rabbitMQ.Subscribe<Order>(RabbitMQService.ORDERS_TOPIC, order =>
{
    Console.WriteLine($"┌─ ORDER RECEIVED ─────────────────────────┐");
    Console.WriteLine($"  {order.Username,-10} {order.Side,-4}  {order.Quantity} {order.Stock}  @  ${order.Price:F2,-10}");
    Console.WriteLine($"└──────────────────────────────────────────┘");
    
    // Pass the incoming order to the order book for matching
    var trade = orderBook.ProcessOrder(order);
    
    if (trade == null)
    {
        // No matching opposite-side order was found — order sits in the book
        Console.WriteLine($"┌─ NO MATCH FOUND ─────────────────────────┐");
        Console.WriteLine($"  Order added to the order book.            │");
        Console.WriteLine($"  Buy orders:  {orderBook.GetBuyOrders().Count,-4}  Sell orders: {orderBook.GetSellOrders().Count,-4}          │");
        Console.WriteLine($"└──────────────────────────────────────────┘");
    }
    else
    {
        // A matching order was found — display the completed trade details
        Console.WriteLine($"┌─ TRADE EXECUTED ─────────────────────────┐");
        Console.WriteLine($"  Buyer:    {trade.Buyer,-10}  Seller:  {trade.Seller,-10}");
        Console.WriteLine($"  Stock:    {trade.Stock,-10}  Qty:     {trade.Quantity,-10}");
        Console.WriteLine($"  Price:    ${trade.Price,-9:F2}  Time:    {trade.ExecutedAt:HH:mm:ss}");
        Console.WriteLine($"  Book:     {orderBook.GetBuyOrders().Count} buys  │  {orderBook.GetSellOrders().Count} sell remaining          │");
        Console.WriteLine($"└──────────────────────────────────────────┘");

        // --- MIDDLEWARE ADDITION ---
        // Publish the completed trade to the 'trades' fanout exchange.
        // Any application subscribed to 'trades' (e.g. a GUI or reporting tool)
        // will automatically receive this trade confirmation.
        rabbitMQ.Publish(RabbitMQService.TRADES_TOPIC, trade);
        Console.WriteLine($"│ >> Broadcast to '{RabbitMQService.TRADES_TOPIC}'.");
    }

});
Console.WriteLine($"┌─ EXCHANGE READY ─────────────────────────┐");
Console.WriteLine($"│ Waiting for orders...                    │");
Console.WriteLine($"│ Press Ctrl+C to shut down.               │");
Console.WriteLine($"└──────────────────────────────────────────┘");

// --- MIDDLEWARE ADDITION ---
// Keep the application alive indefinitely so it can continue receiving
// orders from RabbitMQ. Previously the app used a while(true) console loop —
// now we block the main thread using Task.Delay and a CancellationToken,
// which allows a clean shutdown on Ctrl+C without killing the process abruptly.
var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true; // Prevent the process from terminating immediately
    cts.Cancel();
Console.WriteLine("\n┌─ SHUTTING DOWN ──────────────────────────┐");
Console.WriteLine("│ XYZ Exchange closing connection...       │");
Console.WriteLine("└──────────────────────────────────────────┘");
};

try
{
    // Block here until cancellation is requested via Ctrl+C
    await Task.Delay(Timeout.Infinite, cts.Token);
}

catch (TaskCanceledException)
{
   // Expected when Ctrl+C is pressed — allows clean disposal of RabbitMQ connection
}