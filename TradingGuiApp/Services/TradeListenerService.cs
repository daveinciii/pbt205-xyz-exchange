using Microsoft.AspNetCore.SignalR;
using TradingCore.Models;
using TradingCore.Services;
using TradingGuiApp.Hubs;

namespace TradingGuiApp.Services
{
    public class TradeListenerService : BackgroundService
    {
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
            int port = parts.Length > 1 && int.TryParse(parts[1], out var parsedPort) ? parsedPort : 5672;

            using var rabbitMQ = new RabbitMQService(host, port);

            rabbitMQ.Subscribe<Trade>(RabbitMQService.TRADES_TOPIC, trade =>
            {
                _logger.LogInformation(
                    "Trade received: {Stock} Buyer={Buyer} Seller={Seller} Qty={Qty} Price={Price}",
                    trade.Stock, trade.Buyer, trade.Seller, trade.Quantity, trade.Price);

                _hubContext.Clients.All.SendAsync("ReceiveTrade", trade, stoppingToken);
            });

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}