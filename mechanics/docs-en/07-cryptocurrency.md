# Cryptocurrency - Game Mechanics Reference

## Product Overview

**Product ID:** `cryptocurrency`  
**Category:** Digital Financial Asset  
**Risk Level:** 5/5 (Very High Risk)

---

## The Pitch (Marketing Copy)

### Headline
**The Wild West of Finance. Enter at Your Own Risk.**

### Tagline
*100x gains possible. 100% losses also possible. Only play with money you can afford to lose entirely.*

### Description
Crypto is the financial frontier. Bitcoin. Ethereum. Solana. Names that didn't exist 15 years ago now command trillion-dollar valuations.

**But let's be brutally honest:** This is speculation, not investing. Prices can crash 50% in a week. Exchanges can get hacked. Regulations can change overnight.

Since January 2025, OJK regulates crypto in Indonesia. Use only licensed exchanges. Never invest your emergency fund. And for the love of your future self—don't FOMO buy at all-time highs.

---

## Regulatory Framework

### Governing Bodies
- **Regulator:** OJK (since Jan 2025, transferred from BAPPEBTI)
- **Legal Basis:** POJK No. 27/2024, POJK No. 23/2025

### ⚠️ CRITICAL WARNINGS
- **NO government guarantee**
- **NO deposit protection**
- **Highly speculative asset**
- Use only OJK-licensed exchanges

### Licensed Exchanges
Indodax, Tokocrypto, Pintu, Rekeningku, Zipmex

---

## Game Mechanics

### Base Statistics
| Stat | Score (1-5) |
|------|-------------|
| Risk | 5 |
| Liquidity | 5 |
| Complexity | 4 |
| Potential Return | 5 |

### Sample Assets
| Code | Name | Volatility |
|------|------|------------|
| BTC | Bitcoin | 10% |
| ETH | Ethereum | 12% |
| BNB | Binance Coin | 15% |
| USDT | Tether | 0.1% |
| SOL | Solana | 18% |

### Financial Parameters
| Parameter | Value |
|-----------|-------|
| Min Investment | Rp 10,000 |
| Trading Fee | 0.1% - 0.5% |
| Transaction Tax | 0.1% |
| Income Tax | 0.1% |
| Trading Hours | 24/7 |

---

## Random Events

| Event | Probability | Effect |
|-------|-------------|--------|
| Bull Run | 5% | +20% to +100% |
| Crash | 8% | -20% to -50% |
| Positive Regulation | 5% | +10% to +25% |
| Exchange Hack | 1% | 10-50% loss |
| Whale Movement | 10% | High volatility |

---

## Scoring System

| Achievement | Points |
|-------------|--------|
| Per 1% gain | +5 |
| Per 1% loss | -3 |
| Survive crash | +100 |
| FOMO buy (buy at ATH) | -20 |
| Diamond hands (hold through 50% drop) | +100 |

---

## Implementation Notes

```python
import random

def simulate_crypto_price(current_price, volatility=0.10):
    daily_return = random.gauss(0, volatility/15)  # Daily vol
    
    # Extreme events
    if random.random() < 0.05:
        daily_return += random.uniform(-0.20, 0.30)
    
    return current_price * (1 + daily_return)
```
