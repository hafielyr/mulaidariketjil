# Individual Stocks - Game Mechanics Reference

## Product Overview

**Product ID:** `individual_stocks`  
**Category:** Capital Market  
**Risk Level:** 5/5 (High Risk)

---

## The Pitch (Marketing Copy)

### Headline
**Own a Piece of Indonesia's Giants.**

### Tagline
*From BCA to Telkom—become a shareholder in companies that power the nation.*

### Description
When you buy stocks, you become a part-owner of actual companies. Their wins are your wins. Their losses... well, you get the picture.

Stocks can be volatile. But historically, the stock market has outperformed almost every other asset class over the long term.

**The catch:** Your money CAN go down. A lot. This is not for the faint-hearted or money you need next month.

---

## Regulatory Framework

### Governing Bodies
- **Exchange:** IDX (Indonesia Stock Exchange)
- **Regulator:** OJK (Otoritas Jasa Keuangan)
- **Investor Protection:** SIPF (Securities Investor Protection Fund)

### SIPF Coverage
| Parameter | Value |
|-----------|-------|
| Coverage | Securities protection (NOT value) |
| Maximum | Rp 100,000,000 |
| Protects Against | Broker failure |
| Does NOT Protect | Market losses |

---

## Game Mechanics

### Base Statistics
| Stat | Score (1-5) |
|------|-------------|
| Risk | 5 |
| Liquidity | 4 |
| Complexity | 4 |
| Potential Return | 5 |

### Trading Rules

| Parameter | Value |
|-----------|-------|
| Lot Size | 100 shares |
| Minimum Price | Rp 50 |
| Auto-Rejection Limit | ±35% daily |
| Settlement | T+2 |

### Trading Hours (WIB)
- Session 1: 09:00 - 12:00
- Session 2: 13:30 - 16:00

### Fees and Taxes
| Type | Rate |
|------|------|
| Buy Fee | 0.10% - 0.25% |
| Sell Fee | 0.15% - 0.35% |
| Sell Tax | 0.1% |
| Dividend Tax | 10% |

---

## Random Events

| Event | Probability | Effect |
|-------|-------------|--------|
| Strong Earnings | 15% | +5% to +15% |
| Weak Earnings | 15% | -5% to -15% |
| Dividend Announcement | 12% | 2-8% yield |
| Stock Split | 3% | More shares, lower price |
| Market Crash | 2% | -10% to -20% market-wide |

---

## Scoring System

| Achievement | Points |
|-------------|--------|
| Per 1% gain | +10 |
| Per 1% loss | -5 |
| Dividend received | +25 |
| Hold 1 year | +50 |
| First dividend | +50 |

---

## Sample Stocks

| Code | Name | Sector |
|------|------|--------|
| BBCA | Bank Central Asia | Financials |
| BBRI | Bank Rakyat Indonesia | Financials |
| TLKM | Telkom Indonesia | Communications |
| ASII | Astra International | Consumer |
| UNVR | Unilever Indonesia | Consumer Goods |

---

## Implementation Notes

```python
def calculate_buy_total(price, lots, fee_rate=0.0015):
    shares = lots * 100
    gross = price * shares
    fee = gross * fee_rate
    return gross + fee

def calculate_sell_proceeds(price, lots, fee_rate=0.0025, tax_rate=0.001):
    shares = lots * 100
    gross = price * shares
    fee = gross * fee_rate
    tax = gross * tax_rate
    return gross - fee - tax
```
