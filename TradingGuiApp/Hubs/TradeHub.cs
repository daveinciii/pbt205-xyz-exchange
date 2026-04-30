using Microsoft.AspNetCore.SignalR;
using TradingCore.Models;
using TradingGuiApp.Services;

namespace TradingGuiApp.Hubs
{
    // SignalR hub for the trading dashboard.
    //
    // Browser → server: `GetHistory(stock)` and `GetLatestPrices()` are
    //   invoked on connect to populate the dashboard before any new trades
    //   arrive. This avoids the "blank screen until first trade" problem.
    //
    // Server → browser: `ReceiveTrade` is invoked by TradeListenerService
    //   whenever a new trade is broadcast through the 'trades' RabbitMQ
    //   exchange.
    public class TradeHub : Hub
    {
        private readonly TradeHistory _history;

        public TradeHub(TradeHistory history)
        {
            _history = history;
        }

        // Returns the most recent trades, optionally filtered by stock.
        // Called by the dashboard on connect and when the user clicks a
        // filter pill. Limit is capped server-side to keep the payload sane.
        public Task<IReadOnlyList<Trade>> GetHistory(string? stock = null, int limit = 50)
        {
            if (limit < 1) limit = 1;
            if (limit > 100) limit = 100;
            return Task.FromResult(_history.GetRecent(limit, stock));
        }

        // Returns the latest trade for each stock, keyed by symbol.
        // The dashboard uses this to populate price tiles on connect.
        public Task<IReadOnlyDictionary<string, Trade>> GetLatestPrices()
        {
            return Task.FromResult(_history.GetLatestPerStock());
        }
    }
}