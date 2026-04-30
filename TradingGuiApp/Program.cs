using TradingGuiApp.Hubs;
using TradingGuiApp.Services;

var builder = WebApplication.CreateBuilder(args);

// Disable JSON property naming policy so SignalR sends Trade fields with
// the same PascalCase casing as the C# model (Stock, Price, ExecutedAt
// etc.). Default behaviour camelCases them, which breaks the dashboard
// JavaScript that reads trade.Price / trade.Stock directly.
builder.Services.AddSignalR()
    .AddJsonProtocol(options =>
    {
        options.PayloadSerializerOptions.PropertyNamingPolicy = null;
    });

// TradeHistory is a singleton — both the hub (per-request) and the
// listener (host-lifetime) need to read/write the same buffer.
builder.Services.AddSingleton<TradeHistory>();

builder.Services.AddHostedService<TradeListenerService>();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapHub<TradeHub>("/tradehub");

app.Run();