# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

COPY TradingCore/*.csproj ./TradingCore/
COPY SendOrderApp/*.csproj ./SendOrderApp/
COPY ExchangeApp/*.csproj ./ExchangeApp/

RUN dotnet restore ./ExchangeApp/ExchangeApp.csproj

COPY . .
RUN dotnet publish ./ExchangeApp/ExchangeApp.csproj -c Release -o /app/out

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=build /app/out .

ENTRYPOINT ["dotnet", "ExchangeApp.dll"]