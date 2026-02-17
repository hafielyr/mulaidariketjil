# Certificate of Deposit (Time Deposit / Deposito)
## Investment Game Mechanics Reference

---

## 🎯 Product Overview

### The Elevator Pitch
**Your Money's VIP Room** — Same safety as savings, but with better perks. Lock your money for a set time, get rewarded with higher interest. It's delayed gratification that actually pays.

### What It Actually Is
A Certificate of Deposit (or "Deposito" in Indonesia) is a fixed-term savings product. You agree to lock your money for a specific period (1 month to 24 months), and in return, the bank pays you higher interest than a regular savings account. Still fully protected by LPS up to Rp 2 billion.

---

## 📊 Key Stats for Game Mechanics

| Attribute | Value |
|-----------|-------|
| Risk Level | ⭐ (1/5) - Very Low |
| Liquidity | ★★ (2/5) - Locked Until Maturity |
| Complexity | ★★ (2/5) - Easy to Understand |
| Min. Investment | Rp 1,000,000 (typical: Rp 8,000,000) |
| Interest Range | 2.5% - 6% per year (varies by tenor) |
| Tax on Interest | 20% |
| Government Guarantee | Up to Rp 2 Billion (LPS) |

---

## 🏛️ Regulatory Framework (OJK & LPS)

### Same Protection, Better Returns
Deposits enjoy the same LPS protection as savings accounts:
- **Maximum Guarantee**: Rp 2,000,000,000 per customer per bank
- **Coverage**: Principal + accrued interest
- **Same 3T Rules Apply**

### Interest Rate by Tenor (Typical Ranges)

| Tenor | Interest Rate Range |
|-------|---------------------|
| 1 Month | 2.5% - 4.0% |
| 3 Months | 3.0% - 4.5% |
| 6 Months | 3.5% - 5.0% |
| 12 Months | 4.0% - 5.5% |
| 24 Months | 4.5% - 6.0% |

⚠️ **LPS Rate Warning**: If bank offers rates exceeding LPS maximum (currently 3.5% for commercial banks), your deposit may not be fully protected!

---

## 🎮 Game Mechanics

### Opening a Time Deposit

```
PLAYER MUST SELECT:
1. Amount (min Rp 1,000,000)
2. Tenor (1, 3, 6, 12, or 24 months)
3. Rollover Option (ARO, ARO+Interest, Non-ARO)

SYSTEM THEN:
- Locks funds until maturity date
- Records interest rate (fixed for entire tenor)
- Issues digital certificate
```

### Rollover Options Explained

| Option | What Happens at Maturity |
|--------|--------------------------|
| **ARO** (Auto Roll Over) | Principal rolls over, interest sent to savings |
| **ARO + Interest** | Principal + interest rolls over (compounding!) |
| **Non-ARO** | Everything returns to savings account |

### Interest Calculation

```
Total Interest = Principal × Annual Rate × (Tenor in days ÷ 365)
Net Interest = Total Interest × (1 - 0.20)

Example (12-month deposit):
Principal: Rp 100,000,000
Annual Rate: 5%
Gross Interest: 100,000,000 × 0.05 = Rp 5,000,000
Tax (20%): Rp 1,000,000
Net Interest: Rp 4,000,000
```

### Early Withdrawal Penalties

```
IF player withdraws before maturity:
- Forfeit ALL accrued interest
- Pay penalty fee: 1% of principal
- Must notify bank 3 days in advance
```

### Random Events

| Event | Probability | Effect |
|-------|-------------|--------|
| Promo Rate Available | 8% | +1% bonus rate for new deposits |
| BI Rate Increase | 8% | New deposits get better rates |
| BI Rate Decrease | 7% | New deposits get lower rates |
| Bank Failure | 0.1% | LPS guarantee activated |

---

## 🏆 Achievements & Scoring

### Point System
- **+1 point** per Rp 10,000 interest earned
- **+25 points** for 6-month deposit held to maturity
- **+50 points** for 12-month deposit held to maturity
- **+100 points** for 24-month deposit held to maturity
- **-30 points** for early withdrawal

### Unlockable Achievements

| Achievement | Condition | Points |
|-------------|-----------|--------|
| 🔐 Locked & Loaded | Open first time deposit | 30 |
| ⏳ Patient Investor | Hold 12-month deposit to maturity | 75 |
| 🔄 Compound Master | ARO with interest 3 times | 100 |
| 🏦 Deposit Diversifier | Deposits at 3 different tenors | 60 |

---

## 💡 Strategic Tips (For Game AI)

### Optimal Strategy Hints
1. **Laddering Strategy**: Spread deposits across different maturities for liquidity + returns
2. **Rate Lock Timing**: Lock in longer tenors when rates are high
3. **Compound Power**: Use ARO+Interest for maximum growth
4. **LPS Awareness**: Stay under Rp 2B per bank

### When to Recommend This Product
- Player has stable emergency fund in savings
- Player has money they won't need for 3+ months
- Player wants guaranteed returns (no market risk)
- Player prefers predictable income

### When NOT to Recommend
- Player might need the money unexpectedly
- Player is seeking higher growth (suggest index funds)
- Player already maxing out LPS limits
- Interest rates are expected to rise significantly

### Laddering Strategy Example
```
Instead of: Rp 120M in one 12-month deposit
Do this:
- Rp 30M in 3-month deposit
- Rp 30M in 6-month deposit
- Rp 30M in 9-month deposit
- Rp 30M in 12-month deposit

Result: One deposit matures every 3 months = regular access + decent rates
```

---

## 📝 Sample In-Game Dialogues

### Introduction
> "Ready to level up from savings? Time deposits are like VIP savings — you commit to a timeframe, and the bank rewards your patience with better interest rates. Same safety, better gains."

### On Opening First Deposit
> "🔐 Deposit LOCKED! Your money is now working harder for you. The clock is ticking toward payday. Just remember: patience is literally paying you right now."

### On Maturity Notification
> "⏰ DING DING! Your [X]-month deposit matures in 7 days! Decision time: Roll it over for compound magic, or take the win and redeploy elsewhere?"

### On Successful Maturity
> "💰 PAYDAY! Your patience has been rewarded with Rp [X] in interest. That's [Y]% growth while you did absolutely nothing. This is how wealth is quietly built."

### On Early Withdrawal Warning
> "⚠️ Hold up! Withdrawing early means losing ALL your accrued interest PLUS a 1% penalty. That's Rp [X] gone. Are you sure? Sometimes the best move is no move."

### On Compound Rollover
> "🔄 COMPOUND ACTIVATED! Your interest is now earning interest. Einstein called this the 8th wonder of the world. You're now playing the long game like a pro."

---

## 🔗 Connections to Other Products

### Natural Progression Path
```
Savings Account (emergency fund complete)
    ↓
Certificate of Deposit ← You are here! 📍
    ↓
Government Bonds (longer term, similar safety)
    ↓
Index Fund (when comfortable with market risk)
```

### Complementary Strategies
- **Savings + Deposit Combo**: Emergency fund in savings, excess in deposits
- **Deposit Ladder**: Multiple deposits maturing at different times
- **Deposit → SBN Bridge**: When deposits mature during SBN offering period

---

## ⚠️ Risk Disclosures (Required Display)

1. Early withdrawal results in forfeiture of interest and penalty fees
2. Interest rates are fixed at opening — you may miss better rates later
3. Inflation may exceed deposit interest, reducing real purchasing power
4. LPS guarantee only applies if all requirements are met
5. Interest rates shown are gross; actual returns are after 20% tax
6. Past rates do not guarantee future returns

---

*Document Version: 1.0*
*Last Updated: January 2025*
*Regulatory Reference: OJK & LPS Regulations*
