# Certificate of Deposit (Time Deposit) - Game Mechanics Reference

## Product Overview

**Product ID:** `certificate_of_deposit`  
**Category:** Banking  
**Risk Level:** 1/5 (Very Low Risk)

---

## The Pitch (Marketing Copy)

### Headline
**Lock It. Grow It. Forget It.**

### Tagline
*Higher interest than savings, same government protection. Your patience pays off—literally.*

### Description
Here's the deal: you promise to keep your money untouched for a set period, and the bank rewards you with better interest rates. It's like a loyalty program for your cash.

Deposito isn't about getting rich quick. It's about making your idle money work harder while you sleep. The longer you commit, the sweeter the returns. And yes, LPS still has your back up to Rp 2 billion.

Perfect for that car down payment fund. Ideal for your wedding savings. Basically, any money you don't need for the next few months.

---

## Regulatory Framework

### Governing Bodies
- **Primary Regulator:** OJK (Otoritas Jasa Keuangan)
- **Deposit Guarantee:** LPS (Lembaga Penjamin Simpanan)

### LPS Guarantee
| Parameter | Value |
|-----------|-------|
| Maximum Coverage | Rp 2,000,000,000 per customer per bank |
| Requirements | 3T Rule applies |
| Interest Rate Limit | Must not exceed LPS maximum rate |

---

## Game Mechanics

### Base Statistics
| Stat | Score (1-5) |
|------|-------------|
| Risk | 1 |
| Liquidity | 2 |
| Complexity | 2 |
| Potential Return | 2 |

### Tenor Options & Interest Rates

| Tenor | Min Rate | Max Rate | Typical |
|-------|----------|----------|---------|
| 1 Month | 2.5% | 4.0% | 3.0% |
| 3 Months | 3.0% | 4.5% | 3.5% |
| 6 Months | 3.5% | 5.0% | 4.0% |
| 12 Months | 4.0% | 5.5% | 4.5% |
| 24 Months | 4.5% | 6.0% | 5.0% |

### Financial Parameters

```json
{
  "min_deposit": 1000000,
  "typical_min_deposit": 8000000,
  "tax_on_interest": "20%",
  "early_withdrawal_penalty": {
    "forfeit_interest": true,
    "additional_penalty": "1% of principal"
  }
}
```

### Rollover Options

| Type | Name | Interest Handling |
|------|------|-------------------|
| ARO | Auto Roll Over | Interest transferred to savings account |
| ARO + Interest | Auto Roll Over with Interest | Interest compounds with principal |
| Non-ARO | No Auto Roll Over | All funds returned at maturity |

### Available Actions

| Action | Requirements | Penalty |
|--------|-------------|---------|
| Open Deposit | Min Rp 1,000,000 | None |
| Withdraw at Maturity | Wait until maturity date | None |
| Early Withdrawal | Notify 3 days before | Forfeit interest + 1% penalty |
| Change Rollover | Before maturity | None |

---

## Interest Calculation

### At Maturity
```
Gross Interest = Principal × (Annual Rate × Tenor/12)
Tax = Gross Interest × 20%
Net Interest = Gross Interest - Tax
Total Payout = Principal + Net Interest
```

### Early Withdrawal Scenario
```
Penalty = Principal × 1%
Interest Received = 0 (forfeited)
Payout = Principal - Penalty
```

---

## Random Events

### Positive Events
| Event | Probability | Effect |
|-------|-------------|--------|
| Promotional Rate | 8% | +1% bonus rate for new deposits |
| BI Rate Hike | 10% | Market rates increase |

### Negative Events
| Event | Probability | Effect |
|-------|-------------|--------|
| BI Rate Cut | 10% | Market rates decrease |
| Bank Failure | 0.1% | LPS guarantee activated |

### Maturity Event
At maturity, player must choose:
1. **Withdraw All** - Receive principal + interest
2. **Roll Over (Principal)** - Interest to savings, principal continues
3. **Roll Over (All)** - Compound everything for next term
4. **Partial Withdraw** - Take some, roll over the rest

---

## Scoring System

| Achievement | Points |
|-------------|--------|
| First deposit matured | +50 |
| Per Rp 10,000 interest earned | +1 |
| Hold to maturity | +50 |
| Choose longer tenor (12M) | +25 |
| Choose longer tenor (24M) | +50 |
| Early withdrawal | -30 |

---

## Educational Objectives

This product teaches players:
- ✅ Time value of money
- ✅ Trade-off between liquidity and returns
- ✅ Importance of financial planning
- ✅ Compound interest concept
- ✅ Opportunity cost of locked funds

---

## UI/UX Guidelines

### Visual Theme
- **Primary Color:** #2196F3 (Blue - Trust)
- **Secondary Color:** #64B5F6
- **Icon:** Certificate/Document

### Key Information Display
- Maturity countdown timer
- Current vs projected interest
- Rollover option selected
- LPS coverage indicator

---

## Implementation Notes for AI Agents

1. **Maturity Tracking:** Create calendar event for maturity date
2. **Auto-Rollover:** If ARO selected and no player action, automatically roll over
3. **Rate Lock:** Interest rate is locked at deposit creation, not affected by market changes
4. **Penalty Calculation:** For early withdrawal, calculate exact penalty
5. **Multiple Deposits:** Player can have multiple deposits with different maturities

```python
# Sample maturity calculation
def calculate_deposit_maturity(principal, annual_rate, tenor_months, tax_rate=0.20):
    gross_interest = principal * (annual_rate * tenor_months / 12)
    net_interest = gross_interest * (1 - tax_rate)
    total_payout = principal + net_interest
    return {
        "gross_interest": gross_interest,
        "tax_deducted": gross_interest * tax_rate,
        "net_interest": net_interest,
        "total_payout": total_payout
    }
```

### State Machine for Deposit Lifecycle

```
[CREATED] → [ACTIVE] → [MATURING] → [MATURED]
                ↓                        ↓
        [EARLY_WITHDRAWN]         [ROLLED_OVER] → [ACTIVE]
                                        ↓
                                  [WITHDRAWN]
```
