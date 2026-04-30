using Microsoft.AspNetCore.SignalR;
using RabbitMQ.Client.Exceptions;
using TradingCore.Cli;
using TradingCore.Configuration;
using TradingCore.Models;
using TradingCore.Services;
using TradingGuiApp.Hubs;

namespace TradingGuiApp.Services
{
    // Background service that bridges RabbitMQ trade events to SignalR clients.
    //
    // Subscribes to the 'trades' fanout exchange and forwards every completed
    // Trade to all connected browsers via the 'ReceiveTrade' SignalR method.
    // The full Trade object (including Stock) is sent intact so the dashboard
    // can route updates per-stock.
    //
    // Resilience: the broker may not be up when the GUI starts (common with
    // Docker Compose). Connection is wrapped in retry-with-backoff so the
    // web host stays alive while RabbitMQ finishes starting. After a fixed
    // number of failed attempts the service stays alive and logs the error
    // rather than crashing the host.
    public class TradeListenerService : BackgroundService
    {
        private const int MaxConnectAttempts = 5;
        private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(2);

        private readonly IHubContext<TradeHub> _hubContext;
        private readonly ILogger<TradeListenerService> _logger;
        private readonly IConfiguration _configuration;

        public TradeListenerService(
            IHubContext<TradeHub> hubContext,
            ILogger<TradeListenerService> logger,
            IConfiguration configuration)
        {
            _hubContext = hubContext;
            _logger = logger;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            string endpoint = _configuration["RabbitMQ:Endpoint"] ?? "localhost";
            var parts = endpoint.Split(':');
            string host = parts[0];
            int port    = parts.Length > 1 && int.TryParse(parts[1], out var parsedPort) ? parsedPort : 5672;

            ConsoleUi.Banner("Trading GUI — Starting");
            ConsoleUi.Box("Configured stocks", string.Join(", ", StockConfig.Stocks));
            ConsoleUi.Box("RabbitMQ", $"Endpoint: {host}:{port}");

            // Wrap the connection in retry-with-backoff. Without this, a broker
            // that is still starting up causes BrokerUnreachableException to
            // bubble up and kill the entire web host.
            RabbitMQService? rabbitMQ = null;
            for (int attempt = 1; attempt <= MaxConnectAttempts; attempt++)
            {
                try
                {
                    rabbitMQ = new RabbitMQService(host, port);
                    break;
                }
                catch (BrokerUnreachableException ex)
                {
                    ConsoleUi.Error(
                        $"Broker unreachable (attempt {attempt}/{MaxConnectAttempts}): {ex.Message}");
                    if (attempt == MaxConnectAttempts)
                    {
                        ConsoleUi.Error("Giving up on RabbitMQ. GUI will run but show no live trades.");
                        return; // exit ExecuteAsync — host stays alive, just no trades flow
                    }
                    await Task.Delay(RetryDelay, stoppingToken);
                }
            }

            // From here on the service owns the RabbitMQ connection. Wrapping
            // in `using` here so the channel is closed cleanly on shutdown.
            using (rabbitMQ)
            {
                rabbitMQ!.Subscribe<Trade>(RabbitMQService.TRADES_TOPIC, trade =>
                {
                    // Per-stock routing depends on the Stock field being present
                    // on every Trade. Stage 1.1 removed the "XYZ" default from
                    // the model so a missing value would surface as an empty
                    // string here — log it loudly if that ever happens.
                    if (string.IsNullOrWhiteSpace(trade.Stock))
                    {
                        ConsoleUi.Error("Received trade with no Stock field. Skipping broadcast.");
                        return;
                    }

                    ConsoleUi.Box("Trade broadcast",
                        $"{trade.Stock,-5} {trade.Buyer,-10} ↔ {trade.Seller,-10} " +
                        $"{trade.Quantity} @ ${trade.Price:F2}");

                    _logger.LogInformation(
                        "Trade broadcast: {Stock} Buyer={Buyer} Seller={Seller} Qty={Qty} Price={Price:F2}",
                        trade.Stock, trade.Buyer, trade.Seller, trade.Quantity, trade.Price);

                    // Send the full Trade object — Stock, Buyer, Seller, Quantity,
                    // Price, ExecutedAt all reach the browser intact for per-stock
                    // dashboard routing (FR-05).
                    _hubContext.Clients.All.SendAsync("ReceiveTrade", trade, stoppingToken);
                });

                ConsoleUi.Box("GUI ready",
                    "Listening for trades on 'trades' exchange.",
                    "Open http://localhost:5219 in a browser.");

                // Keep the background service alive until the host is stopped.
                while (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(1000, stoppingToken);
                }
            }
        }
    }
}