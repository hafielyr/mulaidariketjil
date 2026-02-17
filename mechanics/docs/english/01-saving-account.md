# Saving Account (Tabungan)
## Investment Game Mechanics Reference

---

## 🎯 Product Overview

### The Elevator Pitch
**Your Money's Safe House** — The OG of financial products. Before you chase returns, build your fortress. A saving account isn't sexy, but it's the foundation every wealth builder needs.

### What It Actually Is
A saving account is a bank deposit product that lets you store money safely while earning a small interest. It's fully liquid (take your money anytime), government-protected up to Rp 2 billion, and the perfect home for your emergency fund.

---

## 📊 Key Stats for Game Mechanics

| Attribute | Value |
|-----------|-------|
| Risk Level | ⭐ (1/5) - Very Low |
| Liquidity | ★★★★★ (5/5) - Instant Access |
| Complexity | ★ (1/5) - Beginner Friendly |
| Min. Investment | Rp 10,000 |
| Typical Interest | 0.5% - 6% per year |
| Tax on Interest | 20% |
| Government Guarantee | Up to Rp 2 Billion (LPS) |

---

## 🏛️ Regulatory Framework (OJK & LPS)

### The Guardian: LPS (Lembaga Penjamin Simpanan)
Think of LPS as your money's insurance policy. If a bank fails (rare, but possible), LPS has your back.

**The Magic Number: Rp 2,000,000,000**
- Maximum guarantee per customer per bank
- Covers principal + interest
- Applies to savings, deposits, and current accounts

### The 3T Rule (Requirements for LPS Protection)
Your savings are protected IF:
1. **Tercatat** - Properly recorded in bank's books
2. **Tingkat Bunga** - Interest rate doesn't exceed LPS maximum
3. **Tidak Merugikan** - You haven't done anything to harm the bank

### Current LPS Guaranteed Interest Rates (Oct 2025 - Jan 2026)
| Bank Type | IDR Rate | Foreign Currency |
|-----------|----------|------------------|
| Commercial Banks | 3.50% | 2.00% |
| Rural Banks (BPR) | 6.00% | - |

⚠️ **Pro Tip**: If a bank offers interest ABOVE these rates, your deposit may NOT be protected by LPS!

---

## 🎮 Game Mechanics

### Monthly Tick Events

```
EACH MONTH:
1. Calculate average daily balance
2. Apply interest: balance × (annual_rate ÷ 12)
3. Deduct tax: interest × 20%
4. Deduct admin fee (if applicable)
5. Check for random events
```

### Interest Calculation Formula
```
Net Monthly Interest = (Average Balance × Annual Rate ÷ 12) × (1 - 0.20)

Example:
Balance: Rp 10,000,000
Annual Rate: 3%
Gross Interest: 10,000,000 × 0.03 ÷ 12 = Rp 25,000
Tax (20%): Rp 5,000
Net Interest: Rp 20,000
```

### Random Events

| Event | Probability | Effect |
|-------|-------------|--------|
| Bonus Interest Promo | 5% | 1.5x interest this month |
| BI Rate Increase | 10% | +0.5% to all rates |
| BI Rate Decrease | 10% | -0.5% to all rates |
| Bank Failure | 0.1% | LPS guarantee activated |

### Player Actions

| Action | Details |
|--------|---------|
| **Deposit** | Min Rp 10,000, No fee |
| **Withdraw** | Instant, No penalty |
| **Transfer (Same Bank)** | Free |
| **Transfer (Other Bank)** | Rp 6,500 |
| **Transfer (BI-FAST)** | Rp 2,500 |

---

## 🏆 Achievements & Scoring

### Point System
- **+1 point** per Rp 1,000 interest earned
- **+50 points** for consistent monthly deposits (3+ months)
- **+100 points** for completing emergency fund goal
- **-100 points** if balance exceeds LPS limit at single bank

### Unlockable Achievements

| Achievement | Condition | Points |
|-------------|-----------|--------|
| 🎯 First Steps | Make first deposit | 25 |
| 🛡️ Safety Net Builder | Balance reaches Rp 1M | 50 |
| 💪 Financially Prepared | Balance reaches Rp 10M | 100 |
| 🏦 Diversified Saver | Accounts at 2+ banks | 75 |

---

## 💡 Strategic Tips (For Game AI)

### Optimal Strategy Hints
1. **Emergency Fund First**: 3-6 months of expenses in savings before other investments
2. **LPS Limit Awareness**: Split funds across banks if approaching Rp 2B
3. **Rate Shopping**: Digital banks often offer higher rates (but check LPS coverage!)
4. **Admin Fee Avoidance**: Maintain minimum balance to avoid monthly fees

### When to Recommend This Product
- Player has no emergency fund
- Player needs immediate liquidity
- Player is risk-averse (conservative profile)
- Player is just starting their financial journey

### When NOT to Recommend
- Player already has adequate emergency fund
- Player's money will sit idle for 1+ years (suggest deposits)
- Player is seeking growth (inflation will eat returns)

---

## 📝 Sample In-Game Dialogues

### Introduction
> "Every financial empire starts with a simple truth: you can't invest what you don't have saved. Your saving account is your financial home base — safe, accessible, and protected by the government up to Rp 2 billion."

### On First Deposit
> "Boom! 💥 You just took the first step most people never take. Your money is now earning interest 24/7 while you sleep. Small? Yes. But this is where millionaires start."

### On Reaching Rp 1 Million
> "Safety Net: ACTIVATED. You now have a cushion against life's unexpected plot twists. Keep building — the magic number is 3-6 months of expenses."

### On LPS Warning
> "⚠️ Heads up! You're approaching the Rp 2 billion LPS guarantee limit at this bank. Consider spreading your wealth across multiple banks to keep full protection."

### On Interest Payment
> "Ka-ching! 💰 Your money just made Rp [X] while you were living your life. It's not much, but it's honest work. Compound interest is working for you now."

---

## 🔗 Connections to Other Products

### Natural Progression Path
```
Saving Account → Certificate of Deposit (higher interest, locked)
             → Government Bonds (better returns, still safe)
             → Index Fund (when emergency fund complete)
```

### Complementary Products
- **Auto-debit to Index Fund**: Set up regular investing from savings
- **Auto-debit to Deposits**: Move excess to higher-yield deposits
- **Backup for Stock Trading**: Keep trading capital here before deploying

---

## ⚠️ Risk Disclosures (Required Display)

1. Interest rates may change based on Bank Indonesia policy
2. Inflation may exceed interest earned, reducing real purchasing power
3. LPS guarantee only applies if requirements are met
4. Admin fees may reduce or eliminate interest earnings on small balances
5. Past interest rates do not guarantee future returns

---

*Document Version: 1.0*
*Last Updated: January 2025*
*Regulatory Reference: OJK & LPS Regulations*
