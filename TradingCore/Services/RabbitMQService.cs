using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Newtonsoft.Json;
using System.Text;
// Note: System.Security.Cryptography.X509Certificates was removed — not required here

namespace TradingCore.Services
{
    // --- MIDDLEWARE ADDITION ---
    // Wraps RabbitMQ connection logic for publishing and subscribing.
    // Each "topic" in the assignment maps to a RabbitMQ fanout exchange —
    // fanout broadcasts every message to ALL subscribers.
    public class RabbitMQService:IDisposable
    {
        // Holds the TCP connection to the RabbitMQ broker
        private readonly IConnection _connection;

        // IChannel is the v7 replacement for IModel — all publish/subscribe operations run through here
        private readonly IChannel _channel;

        // Topic names matching the assignment spec
        public const string ORDERS_TOPIC = "orders";
        public const string TRADES_TOPIC = "trades";
        
        public RabbitMQService(string host = "localhost", int port = 5672)
        {
            var factory = new ConnectionFactory
            {
                HostName = host,
                Port = port 
            };
            // --- MIDDLEWARE ADDITION ---
            // RabbitMQ.Client v7 uses async API — .GetAwaiter().GetResult() blocks
            // the thread so the constructor stays synchronous
            _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
            _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

            // --- MIDDLEWARE ADDITION ---
            // Declare both exchanges upfront — fanout type broadcasts to ALL subscribers.
            // durable: true means exchanges survive a RabbitMQ broker restart.
            // Safe to call multiple times — will not overwrite an existing exchange.
            _channel.ExchangeDeclareAsync(ORDERS_TOPIC, ExchangeType.Fanout, durable: true).GetAwaiter().GetResult();
            _channel.ExchangeDeclareAsync(TRADES_TOPIC, ExchangeType.Fanout, durable: true).GetAwaiter().GetResult();
        
            Console.WriteLine($"┌─ RABBITMQ ───────────────────────────────┐");
            Console.WriteLine($"  Connected to {host}:{port}");
            Console.WriteLine($"└──────────────────────────────────────────┘");
        }

        // --- MIDDLEWARE ADDITION ---
        // Serialises an object to JSON and publishes it to the given exchange.
        public void Publish<T>(string exchange, T message)
        {
            // --- MIDDLEWARE ADDITION ---
            // Serialise the object to JSON — StringEnumConverter ensures enums
            // appear as "BUY"/"SELL" rather than integers in the message payload
            var json = JsonConvert.SerializeObject(message);
            var body = Encoding.UTF8.GetBytes(json);

            // --- MIDDLEWARE ADDITION ---
            // BasicProperties v7 replaces CreateBasicProperties() from v6.
            // Persistent: true — message survives a RabbitMQ broker restart.
            var props = new BasicProperties
            {
                Persistent = true,
                ContentType = "application/json"
            };

            // --- MIDDLEWARE ADDITION ---
            // routingKey is empty — fanout exchanges ignore routing keys and
            // broadcast to every bound queue automatically
            _channel.BasicPublishAsync(
                exchange: exchange, 
                routingKey: string.Empty, 
                mandatory: false,
                basicProperties: props, 
                body: body
            ).GetAwaiter().GetResult();

            Console.WriteLine($"┌─ RABBITMQ ───────────────────────────────┐");
            Console.WriteLine($"│ Published to '{exchange}' topic.           │");
            Console.WriteLine($"└──────────────────────────────────────────┘");
        }

        // --- MIDDLEWARE ADDITION
        /// Subscribes to an exchange. Creates a unique temporary queue for this
        /// subscriber so every subscriber gets its own copy of each message.
        public void Subscribe<T>(string exchange, Action<T> onMessage)
        {
            // --- MIDDLEWARE ADDITION ---
            // Declare a temporary exclusive queue — RabbitMQ generates a unique name.
            // exclusive: true — only this connection can use it.
            // autoDelete: true — queue is deleted when this connection closes.
            var queueName = _channel.QueueDeclareAsync(
                queue: string.Empty, 
                durable: false, 
                exclusive: true, 
                autoDelete: true 
            ).GetAwaiter().GetResult().QueueName;
            
            // Bind the queue to the fanout exchange — routingKey ignored by fanout
            _channel.QueueBindAsync(queueName, exchange, routingKey: string.Empty).GetAwaiter().GetResult();

            // --- MIDDLEWARE ADDITION ---
            // AsyncEventingBasicConsumer replaces EventingBasicConsumer from v6
            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (_, ea) =>
            {
                try
                {
                    // Deserialise the raw bytes back into the expected type T
                    var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                    var obj = JsonConvert.DeserializeObject<T>(json);
                    if (obj != null)
                    {
                        onMessage(obj);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"┌─ RABBITMQ ERROR ─────────────────────────┐");
                    Console.WriteLine($"  Deserialisation error: {ex.Message,-19}");
                    Console.WriteLine($"└──────────────────────────────────────────┘");
                }
            };

            // autoAck: true — messages are acknowledged automatically on receipt
            _channel.BasicConsumeAsync(queueName, autoAck: true, consumer:consumer).GetAwaiter().GetResult();
            Console.WriteLine($"┌─ RABBITMQ ───────────────────────────────┐");
            Console.WriteLine($"|  Subscribed to '{exchange}' topic.          |");    
            Console.WriteLine($"└──────────────────────────────────────────┘");
        }

         // Clean up channel and connection when the using block ends
        public void Dispose()
        {
            _channel?.CloseAsync().GetAwaiter().GetResult();
            _connection?.CloseAsync().GetAwaiter().GetResult();
        }
    }
}