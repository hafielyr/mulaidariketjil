# Index Fund (Mutual Fund) - Game Mechanics Reference

## Product Overview

**Product ID:** `index_fund`  
**Category:** Capital Market  
**Risk Level:** 3/5 (Medium Risk)

---

## The Pitch (Marketing Copy)

### Headline
**Own the Entire Market. Not Just One Stock.**

### Tagline
*Why pick one horse when you can bet on the whole race? Professional management, instant diversification, starting from just Rp 10,000.*

### Description
Let's be real—most of us don't have time to research hundreds of stocks. That's where mutual funds come in. You throw your money into a pool, professional managers handle the heavy lifting, and boom—you own a slice of the entire market.

Index funds take it further. They track major indices like LQ45 or IDX30, giving you exposure to Indonesia's biggest companies in one simple purchase. Low fees, broad diversification, and historically solid returns.

**Warning:** This is NOT a bank product. Not covered by LPS. Your money CAN go down. But hey, no risk, no reward, right?

---

## Regulatory Framework

### Governing Bodies
- **Primary Regulator:** OJK (Otoritas Jasa Keuangan)
- **Fund Management:** Licensed Investment Manager (Manajer Investasi)
- **Asset Custody:** Custodian Bank (Bank Kustodian)

### Key Regulations
- UU No. 8 Tahun 1995 tentang Pasar Modal
- POJK tentang Reksa Dana

### Important Disclaimers
⚠️ **NOT guaranteed by LPS**  
⚠️ **NOT a bank deposit**  
⚠️ **Past performance ≠ future results**  
⚠️ **Prices can go up AND down**

---

## Game Mechanics

### Base Statistics
| Stat | Score (1-5) |
|------|-------------|
| Risk | 3 |
| Liquidity | 4 |
| Complexity | 2 |
| Potential Return | 3 |

### Fund Types Available

| Type | Risk | Return Range | Volatility |
|------|------|--------------|------------|
| Money Market | 1 | 3% - 6% | Very Low |
| Fixed Income | 2 | 4% - 10% | Low |
| Balanced | 3 | -10% to +15% | Medium |
| Equity | 4 | -25% to +35% | High |
| Index | 4 | -20% to +30% | High |

### Available Indices
- **IHSG** - IDX Composite (all stocks)
- **LQ45** - 45 most liquid stocks
- **IDX30** - 30 largest market cap
- **IDX80** - 80 stocks with best fundamentals

### Financial Parameters

```json
{
  "min_investment": 10000,
  "min_subsequent": 10000,
  "fees": {
    "subscription": "0% - 2%",
    "redemption": "0% - 2%",
    "management": "0.1% - 2.5% annually"
  },
  "tax_on_gains": "0%",
  "nav_update": "Daily"
}
```

### Settlement Times

| Action | Settlement |
|--------|------------|
| Subscription (Buy) | T+1 |
| Redemption (Sell) | T+7 |
| Switching | T+7 |

---

## NAV Calculation

### Daily NAV Movement
```
Base Movement = Random Normal Distribution (mean=0.0003, std=volatility)
Market Correlation Factor = 0.8
Tracking Error (for index funds) = ±0.2%

New NAV = Previous NAV × (1 + Daily Movement)
```

### Portfolio Value
```
Units Owned = Investment Amount ÷ NAV at Purchase
Current Value = Units Owned × Current NAV
Gain/Loss = Current Value - Total Invested
```

---

## Random Events

### Positive Events
| Event | Probability | Effect |
|-------|-------------|--------|
| Market Rally | 5% | NAV +3% to +8% |
| Dividend Distribution | 15% | Receive 2-5% dividend |

### Negative Events
| Event | Probability | Effect |
|-------|-------------|--------|
| Market Correction | 8% | NAV -2% to -8% |
| Market Crash | 2% | NAV -15% to -30% |
| Fund Manager Change | 3% | Increased volatility |

---

## Scoring System

| Achievement | Points |
|-------------|--------|
| Per 1% positive return | +10 |
| Hold 6 months | +25 |
| Hold 12 months | +50 |
| Hold 24 months | +100 |
| Survive a crash (don't sell during -15% drop) | +100 |
| Panic sell during crash | -50 |

---

## Educational Objectives

This product teaches players:
- ✅ Diversification benefits
- ✅ Professional fund management
- ✅ Market volatility and patience
- ✅ Dollar-cost averaging strategy
- ✅ Difference between banking and investment products
- ✅ Reading NAV and fund performance

---

## Risk Management Tips (Display to Player)

1. **Don't invest emergency funds** - Only invest money you won't need for 3+ years
2. **Don't panic sell** - Markets recover; time in market beats timing the market
3. **Diversify across fund types** - Mix equity with fixed income
4. **Check fund expense ratio** - Lower fees = more returns for you
5. **Read the prospectus** - Understand what you're buying

---

## UI/UX Guidelines

### Visual Theme
- **Primary Color:** #9C27B0 (Purple - Growth)
- **Secondary Color:** #BA68C8
- **Icon:** Line Chart

### Key Information Display
- Real-time NAV
- Performance chart (1M, 3M, YTD, 1Y, 3Y)
- Units owned
- Total invested vs current value
- Gain/Loss percentage
- Fund composition breakdown

---

## Implementation Notes for AI Agents

1. **NAV Simulation:** Generate realistic daily NAV movements based on fund type
2. **Market Correlation:** All equity funds should move somewhat together
3. **Crash Events:** When market crash occurs, affect ALL equity-related funds
4. **T+7 Settlement:** Redemption amount should be based on NAV at T+7, not request date
5. **Dividend Reinvestment:** Option to auto-reinvest dividends

```python
import random
import math

def simulate_daily_nav(previous_nav, fund_type, market_trend=0):
    volatility = {
        "money_market": 0.0005,
        "fixed_income": 0.002,
        "balanced": 0.004,
        "equity": 0.008,
        "index": 0.007
    }
    
    vol = volatility.get(fund_type, 0.005)
    daily_return = random.gauss(0.0003 + market_trend, vol)
    new_nav = previous_nav * (1 + daily_return)
    return round(new_nav, 4)

def calculate_units(investment, nav, subscription_fee=0.01):
    net_investment = investment * (1 - subscription_fee)
    units = net_investment / nav
    return units

def calculate_redemption(units, nav, redemption_fee=0.01):
    gross_value = units * nav
    net_value = gross_value * (1 - redemption_fee)
    return net_value
```

### Sample Fund Performance Simulation

```python
# Simulate 1 year of daily NAV for equity fund
import random

def simulate_year_nav(starting_nav=1000):
    nav_history = [starting_nav]
    
    for day in range(252):  # Trading days in a year
        # Random daily return with slight positive drift
        daily_return = random.gauss(0.0003, 0.015)
        
        # Occasional market events
        event_roll = random.random()
        if event_roll < 0.02:  # 2% chance of significant move
            if random.random() < 0.5:
                daily_return += random.uniform(0.03, 0.08)  # Rally
            else:
                daily_return -= random.uniform(0.05, 0.15)  # Crash
        
        new_nav = nav_history[-1] * (1 + daily_return)
        nav_history.append(round(new_nav, 2))
    
    return nav_history
```
