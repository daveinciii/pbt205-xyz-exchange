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

## Stage 3.2 — SignalR end-to-end delivery (FR-05)

**Setup:** RabbitMQ, ExchangeApp, and TradingGuiApp all running.
Browser opened at http://localhost:5219.

**Steps:**
1. Tia BUY XYZ 100 @ $99.00
2. David SELL XYZ 100 @ $99.00 (matches step 1)

**Expected:** Within ~1 second of the matching SELL, the browser
page updates without refresh: the latest-trade panel shows the new
price, buyer, seller, quantity, and time. A row appears in the
recent-trades list. The "Live updates connected" status indicator
remains visible.

**Actual:** Pass. Page updated to show $99.00 with Tia / David /
100 / 3:46:29 PM. Recent-trades list received "Tia bought from
David | XYZ | 100 @ $99.00". No page refresh required.

**Note:** The single-stock A1 frontend correctly receives and
displays the Stock field from the broadcast, but the page header
still reads "XYZ Corp - Latest Trade" hardcoded. The multi-stock
dashboard rewrite in Stage 4 replaces this layout entirely.

**Evidence:** evidence/stage-3-browser-signalr.png

## Stage 4 — Multi-stock dashboard (FR-01, FR-05, FR-07)

**Setup:** RabbitMQ, ExchangeApp, and TradingGuiApp all running. Dashboard
opened at http://localhost:5219. Three matching pairs submitted via
SendOrderApp across two stocks.

**Steps:**
1. Initial page load with no trades — dashboard renders empty state
2. Tia BUY XYZ @ $50 / David SELL XYZ @ $50 (matches, first XYZ trade)
3. Eve BUY ABC @ $25 / Mark SELL ABC @ $25 (matches, first ABC trade)
4. Tia BUY XYZ @ $60 / David SELL XYZ @ $60 (matches, second XYZ trade)
5. Click XYZ filter pill in Recent Trades section

**Expected:**
- Empty state shows three "Awaiting first trade" tiles and an empty
  history with All/XYZ/ABC/DEF filter pills
- After step 2: XYZ tile populates with $50.00 and the trade appears
  in history
- After step 3: ABC tile populates with $25.00, history shows both
  trades newest first
- After step 4: XYZ tile updates to $60.00, change indicator appears
  showing ↑ 20.00% green; history shows three trades
- After step 5: Recent trades list shows only the two XYZ entries

**Actual:** Pass on all five steps. Connection pill remained green
throughout (SignalR stayed connected). DEF tile correctly remained in
empty state because no DEF trades fired. Change indicator computed
correctly from prior in-memory state.

**Evidence:**
- evidence/stage-4-dashboard-empty.png — initial render with no trades
- evidence/stage-4-dashboard-populated.png — three trades visible with
  XYZ change indicator
- evidence/stage-4-history-filter.png — XYZ filter applied, only XYZ
  rows visible