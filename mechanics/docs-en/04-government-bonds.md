# Government Bonds (SBN Retail) - Game Mechanics Reference

## Product Overview

**Product ID:** `government_bonds`  
**Category:** Capital Market  
**Risk Level:** 1/5 (Very Low Risk)

---

## The Pitch (Marketing Copy)

### Headline
**Lend to the Nation. Get Paid Monthly.**

### Tagline
*100% government-guaranteed returns. Help build Indonesia while growing your wealth. Patriotism that pays.*

### Description
Here's a wild concept: the Indonesian government borrows money from YOU, and pays you interest every single month. That's SBN Retail in a nutshell.

Unlike bank deposits, your money isn't sitting in a vault—it's building roads, schools, and hospitals. And unlike stocks, you know exactly what you'll get back. The government literally guarantees it by law.

Whether you want fixed returns (ORI, SR) or flexibility to benefit from rate changes (SBR, ST), there's an SBN for everyone. Plus, Sharia-compliant options exist for those who want halal investments.

**Bottom line:** It's as safe as it gets, the returns beat deposits, and you're literally funding your country's future.

---

## Regulatory Framework

### Governing Bodies
- **Issuer:** Kementerian Keuangan Republik Indonesia
- **Regulator:** OJK / DJPPR

### Legal Basis
- UU No. 24 Tahun 2002 tentang Surat Utang Negara
- UU No. 15 Tahun 2017 tentang SBSN

### Government Guarantee
| Parameter | Details |
|-----------|---------|
| Guarantee Level | 100% Principal + Coupon |
| Backed By | State Budget (APBN) |
| Default Risk | Virtually zero (sovereign guarantee) |

---

## Product Variants

### Conventional SBN

| Product | Type | Tradeable | Coupon | Tenor |
|---------|------|-----------|--------|-------|
| **ORI** | Fixed Rate | ✅ Yes | Fixed | 3 or 6 years |
| **SBR** | Floating Rate | ❌ No | Floating with Floor | 2 or 4 years |

### Sharia SBN (Sukuk)

| Product | Type | Tradeable | Coupon | Tenor |
|---------|------|-----------|--------|-------|
| **SR** | Fixed Rate | ✅ Yes | Fixed | 3 or 5 years |
| **ST** | Floating Rate | ❌ No | Floating with Floor | 2 or 4 years |

---

## Game Mechanics

### Base Statistics
| Stat | Score (1-5) |
|------|-------------|
| Risk | 1 |
| Liquidity | 3 |
| Complexity | 2 |
| Potential Return | 2 |

### Financial Parameters

```json
{
  "min_investment": 1000000,
  "max_investment": 5000000000,
  "investment_multiples": 1000000,
  "coupon_payment": "monthly",
  "coupon_tax": "10%",
  "capital_gains_tax": "10%",
  "typical_coupon_range": "5.5% - 7%"
}
```

### Coupon Calculation

#### Fixed Rate (ORI, SR)
```
Monthly Coupon = (Principal × Annual Coupon Rate ÷ 12) × (1 - 0.10)
```

#### Floating Rate (SBR, ST)
```
Reference Rate = BI 7-Day Repo Rate + Spread
Coupon Rate = MAX(Reference Rate, Floor Rate)
Monthly Coupon = (Principal × Coupon Rate ÷ 12) × (1 - 0.10)
```

---

## Actions Available

### Primary Market (During Offering Period)
| Action | Requirements |
|--------|-------------|
| Purchase | Valid SID, e-KTP, within offering period |

### Secondary Market (ORI & SR Only)
| Action | Requirements |
|--------|-------------|
| Sell | Market is open, have holdings |
| Buy | Market is open, funds available |

### Early Redemption (SBR & ST Only)
| Action | Requirements |
|--------|-------------|
| Early Redeem | Held for 12+ months, max 50% of holdings |

---

## Random Events

### Positive Events
| Event | Probability | Effect |
|-------|-------------|--------|
| New Series Launch | 25% quarterly | Opportunity to buy new series |
| BI Rate Increase | 10% | Higher floating coupon (SBR/ST) |
| Secondary Market Premium | 8% | ORI/SR trades above par (101-105%) |

### Negative Events
| Event | Probability | Effect |
|-------|-------------|--------|
| BI Rate Decrease | 10% | Lower coupon (but floor protects) |
| Secondary Market Discount | 8% | ORI/SR trades below par (95-99%) |

### Note on "Negative" Events
Even with rate decreases, SBR and ST have floor rates—meaning your coupon never drops below a minimum guaranteed rate. And if you hold to maturity, you always get 100% principal back.

---

## Scoring System

| Achievement | Points |
|-------------|--------|
| Per Rp 1 million invested | +10 |
| Hold to maturity | +100 |
| National contribution bonus | +50 |
| Sharia-compliant investment | +25 |
| First SBN purchase | +50 |
| Own all 4 SBN types | +200 |

---

## Educational Objectives

This product teaches players:
- ✅ Government debt instruments
- ✅ Fixed income investing
- ✅ Interest rate mechanics
- ✅ Secondary market trading
- ✅ Sharia-compliant finance basics
- ✅ Contributing to national development

---

## Offering Schedule (Sample)

| Series | Offering Period | Tenor |
|--------|-----------------|-------|
| ORI027 | Jan 27 - Feb 20 | 3 years |
| ST014 | Mar 7 - Apr 16 | 2 years |
| SR022 | May 16 - Jun 18 | 3 years |
| SBR014 | Jul 14 - Aug 7 | 2 years |
| ORI028 | Sep 29 - Oct 23 | 6 years |
| ST015 | Nov 10 - Dec 3 | 4 years |

---

## UI/UX Guidelines

### Visual Theme
- **Primary Color:** #FF5722 (Orange - Government)
- **Secondary Color:** #FF8A65
- **Sharia Color:** #4CAF50 (Green)
- **Icon:** Shield with Checkmark

### Key Information Display
- Coupon rate
- Next coupon payment date
- Time to maturity
- Current market price (for tradeable)
- Monthly income projection
- Holding breakdown by series

---

## Implementation Notes for AI Agents

1. **Offering Period:** SBN can only be purchased during specific windows
2. **Coupon Calendar:** Generate monthly coupon payments on fixed dates
3. **Secondary Market:** For ORI/SR, simulate market prices based on interest rates
4. **Early Redemption Limit:** Track 50% maximum for SBR/ST
5. **Holding Period:** SBR/ST require 12-month hold before early redemption

```python
def calculate_monthly_coupon(principal, annual_rate, tax_rate=0.10):
    gross_coupon = principal * (annual_rate / 12)
    net_coupon = gross_coupon * (1 - tax_rate)
    return net_coupon

def calculate_floating_coupon(principal, bi_rate, spread, floor_rate, tax_rate=0.10):
    reference_rate = bi_rate + spread
    actual_rate = max(reference_rate, floor_rate)
    gross_coupon = principal * (actual_rate / 12)
    net_coupon = gross_coupon * (1 - tax_rate)
    return net_coupon, actual_rate

def calculate_secondary_market_price(face_value, coupon_rate, market_rate, years_to_maturity):
    # Simplified bond pricing
    if market_rate < coupon_rate:
        # Trades at premium
        premium = (coupon_rate - market_rate) * years_to_maturity * 0.5
        return face_value * (1 + premium)
    else:
        # Trades at discount
        discount = (market_rate - coupon_rate) * years_to_maturity * 0.5
        return face_value * (1 - discount)
```

### Sample Monthly Coupon Schedule

```python
def generate_coupon_schedule(principal, annual_rate, start_date, maturity_years):
    schedule = []
    current_date = start_date
    
    for month in range(maturity_years * 12):
        coupon = calculate_monthly_coupon(principal, annual_rate)
        schedule.append({
            "month": month + 1,
            "date": current_date,
            "gross_coupon": principal * (annual_rate / 12),
            "tax": principal * (annual_rate / 12) * 0.10,
            "net_coupon": coupon
        })
        # Move to next month
        current_date = add_month(current_date)
    
    # Add principal return at maturity
    schedule.append({
        "month": "maturity",
        "date": current_date,
        "principal_return": principal
    })
    
    return schedule
```
