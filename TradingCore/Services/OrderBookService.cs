using TradingCore.Models;

namespace TradingCore.Services
{
    public class OrderBookService
    {
        private readonly List<Order> _buyOrders = new();
        private readonly List<Order> _sellOrders = new();

        public Trade? ProcessOrder(Order incomingOrder)
        {
            if (incomingOrder.Side == OrderSide.BUY)
            {
                return MatchBuyOrder(incomingOrder);
            }

            return MatchSellOrder(incomingOrder);
        }

        private Trade? MatchBuyOrder(Order buyOrder)
        {
            var matchingSell = _sellOrders
                .Where(s => s.Stock == buyOrder.Stock && s.Price <= buyOrder.Price)
                .OrderBy(s => s.Price)
                .ThenBy(s => s.CreatedAt)
                .FirstOrDefault();

            if (matchingSell == null)
            {
                _buyOrders.Add(buyOrder);
                return null;
            }

            _sellOrders.Remove(matchingSell);

            return new Trade
            {
                Stock = buyOrder.Stock,
                Buyer = buyOrder.Username,
                Seller = matchingSell.Username,
                Quantity = 100,
                Price = matchingSell.Price,
                ExecutedAt = DateTime.UtcNow
            };
        }

        private Trade? MatchSellOrder(Order sellOrder)
        {
            var matchingBuy = _buyOrders
                .Where(b => b.Stock == sellOrder.Stock && b.Price >= sellOrder.Price)
                .OrderByDescending(b => b.Price)
                .ThenBy(b => b.CreatedAt)
                .FirstOrDefault();

            if (matchingBuy == null)
            {
                _sellOrders.Add(sellOrder);
                return null;
            }

            _buyOrders.Remove(matchingBuy);

            return new Trade
            {
                Stock = sellOrder.Stock,
                Buyer = matchingBuy.Username,
                Seller = sellOrder.Username,
                Quantity = 100,
                Price = matchingBuy.Price,
                ExecutedAt = DateTime.UtcNow
            };
        }

        public IReadOnlyList<Order> GetBuyOrders() => _buyOrders.AsReadOnly();
        public IReadOnlyList<Order> GetSellOrders() => _sellOrders.AsReadOnly();
    }
}