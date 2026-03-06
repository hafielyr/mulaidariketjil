using InvestmentGame.Shared.Models;

namespace InvestmentGame.Server.Services;

public class GameSession
{
    public string PlayerId { get; set; } = string.Empty;
    public string ConnectionId { get; set; } = string.Empty;
    public AgeMode AgeMode { get; set; } = AgeMode.Adult;
    public Language Language { get; set; } = Language.Indonesian;

    // Multiplayer
    public string? RoomCode { get; set; }
    public bool IsMultiplayer { get; set; }
    public int CurrentYear { get; set; } = 1;
    public int CurrentMonth { get; set; } = 1;
    public int MonthProgress { get; set; } = 0;
    public decimal CashBalance { get; set; } = 5_000_000;

    // Savings Account (always available, no percentage shown)
    public SavingsAccount? SavingsAccount { get; set; }

    // Portfolio for stocks, index funds, gold, crypto, crowdfunding
    public Dictionary<string, PortfolioItem> Portfolio { get; set; } = new();

    // Depositos (Certificate of Deposit)
    public List<DepositoItem> Depositos { get; set; } = new();

    // Government Bonds
    public List<BondItem> Bonds { get; set; } = new();

    // Available stocks (5 random, prices change every 2 months)
    public List<StockInfo> AvailableStocks { get; set; } = new();

    // Available index funds (1 conventional + 1 shariah, real prices)
    public List<IndexInfo> AvailableIndices { get; set; } = new();

    // Gold price history for mini chart
    public List<decimal> GoldPriceHistory { get; set; } = new();

    // Available cryptos (Bitcoin, ETH, Memecoin)
    public List<CryptoInfo> AvailableCryptos { get; set; } = new();

    // Available crowdfunding projects (3-5 different business types)
    public List<CrowdfundingProject> AvailableCrowdfunding { get; set; } = new();

    // Player's crowdfunding investments (with lock-up tracking)
    public List<CrowdfundingInvestment> CrowdfundingInvestments { get; set; } = new();

    // Pending notification for failed crowdfunding project
    public string? CrowdfundingFailureMessage { get; set; }

    // Asset prices for fluctuating assets
    public Dictionary<string, decimal> AssetPrices { get; set; } = new();
    public Dictionary<string, decimal> PreviousPrices { get; set; } = new();

    public bool IsGameOver { get; set; } = false;
    public string? GameOverReason { get; set; }
    public bool IsPaused { get; set; } = false;
    public bool IsEventPending { get; set; } = false;
    public DateTime? EventPendingAt { get; set; }  // set when IsEventPending becomes true (multiplayer auto-pay)
    public DateTime LastTick { get; set; } = DateTime.UtcNow;
    public List<string> GameLog { get; set; } = new();
    public RandomEvent? ActiveEvent { get; set; }
    public decimal? EventCost { get; set; }
    public int EventMonthForYear { get; set; } = 0; // Randomized month (6-10) for event to occur
    public bool EventOccurredThisYear { get; set; } = false; // Track if event already happened this year
    public List<string> UnlockedAssets { get; set; } = new();
    public string? NewUnlockMessage { get; set; }
    public bool ShowIntro { get; set; } = false;
    public string? IntroAssetType { get; set; }

    // Per-session deposito and bond rates (refreshed each game year)
    public List<DepositoRate> CurrentDepositoRates { get; set; } = new();
    public List<BondRate> CurrentBondRates { get; set; } = new();

    // === BOT STATE ===
    // Bot uses aggressive balanced strategy optimized for growth:
    // 2% Savings, 15% Deposito (24mo), 15% Bonds (ST), 35% Index Fund, 10% Stocks, 23% Gold
    public decimal BotCashBalance { get; set; } = 5_000_000;
    public decimal BotSavingsBalance { get; set; } = 0;
    public List<DepositoItem> BotDepositos { get; set; } = new();
    public List<BondItem> BotBonds { get; set; } = new();
    public decimal BotIndexFundUnits { get; set; } = 0;
    public decimal BotIndexFundCost { get; set; } = 0;
    public decimal BotGoldUnits { get; set; } = 0;
    public decimal BotGoldCost { get; set; } = 0;
    public decimal BotStockCost { get; set; } = 0; // Total cost basis for bot stocks
    public decimal BotStockValue { get; set; } = 0; // Current value of bot stocks (grows toward 200% profit)
    public string BotStockTicker { get; set; } = string.Empty; // Which stock the bot picked
    public int BotEventsPaidFromCash { get; set; } = 0;
    public int BotEventsPaidFromSavings { get; set; } = 0;
    public int BotEventsPaidFromPortfolio { get; set; } = 0;
    public decimal BotTotalEventCostPaid { get; set; } = 0;

    // Player event tracking
    public decimal PlayerTotalEventCostPaid { get; set; } = 0;

    // Track cumulative investment returns
    public decimal TotalSavingsInterestEarned { get; set; } = 0;
    public decimal TotalDepositoInterestEarned { get; set; } = 0;
    public decimal TotalBondCouponEarned { get; set; } = 0;
    public decimal TotalRealizedPortfolioGainLoss { get; set; } = 0; // Accumulated P/L from sold stocks/index/gold/crypto
    public decimal TotalDividendEarned { get; set; } = 0; // Accumulated stock dividends
    // Track total crowdfunding invested (to calculate gain/loss)
    public decimal TotalCrowdfundingInvested { get; set; } = 0;
    public decimal TotalRealizedCrowdfundingGainLoss { get; set; } = 0; // P/L from matured/failed crowdfunding

    public const decimal UNIT_COST = 1_000_000;
    public const int MAX_YEARS = 15;
    public static readonly HashSet<int> EventYears = new() { 2, 3, 5, 6, 8, 10, 11, 12, 14, 15 };
    public const int MONTHS_PER_YEAR = 12;
    public const decimal YEARLY_INCOME = 10_000_000; // Reduced for tighter budgeting

    public int TotalGameMonths => (CurrentYear - 1) * MONTHS_PER_YEAR + CurrentMonth;

    public decimal TotalSavingsValue => SavingsAccount?.Balance ?? 0;
    public decimal TotalPortfolioValue => Portfolio.Values.Sum(p => p.TotalValue);
    public decimal TotalDepositoValue => Depositos.Sum(d => d.CurrentValue);
    public decimal TotalBondValue => Bonds.Sum(b => b.CurrentValue);
    public decimal TotalCrowdfundingValue => CrowdfundingInvestments.Where(c => !c.HasFailed).Sum(c => c.CurrentValue);
    public decimal NetWorth => CashBalance + TotalSavingsValue + TotalPortfolioValue + TotalDepositoValue + TotalBondValue + TotalCrowdfundingValue;

    // Bot calculated values
    public decimal BotIndexFundValue => BotIndexFundUnits * GetBotIndexPrice();
    public decimal BotGoldValue => BotGoldUnits * AssetPrices.GetValueOrDefault("emas", UNIT_COST);

    private decimal GetBotIndexPrice()
    {
        // Bot uses the first conventional index's price for its index fund value
        var convIdx = AvailableIndices.FirstOrDefault(i => !i.IsShariah);
        return convIdx?.CurrentPrice ?? AssetPrices.GetValueOrDefault("reksadana", UNIT_COST);
    }
    public decimal BotTotalDepositoValue => BotDepositos.Sum(d => d.CurrentValue);
    public decimal BotTotalBondValue => BotBonds.Sum(b => b.CurrentValue);
    public decimal BotNetWorth => BotCashBalance + BotSavingsBalance + BotIndexFundValue + BotGoldValue + BotTotalDepositoValue + BotTotalBondValue + BotStockValue;

    public void InitializePrices(Dictionary<string, AssetDefinition> assets)
    {
        foreach (var asset in assets)
        {
            if (!asset.Value.IsFixedIncome && asset.Value.Category != "savings" && asset.Value.Category != "deposito" && asset.Value.Category != "bond")
            {
                AssetPrices[asset.Key] = asset.Value.BasePrice;
                PreviousPrices[asset.Key] = asset.Value.BasePrice;
            }
        }
    }

    public void InitializeStocks(List<StockInfo> stocks)
    {
        AvailableStocks = stocks;
    }

    public void InitializeCryptos(List<CryptoInfo> cryptos)
    {
        AvailableCryptos = cryptos;
    }

    public void InitializeCrowdfunding(List<CrowdfundingProject> projects)
    {
        AvailableCrowdfunding = projects;
    }

    public GameState ToGameState()
    {
        // Calculate player portfolio percentages for pie chart
        var playerNetWorth = NetWorth;
        var playerCashPct = playerNetWorth > 0 ? (CashBalance / playerNetWorth) * 100 : 0;
        var playerSavingsPct = playerNetWorth > 0 ? (TotalSavingsValue / playerNetWorth) * 100 : 0;
        var playerDepositoPct = playerNetWorth > 0 ? (TotalDepositoValue / playerNetWorth) * 100 : 0;
        var playerBondPct = playerNetWorth > 0 ? (TotalBondValue / playerNetWorth) * 100 : 0;
        var playerPortfolioPct = playerNetWorth > 0 ? (TotalPortfolioValue / playerNetWorth) * 100 : 0;

        return new GameState
        {
            PlayerId = PlayerId,
            AgeMode = AgeMode,
            Language = Language,
            CurrentYear = CurrentYear,
            CurrentMonth = CurrentMonth,
            MonthProgress = MonthProgress,
            CashBalance = CashBalance,
            SavingsAccount = SavingsAccount,
            Portfolio = Portfolio.Select(kvp => { kvp.Value.Key = kvp.Key; return kvp.Value; }).ToList(),
            Depositos = Depositos.ToList(),
            Bonds = Bonds.ToList(),
            AvailableStocks = AvailableStocks.ToList(),
            AvailableIndices = AvailableIndices.ToList(),
            AvailableCryptos = AvailableCryptos.ToList(),
            // Gold real price data
            GoldCurrentPrice = AssetPrices.GetValueOrDefault("emas", 0),
            GoldPreviousPrice = PreviousPrices.GetValueOrDefault("emas", 0),
            GoldPriceHistory = GoldPriceHistory.ToList(),
            AvailableCrowdfunding = AvailableCrowdfunding.ToList(),
            CrowdfundingInvestments = CrowdfundingInvestments.ToList(),
            CurrentPrices = AssetPrices.Select(p => new AssetPrice
            {
                AssetType = p.Key,
                DisplayName = GetDisplayName(p.Key),
                Price = p.Value,
                Change = p.Value - (PreviousPrices.GetValueOrDefault(p.Key, p.Value)),
                ChangePercent = PreviousPrices.GetValueOrDefault(p.Key, p.Value) > 0
                    ? ((p.Value - PreviousPrices.GetValueOrDefault(p.Key, p.Value)) / PreviousPrices.GetValueOrDefault(p.Key, p.Value)) * 100
                    : 0,
                ShowChange = p.Key != "tabungan" // Don't show change for savings
            }).ToList(),
            TotalPortfolioValue = TotalPortfolioValue,
            TotalDepositoValue = TotalDepositoValue,
            TotalBondValue = TotalBondValue,
            TotalSavingsValue = TotalSavingsValue,
            TotalCrowdfundingValue = TotalCrowdfundingValue,
            NetWorth = NetWorth,
            CrowdfundingFailureMessage = CrowdfundingFailureMessage,
            IsGameOver = IsGameOver,
            GameOverReason = GameOverReason,
            ActiveEvent = ActiveEvent?.GetTitle(AgeMode, Language),
            ActiveEventDescription = ActiveEvent?.GetDescription(AgeMode, Language),
            EventCost = EventCost,
            IsEventPending = IsEventPending,
            GameLog = GameLog.TakeLast(15).ToList(),
            UnlockedAssets = UnlockedAssets,
            NewUnlockMessage = NewUnlockMessage,
            ShowIntro = ShowIntro,
            IntroAssetType = IntroAssetType,
            TotalGameMonths = TotalGameMonths,
            BotState = ToBotState(),
            // Player portfolio percentages
            PlayerCashPercent = playerCashPct,
            PlayerSavingsPercent = playerSavingsPct,
            PlayerDepositoPercent = playerDepositoPct,
            PlayerBondPercent = playerBondPct,
            PlayerPortfolioPercent = playerPortfolioPct,
            // Event cost tracking
            PlayerTotalEventCostPaid = PlayerTotalEventCostPaid,
            // Investment performance breakdown (realized + unrealized)
            SavingsInterestEarned = TotalSavingsInterestEarned,
            DepositoInterestEarned = TotalDepositoInterestEarned + Depositos.Sum(d => d.CurrentValue - d.Principal),
            BondCouponEarned = TotalBondCouponEarned,
            DividendEarned = TotalDividendEarned,
            PortfolioGainLoss = TotalRealizedPortfolioGainLoss + Portfolio.Values.Sum(p => p.ProfitLoss),
            CrowdfundingGainLoss = TotalRealizedCrowdfundingGainLoss
                + CrowdfundingInvestments.Where(c => !c.HasFailed).Sum(c => c.CurrentValue - c.InvestedAmount),
            TotalInvestmentGainLoss = TotalSavingsInterestEarned
                + TotalDepositoInterestEarned + Depositos.Sum(d => d.CurrentValue - d.Principal)
                + TotalBondCouponEarned
                + TotalDividendEarned
                + TotalRealizedPortfolioGainLoss + Portfolio.Values.Sum(p => p.ProfitLoss)
                + TotalRealizedCrowdfundingGainLoss
                + CrowdfundingInvestments.Where(c => !c.HasFailed).Sum(c => c.CurrentValue - c.InvestedAmount),
            // Multiplayer
            RoomCode = RoomCode,
            IsMultiplayer = IsMultiplayer,
            EventAutoPaySecondsRemaining = (IsMultiplayer && IsEventPending && EventPendingAt != null)
                ? Math.Max(0, 10 - (int)(DateTime.UtcNow - EventPendingAt.Value).TotalSeconds)
                : null,
            // Per-session rates
            AvailableDepositoRates = CurrentDepositoRates.ToList(),
            AvailableBondRates = CurrentBondRates.ToList()
        };
    }

    private BotState ToBotState()
    {
        var totalInvested = 5_000_000m; // Initial cash (matched to player starting amount)
        var indexFundValue = BotIndexFundValue;
        var goldValue = BotGoldValue;
        var stockValue = BotStockValue;
        var depositoValue = BotTotalDepositoValue;
        var bondValue = BotTotalBondValue;
        var botNetWorth = BotNetWorth;

        // Deduct player's total event cost from bot's displayed net worth
        // This ensures the bot is penalized the same emergency event amount as the player
        var playerEventDeduction = PlayerTotalEventCostPaid;
        var displayNetWorth = botNetWorth - playerEventDeduction;

        // Calculate portfolio percentages for pie chart (based on actual net worth)
        var cashPct = botNetWorth > 0 ? (BotCashBalance / botNetWorth) * 100 : 0;
        var savingsPct = botNetWorth > 0 ? (BotSavingsBalance / botNetWorth) * 100 : 0;
        var depositoPct = botNetWorth > 0 ? (depositoValue / botNetWorth) * 100 : 0;
        var bondPct = botNetWorth > 0 ? (bondValue / botNetWorth) * 100 : 0;
        var indexPct = botNetWorth > 0 ? (indexFundValue / botNetWorth) * 100 : 0;
        var goldPct = botNetWorth > 0 ? (goldValue / botNetWorth) * 100 : 0;
        var stockPct = botNetWorth > 0 ? (stockValue / botNetWorth) * 100 : 0;

        var totalIncomeReceived = totalInvested + (CurrentYear - 1) * YEARLY_INCOME;

        return new BotState
        {
            BotName = "Financial Advisor Bot",
            Strategy = "Balanced",
            CashBalance = BotCashBalance,
            SavingsBalance = BotSavingsBalance,
            Depositos = BotDepositos.ToList(),
            Bonds = BotBonds.ToList(),
            IndexFundUnits = BotIndexFundUnits,
            IndexFundCost = BotIndexFundCost,
            GoldUnits = BotGoldUnits,
            GoldCost = BotGoldCost,
            StockCost = BotStockCost,
            StockValue = stockValue,
            StockTicker = BotStockTicker,
            IndexFundValue = indexFundValue,
            GoldValue = goldValue,
            TotalDepositoValue = depositoValue,
            TotalBondValue = bondValue,
            NetWorth = botNetWorth,
            DisplayNetWorth = displayNetWorth,
            PlayerEventDeduction = playerEventDeduction,
            TotalInvested = totalIncomeReceived,
            TotalProfit = botNetWorth - totalIncomeReceived,
            ProfitPercent = totalIncomeReceived > 0 ? ((botNetWorth - totalIncomeReceived) / totalIncomeReceived) * 100 : 0,
            // Portfolio percentages for pie chart
            CashPercent = cashPct,
            SavingsPercent = savingsPct,
            DepositoPercent = depositoPct,
            BondPercent = bondPct,
            IndexFundPercent = indexPct,
            GoldPercent = goldPct,
            StockPercent = stockPct,
            // Event tracking
            EventsPaidFromCash = BotEventsPaidFromCash,
            EventsPaidFromSavings = BotEventsPaidFromSavings,
            EventsPaidFromPortfolio = BotEventsPaidFromPortfolio,
            TotalEventCostPaid = BotTotalEventCostPaid,
            TargetAllocation = "10% Savings, 10% Deposito, 15% Bonds, 30% Index Fund, 15% Stocks, 20% Gold"
        };
    }

    private string GetDisplayName(string assetType)
    {
        return assetType switch
        {
            "tabungan" => "Tabungan",
            "deposito" => "Deposito",
            "reksadana" => "Reksa Dana Indeks",
            "obligasi" => "Obligasi Negara",
            "saham" => "Saham",
            "emas" => "Emas",
            "crypto" => "Crypto",
            "crowdfunding" => "Crowd Funding",
            _ => assetType
        };
    }

    public void AddLogEntry(string message)
    {
        var timestamp = $"[Thn {CurrentYear} Bln {CurrentMonth}]";
        GameLog.Add($"{timestamp} {message}");
        if (GameLog.Count > 100)
        {
            GameLog.RemoveAt(0);
        }
    }
}
