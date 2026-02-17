# Equity Crowdfunding - Game Mechanics Reference

## Product Overview

**Product ID:** `equity_crowdfunding`  
**Category:** Capital Market  
**Risk Level:** 4/5 (High Risk)

---

## The Pitch (Marketing Copy)

### Headline
**Be the Early Investor. Before They're Famous.**

### Tagline
*Fund the next unicorn from day one. High risk, potentially life-changing rewards.*

### Description
Remember wishing you'd invested in Gojek or Tokopedia before they blew up? Equity crowdfunding gives you that shot—investing in startups and SMEs before they go mainstream.

You're not buying stocks on the exchange. You're funding real businesses directly, becoming a shareholder in companies still building their future.

**The brutal truth:** Most startups fail. Your money could go to zero. But the ones that succeed? 3x, 5x, even 10x returns are possible if they get acquired or go IPO.

---

## Regulatory Framework

### Governing Bodies
- **Regulator:** OJK
- **Legal Basis:** POJK No. 17/2025

### Investment Limits
| Annual Income | Maximum Investment |
|---------------|-------------------|
| Up to Rp 500 million | 5% of income |
| Above Rp 500 million | 10% of income |
| Experienced investor (2+ years) | No limit |

### ⚠️ Warnings
- **NO guarantee** - Investment can go to zero
- **Limited liquidity** - Hard to sell before exit
- **High failure rate** - Most startups fail

---

## Game Mechanics

### Base Statistics
| Stat | Score (1-5) |
|------|-------------|
| Risk | 4 |
| Liquidity | 1 |
| Complexity | 3 |
| Potential Return | 5 |

### Securities Types
| Type | Features |
|------|----------|
| Equity | Ownership, dividends, voting |
| Debt | Fixed return, maturity date |
| Sukuk | Sharia-compliant, profit sharing |

### Financial Parameters
| Parameter | Value |
|-----------|-------|
| Min Investment | Rp 100,000 |
| Typical Holding Period | 3-5 years |
| Liquidity | Very limited |
| Investor Fee | 0% |

---

## Random Events

| Event | Probability | Outcome |
|-------|-------------|---------|
| Funding Success | 60% | Campaign completes |
| Partial Funding | 25% | Reduced target |
| Funding Fail | 15% | Money returned |
| Business Profit | 30% | 5-20% dividend |
| Business Loss | 40% | No dividend |
| Bankruptcy | 20% | Total loss |
| Acquisition Exit | 5% | 1.5x - 5x return |
| IPO Exit | 2% | 3x - 10x return |

---

## Scoring System

| Achievement | Points |
|-------------|--------|
| Successful investment | +50 |
| Exit event | +200 |
| Bankruptcy | -50 |
| Diversified (5+ companies) | +100 |
| First crowdfunding | +50 |
| Successful exit | +300 |

---

## Implementation Notes

```python
def simulate_startup_outcome():
    import random
    roll = random.random()
    
    if roll < 0.20:
        return "bankruptcy", 0  # Total loss
    elif roll < 0.60:
        return "struggling", 0  # No returns yet
    elif roll < 0.90:
        return "profitable", random.uniform(0.05, 0.15)  # Dividend
    elif roll < 0.97:
        return "acquisition", random.uniform(1.5, 5.0)  # Exit multiplier
    else:
        return "ipo", random.uniform(3.0, 10.0)  # Big exit
```
