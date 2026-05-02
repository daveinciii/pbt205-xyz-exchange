using TradingCore.Models;
using TradingCore.Services;

namespace TradingCore.Tests
{
    public class OrderBookServiceTests
    {
        [Fact]
        public void BuyOrder_WithNoMatchingSell_IsAddedToBuyBook()
        {
            var orderBook = new OrderBookService();

            var order = new Order
            {
                Username = "Alice",
                Stock = "XYZ",
                Side = OrderSide.BUY,
                Quantity = 100,
                Price = 15.00,
                CreatedAt = DateTime.UtcNow
            };

            var trade = orderBook.ProcessOrder(order);

            Assert.Null(trade);
            Assert.Single(orderBook.GetBuyOrders());
            Assert.Empty(orderBook.GetSellOrders());
        }

        [Fact]
        public void SellOrder_WithNoMatchingBuy_IsAddedToSellBook()
        {
            var orderBook = new OrderBookService();

            var order = new Order
            {
                Username = "Bob",
                Stock = "XYZ",
                Side = OrderSide.SELL,
                Quantity = 100,
                Price = 15.00,
                CreatedAt = DateTime.UtcNow
            };

            var trade = orderBook.ProcessOrder(order);

            Assert.Null(trade);
            Assert.Single(orderBook.GetSellOrders());
            Assert.Empty(orderBook.GetBuyOrders());
        }

        [Fact]
        public void BuyOrder_WhenPriceIsGreaterThanOrEqualToSellPrice_ExecutesTradeAtSellPrice()
        {
            var orderBook = new OrderBookService();

            var sellOrder = new Order
            {
                Username = "Bob",
                Stock = "XYZ",
                Side = OrderSide.SELL,
                Quantity = 100,
                Price = 15.00,
                CreatedAt = DateTime.UtcNow
            };

            var buyOrder = new Order
            {
                Username = "Alice",
                Stock = "XYZ",
                Side = OrderSide.BUY,
                Quantity = 100,
                Price = 16.00,
                CreatedAt = DateTime.UtcNow
            };

            orderBook.ProcessOrder(sellOrder);
            var trade = orderBook.ProcessOrder(buyOrder);

            Assert.NotNull(trade);
            Assert.Equal("XYZ", trade.Stock);
            Assert.Equal("Alice", trade.Buyer);
            Assert.Equal("Bob", trade.Seller);
            Assert.Equal(100, trade.Quantity);
            Assert.Equal(15.00, trade.Price);
            Assert.Empty(orderBook.GetBuyOrders());
            Assert.Empty(orderBook.GetSellOrders());
        }

        [Fact]
        public void SellOrder_WhenPriceIsAcceptableToExistingBuy_ExecutesTrade()
        {
            var orderBook = new OrderBookService();

            var buyOrder = new Order
            {
                Username = "Alice",
                Stock = "XYZ",
                Side = OrderSide.BUY,
                Quantity = 100,
                Price = 16.00,
                CreatedAt = DateTime.UtcNow
            };

            var sellOrder = new Order
            {
                Username = "Bob",
                Stock = "XYZ",
                Side = OrderSide.SELL,
                Quantity = 100,
                Price = 15.00,
                CreatedAt = DateTime.UtcNow
            };

            orderBook.ProcessOrder(buyOrder);
            var trade = orderBook.ProcessOrder(sellOrder);

            Assert.NotNull(trade);
            Assert.Equal("XYZ", trade.Stock);
            Assert.Equal("Alice", trade.Buyer);
            Assert.Equal("Bob", trade.Seller);
            Assert.Equal(100, trade.Quantity);
            Assert.Equal(16.00, trade.Price);
            Assert.Empty(orderBook.GetBuyOrders());
            Assert.Empty(orderBook.GetSellOrders());
        }

        [Fact]
        public void OrdersForDifferentStocks_DoNotMatch()
        {
            var orderBook = new OrderBookService();

            var xyzBuy = new Order
            {
                Username = "Alice",
                Stock = "XYZ",
                Side = OrderSide.BUY,
                Quantity = 100,
                Price = 20.00,
                CreatedAt = DateTime.UtcNow
            };

            var abcSell = new Order
            {
                Username = "Bob",
                Stock = "ABC",
                Side = OrderSide.SELL,
                Quantity = 100,
                Price = 10.00,
                CreatedAt = DateTime.UtcNow
            };

            orderBook.ProcessOrder(xyzBuy);
            var trade = orderBook.ProcessOrder(abcSell);

            Assert.Null(trade);
            Assert.Single(orderBook.GetBuyOrders());
            Assert.Single(orderBook.GetSellOrders());
        }

        [Fact]
        public void MatchedOrders_AreRemovedFromOrderBook()
        {
            var orderBook = new OrderBookService();

            orderBook.ProcessOrder(new Order
            {
                Username = "Alice",
                Stock = "XYZ",
                Side = OrderSide.BUY,
                Quantity = 100,
                Price = 15.00,
                CreatedAt = DateTime.UtcNow
            });

            var trade = orderBook.ProcessOrder(new Order
            {
                Username = "Bob",
                Stock = "XYZ",
                Side = OrderSide.SELL,
                Quantity = 100,
                Price = 15.00,
                CreatedAt = DateTime.UtcNow
            });

            Assert.NotNull(trade);
            Assert.Empty(orderBook.GetBuyOrders());
            Assert.Empty(orderBook.GetSellOrders());
        }
    }
}