# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY TradingCore/*.csproj ./TradingCore/
COPY SendOrderApp/*.csproj ./SendOrderApp/
COPY ExchangeApp/*.csproj ./ExchangeApp/
COPY TradingGuiApp/*.csproj ./TradingGuiApp/

RUN dotnet restore ./ExchangeApp/ExchangeApp.csproj
RUN dotnet restore ./TradingGuiApp/TradingGuiApp.csproj

COPY . .

RUN dotnet publish ./ExchangeApp/ExchangeApp.csproj -c Release -o /app/exchange
RUN dotnet publish ./TradingGuiApp/TradingGuiApp.csproj -c Release -o /app/gui

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/exchange ./exchange
COPY --from=build /app/gui ./gui
COPY tradingsystem.config.json ./tradingsystem.config.json

WORKDIR /app