# Gold Investment - Game Mechanics Reference

## Product Overview

**Product ID:** `gold_investment`  
**Category:** Commodity  
**Risk Level:** 2/5 (Low-Medium Risk)

---

## The Pitch (Marketing Copy)

### Headline
**The OG Safe Haven. Now Digital.**

### Tagline
*5,000 years of trust. Start from Rp 5,000. Gold never goes out of style.*

### Description
When markets panic, gold shines. It's been humanity's store of value since ancient times, and guess what? Your grandma was right to hoard it.

Now you don't need to buy physical bars or worry about storage. Digital gold lets you own real, BAPPEBTI-regulated gold with 1:1 physical backing. Buy Rp 5,000 worth, sell anytime, or save up and redeem actual gold bars.

Perfect as a portfolio diversifier. Excellent hedge against inflation and currency weakness.

---

## Regulatory Framework

### Governing Bodies
- **Regulator:** BAPPEBTI / OJK
- **Physical Backing:** 1:1 ratio required
- **Storage:** Licensed depository

### Licensed Digital Gold Traders
Treasury, IndoGold, Pluang, LakuEmas, QuantumMetal, Shariacoin

---

## Game Mechanics

### Base Statistics
| Stat | Score (1-5) |
|------|-------------|
| Risk | 2 |
| Liquidity | 4 |
| Complexity | 2 |
| Potential Return | 3 |

### Financial Parameters
| Parameter | Value |
|-----------|-------|
| Min Investment (Digital) | Rp 5,000 |
| Buy/Sell Spread | ~2% |
| Transaction Fee | 0% (digital) |
| Physical Redemption Fee | 2% |
| Purchase Tax (PPh22) | 0.25% |
| Sale Tax (with NPWP) | 1.5% |

---

## Random Events

| Event | Probability | Effect |
|-------|-------------|--------|
| Global Crisis | 3% | +5% to +15% |
| Currency Depreciation | 8% | +2% to +8% (in IDR) |
| Gold Rally | 5% | +3% to +8% |
| Gold Correction | 5% | -3% to -7% |

---

## Scoring System

| Achievement | Points |
|-------------|--------|
| Per gram owned | +5 |
| Per 1% gain | +8 |
| Own 100 grams | +100 |
| First gold purchase | +25 |
| Physical redemption | +50 |

---

## Implementation Notes

```python
def calculate_gold_purchase(amount_idr, price_per_gram, spread=0.02):
    buy_price = price_per_gram * (1 + spread/2)
    grams = amount_idr / buy_price
    return grams

def calculate_gold_sale(grams, price_per_gram, spread=0.02, tax_rate=0.015):
    sell_price = price_per_gram * (1 - spread/2)
    gross = grams * sell_price
    tax = gross * tax_rate
    return gross - tax
```
