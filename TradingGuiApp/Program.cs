using TradingGuiApp.Hubs;
using TradingGuiApp.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();
builder.Services.AddHostedService<TradeListenerService>();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapHub<TradeHub>("/tradehub");

app.Run();