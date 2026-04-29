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