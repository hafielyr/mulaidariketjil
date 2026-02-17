namespace InvestmentGame.Shared.Models;

public enum AgeMode
{
    Kids,
    Adult
}

public enum Language
{
    Indonesian,
    English
}

public class PortfolioItem
{
    public string AssetType { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Ticker { get; set; } = string.Empty; // For stocks
    public decimal Units { get; set; } // Changed to decimal to support fractional crypto units
    public decimal PricePerUnit { get; set; }
    public decimal TotalValue => Units * PricePerUnit;
    public decimal TotalCost { get; set; }
    public decimal ProfitLoss => TotalValue - TotalCost;
    public decimal ProfitLossPercent => TotalCost > 0 ? (ProfitLoss / TotalCost) * 100 : 0;
}

public class SavingsAccount
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public decimal Balance { get; set; }
    public decimal InterestRate { get; set; } // Annual rate (for calculation, not displayed)
}

public class DepositoItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ProductName { get; set; } = "Deposito";
    public decimal Principal { get; set; }
    public int PeriodMonths { get; set; }
    public decimal InterestRate { get; set; } // Annual rate
    public int StartYear { get; set; }
    public int StartMonth { get; set; }
    public int MonthsRemaining { get; set; }
    public bool AutoRollOver { get; set; } = false; // Automatically re-invest when matured
    public decimal MaturityValue => Principal * (1 + (InterestRate * PeriodMonths / 12));
    public decimal CurrentValue => Principal + (Principal * InterestRate * (PeriodMonths - MonthsRemaining) / 12);
    public int ProgressPercent => PeriodMonths > 0 ? (int)(((PeriodMonths - MonthsRemaining) / (decimal)PeriodMonths) * 100) : 0;
    public bool IsMatured => MonthsRemaining <= 0;
}

public class BondItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string BondName { get; set; } = "Obligasi Negara";
    public decimal Principal { get; set; }
    public int PeriodMonths { get; set; }
    public decimal CouponRate { get; set; } // Annual coupon rate
    public int StartYear { get; set; }
    public int StartMonth { get; set; }
    public int MonthsRemaining { get; set; }
    public decimal MaturityValue => Principal; // Bonds return principal at maturity
    public decimal TotalCouponEarned => Principal * CouponRate * (PeriodMonths - MonthsRemaining) / 12;
    public decimal CurrentValue => Principal + TotalCouponEarned;
    public int ProgressPercent => PeriodMonths > 0 ? (int)(((PeriodMonths - MonthsRemaining) / (decimal)PeriodMonths) * 100) : 0;
    public bool IsMatured => MonthsRemaining <= 0;
}

public class StockInfo
{
    public string Ticker { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string Sector { get; set; } = string.Empty;
    public decimal CurrentPrice { get; set; }
    public decimal PreviousPrice { get; set; }
    public decimal Change => CurrentPrice - PreviousPrice;
    public decimal ChangePercent => PreviousPrice > 0 ? (Change / PreviousPrice) * 100 : 0;
    public int LastPriceUpdateMonth { get; set; } // Track when price was last updated
    public decimal DividendYield { get; set; } // Annual dividend yield (e.g., 0.03 = 3%)
    public bool PaysDividend => DividendYield > 0;
}

public class CryptoInfo
{
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal CurrentPrice { get; set; }
    public decimal PreviousPrice { get; set; }
    public decimal Change => CurrentPrice - PreviousPrice;
    public decimal ChangePercent => PreviousPrice > 0 ? (Change / PreviousPrice) * 100 : 0;
}

public class CrowdfundingProject
{
    public string ProjectId { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public string ProjectType { get; set; } = string.Empty; // "Commodities", "Farm", "Tech Startup", "F&B", "Retail"
    public string Description { get; set; } = string.Empty;
    public decimal FundingGoal { get; set; }
    public decimal CurrentFunding { get; set; }
    public decimal MinimumInvestment { get; set; } = 100_000;
    public int DaysRemaining { get; set; }
    public decimal ExpectedReturn { get; set; } // Annual return if successful
    public int RiskLevel { get; set; } // 1-5
    public bool IsActive { get; set; } = true;
    public int LockUpMonths { get; set; } = 12; // Lock-up period in months (cannot withdraw before this)
    public bool HasFailed { get; set; } = false; // 20% chance of failure
    public string? FailureReason { get; set; } // Reason for project failure
}

/// <summary>
/// Tracks player's individual crowdfunding investment with lock-up period
/// </summary>
public class CrowdfundingInvestment
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ProjectId { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public string ProjectType { get; set; } = string.Empty;
    public decimal InvestedAmount { get; set; }
    public decimal ExpectedReturn { get; set; }
    public int StartYear { get; set; }
    public int StartMonth { get; set; }
    public int LockUpMonths { get; set; }
    public int MonthsRemaining { get; set; }
    public bool IsMatured => MonthsRemaining <= 0;
    public bool HasFailed { get; set; } = false;
    public string? FailureReason { get; set; }
    public decimal CurrentValue => HasFailed ? 0 : InvestedAmount * (1 + ExpectedReturn * (LockUpMonths - MonthsRemaining) / 12);
    public int ProgressPercent => LockUpMonths > 0 ? (int)(((LockUpMonths - MonthsRemaining) / (decimal)LockUpMonths) * 100) : 0;
}

public class AssetDefinition
{
    public string Type { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string DisplayNameAdult { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DescriptionAdult { get; set; } = string.Empty;
    public string DetailedInfo { get; set; } = string.Empty;
    public string WhatIsIt { get; set; } = string.Empty;
    public string WhatIsItAdult { get; set; } = string.Empty;
    public string RiskExplanation { get; set; } = string.Empty;
    public string RiskExplanationAdult { get; set; } = string.Empty;
    public string BestFor { get; set; } = string.Empty;
    public string BestForAdult { get; set; } = string.Empty;
    public string ExpectedReturn { get; set; } = string.Empty;
    public string ExpectedReturnAdult { get; set; } = string.Empty;

    // English variants
    public string DisplayNameEN { get; set; } = string.Empty;
    public string DisplayNameAdultEN { get; set; } = string.Empty;
    public string DescriptionEN { get; set; } = string.Empty;
    public string DescriptionAdultEN { get; set; } = string.Empty;
    public string WhatIsItEN { get; set; } = string.Empty;
    public string WhatIsItAdultEN { get; set; } = string.Empty;
    public string RiskExplanationEN { get; set; } = string.Empty;
    public string RiskExplanationAdultEN { get; set; } = string.Empty;
    public string BestForEN { get; set; } = string.Empty;
    public string BestForAdultEN { get; set; } = string.Empty;
    public string ExpectedReturnEN { get; set; } = string.Empty;
    public string ExpectedReturnAdultEN { get; set; } = string.Empty;

    public string RealRules { get; set; } = string.Empty;
    public decimal BasePrice { get; set; }
    public decimal MinReturn { get; set; } // Monthly min return
    public decimal MaxReturn { get; set; } // Monthly max return
    public bool AlwaysPositive { get; set; } = false;
    public string RiskLevel { get; set; } = string.Empty;
    public int UnlockYear { get; set; } = 1;
    public int UnlockMonth { get; set; } = 1; // Month within the year (1-12)
    public decimal MinimumInvestment { get; set; } = 1_000_000;
    public bool IsFixedIncome { get; set; } = false; // For bonds/deposito - no price fluctuation
    public string Category { get; set; } = string.Empty; // savings, deposito, bond, stock, index, gold, crypto, crowdfunding
}

public class DepositoRate
{
    public int PeriodMonths { get; set; }
    public string PeriodName { get; set; } = string.Empty;
    public decimal AnnualRate { get; set; }
    public decimal PenaltyRate { get; set; }
    public decimal MinimumDeposit { get; set; } = 1_000_000;
}

public class BondRate
{
    public int PeriodMonths { get; set; }
    public string PeriodName { get; set; } = string.Empty;
    public string BondType { get; set; } = string.Empty; // ORI, SR, SBR
    public decimal CouponRate { get; set; }
    public decimal MinimumInvestment { get; set; } = 1_000_000;
}

public class GameState
{
    public string PlayerId { get; set; } = string.Empty;
    public AgeMode AgeMode { get; set; } = AgeMode.Adult;
    public Language Language { get; set; } = Language.Indonesian;
    public int CurrentYear { get; set; } = 1;
    public int CurrentMonth { get; set; } = 1;
    public int MonthProgress { get; set; } = 0;
    public decimal CashBalance { get; set; }
    public SavingsAccount? SavingsAccount { get; set; }
    public List<PortfolioItem> Portfolio { get; set; } = new();
    public List<DepositoItem> Depositos { get; set; } = new();
    public List<BondItem> Bonds { get; set; } = new();
    public List<StockInfo> AvailableStocks { get; set; } = new();
    public List<CryptoInfo> AvailableCryptos { get; set; } = new();
    public List<CrowdfundingProject> AvailableCrowdfunding { get; set; } = new();
    public List<CrowdfundingInvestment> CrowdfundingInvestments { get; set; } = new();
    public List<AssetPrice> CurrentPrices { get; set; } = new();
    public decimal TotalPortfolioValue { get; set; }
    public decimal TotalDepositoValue { get; set; }
    public decimal TotalBondValue { get; set; }
    public decimal TotalSavingsValue { get; set; }
    public decimal TotalCrowdfundingValue { get; set; }
    public decimal NetWorth { get; set; }
    public string? CrowdfundingFailureMessage { get; set; }
    public bool IsGameOver { get; set; }
    public string? GameOverReason { get; set; }
    public string? ActiveEvent { get; set; }
    public string? ActiveEventDescription { get; set; }
    public decimal? EventCost { get; set; }
    public bool IsEventPending { get; set; }
    public List<string> GameLog { get; set; } = new();
    public List<string> UnlockedAssets { get; set; } = new();
    public string? NewUnlockMessage { get; set; }
    public bool ShowIntro { get; set; } = false;
    public string? IntroAssetType { get; set; }
    public int TotalGameMonths { get; set; } // Total months played (Year-1)*12 + Month

    // Bot state for comparison at game over
    public BotState? BotState { get; set; }

    // Player portfolio breakdown percentages for pie chart
    public decimal PlayerCashPercent { get; set; }
    public decimal PlayerSavingsPercent { get; set; }
    public decimal PlayerDepositoPercent { get; set; }
    public decimal PlayerBondPercent { get; set; }
    public decimal PlayerPortfolioPercent { get; set; }
}

public class AssetPrice
{
    public string AssetType { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Ticker { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal Change { get; set; }
    public decimal ChangePercent { get; set; }
    public bool ShowChange { get; set; } = true; // For savings, we don't show change
}

public class MarketUpdate
{
    public List<AssetPrice> Prices { get; set; } = new();
    public int Year { get; set; }
    public int Month { get; set; }
}

public class RandomEvent
{
    public string Title { get; set; } = string.Empty;
    public string TitleAdult { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DescriptionAdult { get; set; } = string.Empty;
    public decimal Cost { get; set; }
    public string Impact { get; set; } = string.Empty;
}

/// <summary>
/// Bot state for comparing player performance against emerging market investment strategy.
/// Based on typical financial advisor recommendations for emerging markets:
/// - 5% Emergency Fund (Savings)
/// - 25% Fixed Income - Deposito
/// - 20% Fixed Income - Government Bonds
/// - 30% Equities (Index Fund)
/// - 20% Commodities (Gold)
/// </summary>
public class BotState
{
    public string BotName { get; set; } = "Financial Advisor Bot";
    public string Strategy { get; set; } = "Emerging Market Balanced";

    public decimal CashBalance { get; set; }
    public decimal SavingsBalance { get; set; }
    public List<DepositoItem> Depositos { get; set; } = new();
    public List<BondItem> Bonds { get; set; } = new();
    public decimal IndexFundUnits { get; set; }
    public decimal IndexFundCost { get; set; }
    public decimal GoldUnits { get; set; }
    public decimal GoldCost { get; set; }

    // Calculated values (set by server)
    public decimal IndexFundValue { get; set; }
    public decimal GoldValue { get; set; }
    public decimal TotalDepositoValue { get; set; }
    public decimal TotalBondValue { get; set; }
    public decimal NetWorth { get; set; }

    // For comparison display
    public decimal TotalInvested { get; set; }
    public decimal TotalProfit { get; set; }
    public decimal ProfitPercent { get; set; }

    // Portfolio breakdown percentages for pie chart
    public decimal CashPercent { get; set; }
    public decimal SavingsPercent { get; set; }
    public decimal DepositoPercent { get; set; }
    public decimal BondPercent { get; set; }
    public decimal IndexFundPercent { get; set; }
    public decimal GoldPercent { get; set; }

    // Event tracking
    public int EventsPaidFromCash { get; set; }
    public int EventsPaidFromSavings { get; set; }
    public int EventsPaidFromPortfolio { get; set; }
    public decimal TotalEventCostPaid { get; set; }

    // Target allocation for display
    public string TargetAllocation { get; set; } = "5% Savings, 25% Deposito, 20% Bonds, 30% Index Fund, 20% Gold";
}
