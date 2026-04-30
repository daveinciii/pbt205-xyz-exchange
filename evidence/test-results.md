# A3 Test Results

## TC-06 — Per-stock isolation (FR-01)

**Setup:** Clean RabbitMQ broker. ExchangeApp started against localhost.

**Steps:**
1. Tia submits XYZ BUY 100 @ $50.00
2. David submits ABC SELL 100 @ $50.00
3. Mark submits XYZ SELL 100 @ $50.00

**Expected:** Steps 1 and 2 sit unmatched in the book despite identical
prices because they apply to different stocks. Step 3 matches against
Tia's XYZ BUY, leaving the ABC SELL untouched.

**Actual:** Pass. ExchangeApp logged TRADE EXECUTED between Tia and Mark
at $50.00 only after Mark's XYZ SELL arrived. The book ended at
0 buys / 1 sells remaining (David's ABC SELL).

**Evidence:** evidence/tc-06-per-stock-isolation.png,
evidence/tc-06-rabbitmq-exchanges.png


## TC-10-VARIANT — GUI graceful startup with broker offline

**Setup:** RabbitMQ container forcibly removed before the GUI was started.
TradingGuiApp launched against `localhost:5672`.

**Steps:**
1. `docker rm -f rabbitmq` (no broker present)
2. `dotnet run --project TradingGuiApp`

**Expected:** Web host comes up, attempts to connect, retries 5 times
with backoff, then logs a clear final error and continues running so the
host stays available. No stack trace, no crash.

**Actual:** Pass. Trading GUI banner displayed, web host listening on
port 5219, StockConfig loaded, RabbitMQ endpoint shown, 5 retry
attempts logged with counter, final "Giving up" message displayed.
Process remained alive at the bash prompt.

**Evidence:** evidence/stage-3-graceful-startup.png

**Note:** This addresses A1 limitation §06.5 (Docker dependency causing
unhandled connection exceptions) and A2 risk register entry "RabbitMQ
not available at GUI startup".


## Stage 3.2 — Live trade broadcasting (FR-05)

**Setup:** RabbitMQ running. ExchangeApp and TradingGuiApp both started.
SendOrderApp invoked with two matching pairs across two stocks.

**Steps:**
1. Tia BUY ABC 100 @ $25 + David SELL ABC 100 @ $25 → ABC trade
2. Eve BUY XYZ 100 @ $60 + Mark SELL XYZ 100 @ $60 → XYZ trade

**Expected:** GUI terminal logs a TRADE BROADCAST box for each completed
trade. Stock symbol on each box correctly reflects the matched pair —
ABC for the first match, XYZ for the second. The full Trade object
(Stock, Buyer, Seller, Quantity, Price, ExecutedAt) is forwarded to all
connected SignalR clients via the 'ReceiveTrade' method.

**Actual:** Pass. Both TRADE BROADCAST boxes printed with the correct
stock symbols. End-to-end pipeline confirmed: SendOrderApp → 'orders'
exchange → ExchangeApp → matched → 'trades' exchange → GUI listener
→ SignalR broadcast.

**Evidence:** evidence/stage-3-trade-broadcast.png