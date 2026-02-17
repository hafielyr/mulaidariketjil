# Saving Account - Game Mechanics Reference

## Product Overview

**Product ID:** `saving_account`  
**Category:** Banking  
**Risk Level:** 1/5 (Very Low Risk)

---

## The Pitch (Marketing Copy)

### Headline
**Your Money's Safe House. Seriously.**

### Tagline
*Sleep tight knowing your cash is protected up to Rp 2 billion. No jokes.*

### Description
Think of a saving account as your financial home base. It's not sexy, it won't make you rich overnight, but it's the foundation every smart investor builds on. Your money stays liquid, earns a little interest, and most importantly—it's protected by the government through LPS.

This is where your emergency fund lives. This is where you park cash before making bigger moves. This is adulting 101.

---

## Regulatory Framework

### Governing Bodies
- **Primary Regulator:** OJK (Otoritas Jasa Keuangan)
- **Deposit Guarantee:** LPS (Lembaga Penjamin Simpanan)

### Key Regulations
- UU No. 24 Tahun 2004 tentang LPS
- UU No. 7 Tahun 2009 (Amendment)
- PP No. 66 Tahun 2008

### LPS Guarantee Details
| Parameter | Value |
|-----------|-------|
| Maximum Coverage | Rp 2,000,000,000 per customer per bank |
| Coverage Scope | Principal + accrued interest |
| Requirements | 3T Rule (see below) |

### The 3T Rule (Syarat Penjaminan)
1. **Tercatat** - Deposit must be properly recorded in bank's books
2. **Tingkat Bunga** - Interest rate must not exceed LPS maximum rate
3. **Tidak Merugikan** - Customer must not have caused losses to the bank

### Current LPS Guaranteed Rates (Oct 2025 - Jan 2026)
- Commercial Bank (Rupiah): 3.50% p.a.
- Commercial Bank (Foreign Currency): 2.00% p.a.
- Rural Bank (BPR): 6.00% p.a.

---

## Game Mechanics

### Base Statistics
| Stat | Score (1-5) |
|------|-------------|
| Risk | 1 |
| Liquidity | 5 |
| Complexity | 1 |
| Potential Return | 1 |

### Financial Parameters

```json
{
  "min_deposit": 10000,
  "typical_min_deposit": 100000,
  "interest_rate_range": {
    "min": "0.5%",
    "max": "6.0%",
    "typical": "2.5%"
  },
  "tax_on_interest": "20%",
  "admin_fee_monthly": "Rp 0 - Rp 15,000"
}
```

### Available Actions

| Action | Minimum | Fee | Settlement |
|--------|---------|-----|------------|
| Deposit | Rp 10,000 | Free | Instant |
| Withdraw | Rp 10,000 | Free | Instant |
| Transfer (same bank) | Rp 10,000 | Free | Instant |
| Transfer (different bank) | Rp 10,000 | Rp 6,500 | Instant |

### Monthly Interest Calculation
```
Net Interest = (Average Daily Balance × Annual Rate ÷ 12) × (1 - 0.20)
Final Balance = Previous Balance + Net Interest - Admin Fee
```

---

## Random Events

### Positive Events
| Event | Probability | Effect |
|-------|-------------|--------|
| Bonus Interest Promo | 5% | Interest × 1.5 for one month |
| BI Rate Increase | 10% | +0.5% to interest rate |

### Negative Events
| Event | Probability | Effect |
|-------|-------------|--------|
| BI Rate Decrease | 10% | -0.5% from interest rate |
| Bank Failure | 0.1% | LPS guarantee activated |

### Bank Failure Scenario
When bank failure occurs:
1. Check if total deposits ≤ Rp 2 billion
2. If YES: Full amount protected by LPS
3. If NO: Only Rp 2 billion protected, excess may be lost
4. Player receives LPS payout within game simulation

---

## Scoring System

| Achievement | Points |
|-------------|--------|
| First deposit made | +25 |
| Per Rp 1,000 interest earned | +1 |
| Diversification bonus (own 3+ products) | +50 |
| Exceeding LPS limit without spreading | -100 |

---

## Educational Objectives

This product teaches players:
- ✅ Basic concept of saving and interest
- ✅ Government deposit protection (LPS)
- ✅ Importance of staying within guaranteed limits
- ✅ Trade-off between safety and returns
- ✅ Liquidity management

---

## UI/UX Guidelines

### Visual Theme
- **Primary Color:** #4CAF50 (Green - Safety)
- **Secondary Color:** #81C784
- **Icon:** Piggy Bank

### Key Information Display
Always show:
- Current balance
- Interest rate
- LPS coverage status (green if under limit, yellow warning if approaching)
- Monthly interest earned

---

## Implementation Notes for AI Agents

1. **Balance Tracking:** Update balance daily based on interest accrual
2. **LPS Check:** Always verify if player's total deposits across all banks exceed Rp 2B
3. **Event Triggering:** Roll for random events at the start of each game month
4. **Tax Application:** Automatically deduct 20% tax from interest
5. **Admin Fee:** Apply monthly admin fee based on bank selection

```python
# Sample calculation
def calculate_monthly_interest(balance, annual_rate, tax_rate=0.20):
    gross_interest = balance * (annual_rate / 12)
    net_interest = gross_interest * (1 - tax_rate)
    return net_interest
```
