using TradingCore.Models;
using TradingCore.Services;

namespace TradingGuiApp.Services
{
    public class TradeHistory
    {
        private readonly TradePersistenceService _persistence;

        public TradeHistory(IConfiguration configuration)
        {
            var databasePath = configuration["Database:Path"] ?? "trading.db";
            _persistence = new TradePersistenceService(databasePath);
            _persistence.EnsureDatabase();
        }

        public void Add(Trade trade)
        {
            _persistence.SaveTrade(trade);
        }

        public IReadOnlyList<Trade> GetRecent(int limit = 50, string? stock = null)
        {
            var trades = _persistence.GetAllTrades();

            if (!string.IsNullOrWhiteSpace(stock))
            {
                trades = trades
                    .Where(t => string.Equals(t.Stock, stock, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            return trades
                .OrderByDescending(t => t.ExecutedAt)
                .Take(limit)
                .ToList();
        }

        public IReadOnlyDictionary<string, Trade> GetLatestPerStock()
        {
            return _persistence.GetAllTrades()
                .GroupBy(t => t.Stock, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key.ToUpperInvariant(),
                    g => g.OrderByDescending(t => t.ExecutedAt).First());
        }
    }
}