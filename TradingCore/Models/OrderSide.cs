using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace TradingCore.Models
{
    // Represents the side of a trade order — either a BUY or a SELL.
    // Used by both SendOrderApp (when creating an order) and ExchangeApp
    // (when matching orders in the order book).

    // --- MIDDLEWARE ADDITION ---
    // StringEnumConverter tells Newtonsoft.Json to serialise this enum as a
    // human-readable string ("BUY" / "SELL") rather than an integer (0 / 1).
    // This makes the JSON messages published to RabbitMQ easier to read
    // in the RabbitMQ Management UI (http://localhost:15672) and in logs.
    [JsonConverter(typeof(StringEnumConverter))]
    public enum OrderSide
    {
        BUY,
        SELL
    }
}