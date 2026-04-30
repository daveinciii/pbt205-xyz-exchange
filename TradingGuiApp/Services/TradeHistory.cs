using System.Collections.Concurrent;
using TradingCore.Models;

namespace TradingGuiApp.Services
{
    // Thread-safe in-memory ring buffer of the most recent trades.
    //
    // Stage 4 uses this so the dashboard can show prior activity when a
    // browser connects, instead of going blank until the next trade fires.
    //
    // TODO (Stage 1.2 / 2.2 — David): replace the in-memory buffer with a
    // query against the SQLite trade history once EF Core persistence
    // lands. The Add/GetRecent/GetLatestPerStock surface should stay the
    // same so TradeHub does not need to change.
    public class TradeHistory
    {
        private const int Capacity = 100;

        // ConcurrentQueue lets the listener Add and the hub GetRecent
        // run on different threads without locking. Trim happens after
        // each Add so the queue never grows past Capacity.
        private readonly ConcurrentQueue<Trade> _trades = new();
        private readonly object _trimLock = new();

        public void Add(Trade trade)
        {
            _trades.Enqueue(trade);

            // Trim to Capacity. Lock so two concurrent Adds don't both
            // try to dequeue past the limit and race below it.
            lock (_trimLock)
            {
                while (_trades.Count > Capacity && _trades.TryDequeue(out _)) { }
            }
        }

        // Returns the most recent N trades, newest first. Optional stock
        // filter — when set, only trades for that stock are returned.
        public IReadOnlyList<Trade> GetRecent(int limit = 50, string? stock = null)
        {
            var snapshot = _trades.ToArray();           // O(n), but n ≤ 100
            var filtered = string.IsNullOrEmpty(stock)
                ? snapshot
                : snapshot.Where(t =>
                    string.Equals(t.Stock, stock, StringComparison.OrdinalIgnoreCase))
                  .ToArray();

            return filtered
                .Reverse()                              // newest first
                .Take(limit)
                .ToList();
        }

        // Latest trade per stock — used to populate the price tiles on
        // initial page load. Returns a dictionary keyed by stock symbol.
        public IReadOnlyDictionary<string, Trade> GetLatestPerStock()
        {
            return _trades.ToArray()
                .GroupBy(t => t.Stock, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key.ToUpperInvariant(),
                    g => g.OrderByDescending(t => t.ExecutedAt).First());
        }
    }
}