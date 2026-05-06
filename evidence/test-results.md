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


## TC-09 — SQLite persistence across ExchangeApp restart (FR-06, TR-04)

**Setup:** Clean RabbitMQ broker via Docker. ExchangeApp, TradingGuiApp,
and browser dashboard all running. trading.db SQLite file present from
prior development sessions.

**Steps:**
1. Submit three matching pairs via SendOrderApp:
   - Tia BUY XYZ 100 @ $50.00 + David SELL XYZ 100 @ $50.00
   - Eve BUY ABC 100 @ $25.00 + Mark SELL ABC 100 @ $25.00
   - Tia BUY XYZ 100 @ $60.00 + David SELL XYZ 100 @ $60.00
2. Confirm dashboard shows all three trades and the XYZ tile reflects
   the latest $60.00 price with change indicator
3. Press Ctrl+C in the ExchangeApp terminal to shut it down cleanly
4. Refresh the dashboard browser tab while ExchangeApp is offline
5. Restart ExchangeApp via `dotnet run --project ExchangeApp -- localhost`
6. Submit one further matching pair to confirm post-restart writes also
   persist:
   - Sara BUY DEF 100 @ $100.00 + Liam SELL DEF 100 @ $100.00

**Expected:**
- After step 4 (broker offline, dashboard refreshed): the three
  pre-restart trades remain visible because the dashboard reads from
  the persistent SQLite store via the hub, not from any in-memory cache.
- After step 5 (ExchangeApp restart): the DATABASE startup box reports a
  non-zero count of historical trades loaded from `trading.db`, proving
  ExchangeApp re-reads the DB on cold start.
- After step 6: the new DEF trade appends to the same persistent
  history, visible alongside the pre-restart entries in one continuous
  dashboard view.

**Actual:** Pass on all six steps.
- ExchangeApp shutdown was clean ("XYZ Exchange closing connection..."
  box visible).
- Dashboard refreshed while ExchangeApp was offline rendered XYZ at
  $60.00 with the +20.00% change indicator, ABC at $25.00, and seven
  rows in the recent-trades list — proving the hub returned data from
  SQLite without any live broadcast source connected.
- ExchangeApp restart logged: "DATABASE — SQLite path: trading.db /
  Historical trades loaded: 8" confirming the persistent ledger held
  trades from this session and prior development sessions.
- Post-restart DEF trade matched cleanly (TRADE EXECUTED Sara ↔ Liam at
  $100.00) and appeared at the top of the dashboard's recent-trades
  list alongside all earlier entries.

**Evidence:**
- evidence/tc-09-before-restart.png — SendOrderApp terminal showing the
  three pre-restart matching pairs being submitted
- evidence/tc-09-after-restart.png — dashboard rendering the
  pre-restart trades while ExchangeApp is offline (proves the hub is
  DB-backed, not relying on the in-memory ring buffer)
- evidence/tc-09-restart-loaded.png — ExchangeApp startup output
  immediately following the restart, showing the DATABASE box with
  "Historical trades loaded: 8"
- evidence/tc-09-post-restart-trade.png — dashboard after the
  post-restart DEF trade, showing pre- and post-restart trades in one
  continuous history

**Note:** This test simultaneously validates FR-06 (persistent trade
history), TR-04 (SQLite via EF Core), and the DB-backed wiring of the
TradeHub history methods (Stage 3.1). It also incidentally demonstrates
that trades persist across multiple development sessions — three of the
loaded trades originated from sessions prior to this test run, evidence
of true on-disk persistence rather than process-lifetime caching.


## Stage 6.1 — Build, test, and orchestration verification (TR-06)

**Setup:** Clean project checkout. No containers running. No prior build
artifacts. All commands run from the project root.

### 6.1a — dotnet build

**Expected:** All five projects compile against .NET 10.0 with 0 errors
and 0 warnings.

**Actual:** Pass. Build succeeded in 6.4s. All five projects compiled
cleanly:
- TradingCore (3.1s)
- SendOrderApp (0.5s)
- ExchangeApp (0.5s)
- TradingCore.Tests (1.2s)
- TradingGuiApp (2.0s)

0 errors, 0 warnings.

**Evidence:** evidence/stage-6-build-clean.png

### 6.1b — dotnet test

**Expected:** All unit tests pass with 0 failures and 0 skipped.

**Actual:** Pass. Test summary: total 6, failed 0, succeeded 6,
skipped 0, duration 1.0s. xUnit v3.1.4 via TradingCore.Tests against
.NET 10.0. Build succeeded in 1.9s.

**Evidence:** evidence/stage-6-tests-passing.png

### 6.1c — docker compose up (clean state)

**Expected:** All three containers start in dependency order and reach
their ready states without manual intervention.

**Actual:** Pass. Sequence observed:
1. rabbitmq reached "Server startup complete; 5 plugins started" with
   TCP listener on 5672 and Management UI on 15672 (4859 ms startup).
   Container reported Healthy twice before dependent services started.
2. exchangeapp started after rabbitmq healthy: loaded 3 stocks
   (XYZ, ABC, DEF), connected to rabbitmq:5672, subscribed to 'orders'
   topic, DATABASE box showed "SQLite path: trading.db / Historical
   trades loaded: 0" (clean cold start), reached EXCHANGE READY state.
3. tradinggui started: loaded 3 stocks, connected to rabbitmq:5672,
   subscribed to 'trades' topic, web host listening on
   http://[::]:8080, reached GUI READY state.

**Evidence:** evidence/tr-06-docker-compose-up.png

### 6.1d — End-to-end smoke test through orchestrated stack

**Setup:** Full docker compose stack running. SendOrderApp invoked from
host against localhost.

**Steps:**
1. `dotnet run --project SendOrderApp -- Tia localhost XYZ BUY 100 50.00`
2. `dotnet run --project SendOrderApp -- David localhost XYZ SELL 100 50.00`
3. Browser opened at http://localhost:5219

**Expected:** Both orders publish to the 'orders' exchange, ExchangeApp
matches them, publishes the completed trade to 'trades', and the
dashboard reflects the result live without page refresh.

**Actual:** Pass. Both SendOrderApp invocations logged ORDER CREATED,
RABBITMQ Connected, Published to 'orders' topic, and SUBMITTED boxes
with correct parameters (Tia BUY 100 XYZ @ $50.00 at 03:46:14 UTC;
David SELL 100 XYZ @ $50.00 at 03:46:16 UTC). Dashboard updated live:
XYZ price tile showed $50.00 at 01:46:16 PM; recent trades list showed
Tia ↔ David, XYZ, 100, $50.00. ABC and DEF tiles correctly remained in
empty state. SignalR connection pill showed Connected throughout.

**Evidence:** evidence/tr-06-docker-compose-trade.png