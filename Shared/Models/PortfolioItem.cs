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
    public string Key { get; set; } = string.Empty; // Portfolio dictionary key (e.g. "stock_TLKM", "index_IHSG")
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
    public bool IsShariah { get; set; } // Shariah (Muamalat Mudharabah) or conventional (BRI)
    public string? NisbahRatio { get; set; } // e.g. "55:45" (shariah only, for display)
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
    public bool IsShariah { get; set; } // SR (Sukuk Ritel) or ORI (conventional)
    public string? SeriesName { get; set; } // e.g. "ORI001", "SR005"
    public string? AkadType { get; set; } // e.g. "Ijarah" (shariah only)
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
    public decimal AnnualDividendPerShare { get; set; } // Total dividend per share for current game year (from real data)
    public string DividendType { get; set; } = "None"; // "Final", "Interim", "Final+Interim", or "None"
    public bool PaysDividend => AnnualDividendPerShare > 0;
    public bool IsShariahCompliant { get; set; } // Whether the stock is shariah-compliant
    public List<decimal> PriceHistory { get; set; } = new(); // Last 7 prices for mini chart
}

public class IndexInfo
{
    public string IndexId { get; set; } = string.Empty;       // e.g. "IHSG", "JII"
    public string DisplayName { get; set; } = string.Empty;   // e.g. "IHSG (JCI)", "Jakarta Islamic Index"
    public bool IsShariah { get; set; }
    public decimal CurrentPrice { get; set; }  // NAV / index value
    public decimal PreviousPrice { get; set; }
    public decimal Change => CurrentPrice - PreviousPrice;
    public decimal ChangePercent => PreviousPrice > 0 ? (Change / PreviousPrice) * 100 : 0;
    public List<decimal> PriceHistory { get; set; } = new();
}

public class CryptoInfo
{
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal CurrentPrice { get; set; }
    public decimal PreviousPrice { get; set; }
    public decimal Change => CurrentPrice - PreviousPrice;
    public decimal ChangePercent => PreviousPrice > 0 ? (Change / PreviousPrice) * 100 : 0;
    public List<decimal> PriceHistory { get; set; } = new();
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
    public bool IsShariah { get; set; }
    public string? NisbahRatio { get; set; } // e.g. "55:45" (shariah only)
    public string? BankName { get; set; } // "BRI" or "Bank Muamalat"
}

public class BondRate
{
    public int PeriodMonths { get; set; }
    public string PeriodName { get; set; } = string.Empty;
    public string BondType { get; set; } = string.Empty; // ORI, SR
    public decimal CouponRate { get; set; }
    public decimal MinimumInvestment { get; set; } = 1_000_000;
    public bool IsShariah { get; set; }
    public string? SeriesName { get; set; } // e.g. "ORI004", "SR001"
    public string? AkadType { get; set; } // e.g. "Ijarah" (shariah only)
}

// === MULTIPLAYER MODELS ===

public enum RoomStatus
{
    Waiting,
    InProgress,
    Finished
}

public class RoomInfo
{
    public string RoomCode { get; set; } = string.Empty;
    public string HostConnectionId { get; set; } = string.Empty;
    public string HostPlayerName { get; set; } = string.Empty;
    public AgeMode AgeMode { get; set; } = AgeMode.Adult;
    public Language Language { get; set; } = Language.Indonesian;
    public int MaxPlayers { get; set; } = 100;
    public List<RoomPlayer> Players { get; set; } = new();  // excludes host
    public RoomStatus Status { get; set; } = RoomStatus.Waiting;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class RoomPlayer
{
    public string ConnectionId { get; set; } = string.Empty;
    public string PlayerName { get; set; } = string.Empty;
    public bool IsReady { get; set; }
    public bool IsConnected { get; set; } = true;
    public bool IsUnlockReady { get; set; } = false;  // for unlock-sync flow
}

public class PlayerSummary
{
    public string ConnectionId { get; set; } = string.Empty;
    public string PlayerName { get; set; } = string.Empty;
    public decimal NetWorth { get; set; }
    public bool IsConnected { get; set; } = true;
    public int CurrentYear { get; set; }
    public int CurrentMonth { get; set; }
    public Dictionary<string, decimal> PortfolioBreakdown { get; set; } = new();  // % per asset type
    public decimal TotalGainLoss { get; set; }
}

public class LeaderboardEntry
{
    public int Rank { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    public decimal NetWorth { get; set; }
    public bool IsBot { get; set; }
    public decimal TotalProfit { get; set; }
    public bool IsConnected { get; set; } = true;
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
    public List<IndexInfo> AvailableIndices { get; set; } = new();
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

    // Gold real price data
    public decimal GoldCurrentPrice { get; set; }
    public decimal GoldPreviousPrice { get; set; }
    public List<decimal> GoldPriceHistory { get; set; } = new();

    // Bot state for comparison at game over
    public BotState? BotState { get; set; }

    // Advisory tips (shown at year-end)
    public List<AdvisorTip> AdvisorTips { get; set; } = new();

    // Multiplayer fields
    public string? RoomCode { get; set; }
    public bool IsMultiplayer { get; set; }
    public List<PlayerSummary>? PlayerSummaries { get; set; }
    public bool IsHost { get; set; }
    public bool AllPlayersFinished { get; set; }
    public List<LeaderboardEntry>? FinalLeaderboard { get; set; }
    public int? EventAutoPaySecondsRemaining { get; set; }  // server sets remaining seconds for client countdown

    // Available rates (refreshed per-session each game year)
    public List<DepositoRate> AvailableDepositoRates { get; set; } = new();
    public List<BondRate> AvailableBondRates { get; set; } = new();

    // Player portfolio breakdown percentages for pie chart
    public decimal PlayerCashPercent { get; set; }
    public decimal PlayerSavingsPercent { get; set; }
    public decimal PlayerDepositoPercent { get; set; }
    public decimal PlayerBondPercent { get; set; }
    public decimal PlayerPortfolioPercent { get; set; }

    // Player event cost tracking (for excluding from investment performance)
    public decimal PlayerTotalEventCostPaid { get; set; }

    // Investment performance breakdown for summary
    public decimal SavingsInterestEarned { get; set; }
    public decimal DepositoInterestEarned { get; set; }
    public decimal BondCouponEarned { get; set; }
    public decimal DividendEarned { get; set; }
    public decimal PortfolioGainLoss { get; set; } // Realized + unrealized P/L from Stocks, Index, Gold, Crypto
    public decimal CrowdfundingGainLoss { get; set; } // Realized + unrealized P/L from Crowdfunding
    public decimal TotalInvestmentGainLoss { get; set; }
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
    // Indonesian
    public string Title { get; set; } = string.Empty; // Kids ID
    public string TitleAdult { get; set; } = string.Empty; // Adult ID
    public string Description { get; set; } = string.Empty; // Kids ID
    public string DescriptionAdult { get; set; } = string.Empty; // Adult ID

    // English
    public string TitleEN { get; set; } = string.Empty; // Kids EN
    public string TitleAdultEN { get; set; } = string.Empty; // Adult EN
    public string DescriptionEN { get; set; } = string.Empty; // Kids EN
    public string DescriptionAdultEN { get; set; } = string.Empty; // Adult EN

    public decimal Cost { get; set; }
    public string Impact { get; set; } = string.Empty;

    public string GetTitle(AgeMode mode, Language lang) => (mode, lang) switch
    {
        (AgeMode.Kids, Language.Indonesian) => Title,
        (AgeMode.Adult, Language.Indonesian) => TitleAdult,
        (AgeMode.Kids, Language.English) => TitleEN,
        (AgeMode.Adult, Language.English) => TitleAdultEN,
        _ => Title
    };

    public string GetDescription(AgeMode mode, Language lang) => (mode, lang) switch
    {
        (AgeMode.Kids, Language.Indonesian) => Description,
        (AgeMode.Adult, Language.Indonesian) => DescriptionAdult,
        (AgeMode.Kids, Language.English) => DescriptionEN,
        (AgeMode.Adult, Language.English) => DescriptionAdultEN,
        _ => Description
    };
}

/// <summary>
/// Contextual financial advisory tip shown to the player during gameplay.
/// Generated at year-end based on portfolio analysis and market data.
/// </summary>
public class AdvisorTip
{
    public string Message { get; set; } = string.Empty;
    public string MessageEN { get; set; } = string.Empty;
    public string Category { get; set; } = "info"; // "warning", "suggestion", "info"

    public string GetMessage(Language lang) => lang == Language.English ? MessageEN : Message;
}

/// <summary>
/// Bot state for comparing player performance against aggressive investment strategy.
/// Uses future data to time stock trades. Very hard to beat.
/// </summary>
public class BotState
{
    public string BotName { get; set; } = "Financial Advisor Bot";
    public string Strategy { get; set; } = "Balanced";

    public decimal CashBalance { get; set; }
    public decimal SavingsBalance { get; set; }
    public List<DepositoItem> Depositos { get; set; } = new();
    public List<BondItem> Bonds { get; set; } = new();
    public decimal IndexFundUnits { get; set; }
    public decimal IndexFundCost { get; set; }
    public decimal GoldUnits { get; set; }
    public decimal GoldCost { get; set; }
    public decimal StockCost { get; set; }
    public decimal StockValue { get; set; }
    public string StockTicker { get; set; } = string.Empty;

    // Crypto holdings
    public decimal CryptoUnits { get; set; }
    public decimal CryptoCost { get; set; }
    public decimal CryptoValue { get; set; }
    public string CryptoSymbol { get; set; } = string.Empty;

    // Crowdfunding holdings
    public decimal CrowdfundingValue { get; set; }
    public decimal CrowdfundingCost { get; set; }

    // Calculated values (set by server)
    public decimal IndexFundValue { get; set; }
    public decimal GoldValue { get; set; }
    public decimal TotalDepositoValue { get; set; }
    public decimal TotalBondValue { get; set; }
    public decimal NetWorth { get; set; }
    public decimal DisplayNetWorth { get; set; } // NetWorth after player event deduction
    public decimal PlayerEventDeduction { get; set; } // Amount deducted to match player events

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
    public decimal StockPercent { get; set; }
    public decimal CryptoPercent { get; set; }
    public decimal CrowdfundingPercent { get; set; }

    // Event tracking
    public int EventsPaidFromCash { get; set; }
    public int EventsPaidFromSavings { get; set; }
    public int EventsPaidFromPortfolio { get; set; }
    public decimal TotalEventCostPaid { get; set; }

    // Target allocation for display
    public string TargetAllocation { get; set; } = "40% Stocks, 20% Deposito, 10% Index Fund, 10% Crypto, 5% Bonds, 5% Gold, 5% CrowdFunding, 5% Savings";
}
