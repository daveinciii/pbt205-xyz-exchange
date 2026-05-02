using Microsoft.EntityFrameworkCore;
using TradingCore.Data;
using TradingCore.Models;

namespace TradingCore.Services
{
    public class TradePersistenceService
    {
        private readonly string _databasePath;

        public TradePersistenceService(string databasePath)
        {
            _databasePath = string.IsNullOrWhiteSpace(databasePath)
                ? "trading.db"
                : databasePath;
        }

        private TradingDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<TradingDbContext>()
                .UseSqlite($"Data Source={_databasePath}")
                .Options;

            return new TradingDbContext(options);
        }

        public void EnsureDatabase()
        {
            using var db = CreateContext();
            db.Database.EnsureCreated();
        }

        public List<Trade> GetAllTrades()
        {
            using var db = CreateContext();

            return db.Trades
                .OrderByDescending(t => t.ExecutedAt)
                .Select(t => new Trade
                {
                    Stock = t.Stock,
                    Buyer = t.Buyer,
                    Seller = t.Seller,
                    Quantity = t.Quantity,
                    Price = t.Price,
                    ExecutedAt = t.ExecutedAt
                })
                .ToList();
        }

        public void SaveTrade(Trade trade)
        {
            using var db = CreateContext();

            var record = new TradeRecord
            {
                Stock = trade.Stock,
                Buyer = trade.Buyer,
                Seller = trade.Seller,
                Quantity = trade.Quantity,
                Price = trade.Price,
                ExecutedAt = trade.ExecutedAt
            };

            db.Trades.Add(record);
            db.SaveChanges();
        }
    }
}