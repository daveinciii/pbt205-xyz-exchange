using Microsoft.AspNetCore.SignalR;
using RabbitMQ.Client.Exceptions;
using TradingCore.Cli;
using TradingCore.Configuration;
using TradingCore.Models;
using TradingCore.Services;
using TradingGuiApp.Hubs;

namespace TradingGuiApp.Services
{
    // Background service that bridges RabbitMQ trade events to SignalR clients
    // and records them in the in-memory TradeHistory store so newly-connected
    // browsers can replay recent activity.
    //
    // Resilience: the broker may not be up when the GUI starts (common with
    // Docker Compose). Connection is wrapped in retry-with-backoff so the
    // web host stays alive while RabbitMQ finishes starting.
    public class TradeListenerService : BackgroundService
    {
        private const int MaxConnectAttempts = 5;
        private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(2);

        private readonly IHubContext<TradeHub> _hubContext;
        private readonly ILogger<TradeListenerService> _logger;
        private readonly IConfiguration _configuration;
        private readonly TradeHistory _history;

        public TradeListenerService(
            IHubContext<TradeHub> hubContext,
            ILogger<TradeListenerService> logger,
            IConfiguration configuration,
            TradeHistory history)
        {
            _hubContext = hubContext;
            _logger = logger;
            _configuration = configuration;
            _history = history;
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
                        return;
                    }
                    await Task.Delay(RetryDelay, stoppingToken);
                }
            }

            using (rabbitMQ)
            {
                rabbitMQ!.Subscribe<Trade>(RabbitMQService.TRADES_TOPIC, trade =>
                {
                    if (string.IsNullOrWhiteSpace(trade.Stock))
                    {
                        ConsoleUi.Error("Received trade with no Stock field. Skipping broadcast.");
                        return;
                    }

                    // Stage 4: record every broadcast trade in the in-memory
                    // history store so newly-connected browsers can replay
                    // recent activity via TradeHub.GetHistory / GetLatestPrices.
                    _history.Add(trade);

                    ConsoleUi.Box("Trade broadcast",
                        $"{trade.Stock,-5} {trade.Buyer,-10} ↔ {trade.Seller,-10} " +
                        $"{trade.Quantity} @ ${trade.Price:F2}");

                    _logger.LogInformation(
                        "Trade broadcast: {Stock} Buyer={Buyer} Seller={Seller} Qty={Qty} Price={Price:F2}",
                        trade.Stock, trade.Buyer, trade.Seller, trade.Quantity, trade.Price);

                    _hubContext.Clients.All.SendAsync("ReceiveTrade", trade, stoppingToken);
                });

                ConsoleUi.Box("GUI ready",
                    "Listening for trades on 'trades' exchange.",
                    "Open http://localhost:5219 in a browser.");

                while (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(1000, stoppingToken);
                }
            }
        }
    }
}