using Microsoft.EntityFrameworkCore;

namespace TradingCore.Data
{
    public class TradingDbContext : DbContext
    {
        public DbSet<TradeRecord> Trades => Set<TradeRecord>();

        public TradingDbContext(DbContextOptions<TradingDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TradeRecord>(entity =>
            {
                entity.HasKey(t => t.Id);

                entity.Property(t => t.Stock).IsRequired();
                entity.Property(t => t.Buyer).IsRequired();
                entity.Property(t => t.Seller).IsRequired();

                entity.Property(t => t.Quantity).IsRequired();
                entity.Property(t => t.Price).IsRequired();
                entity.Property(t => t.ExecutedAt).IsRequired();
            });
        }
    }
}