using InvestmentGame.Shared.Models;

namespace InvestmentGame.Server.Services;

public class GameSession
{
    public string PlayerId { get; set; } = string.Empty;
    public string ConnectionId { get; set; } = string.Empty;
    public AgeMode AgeMode { get; set; } = AgeMode.Adult;
    public Language Language { get; set; } = Language.Indonesian;
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

    // === BOT STATE ===
    // Bot uses emerging market balanced strategy recommended by financial advisors:
    // 5% Savings, 25% Deposito, 20% Bonds, 30% Index Fund, 20% Gold
    public decimal BotCashBalance { get; set; } = 5_000_000;
    public decimal BotSavingsBalance { get; set; } = 0;
    public List<DepositoItem> BotDepositos { get; set; } = new();
    public List<BondItem> BotBonds { get; set; } = new();
    public decimal BotIndexFundUnits { get; set; } = 0;
    public decimal BotIndexFundCost { get; set; } = 0;
    public decimal BotGoldUnits { get; set; } = 0;
    public decimal BotGoldCost { get; set; } = 0;
    public int BotEventsPaidFromCash { get; set; } = 0;
    public int BotEventsPaidFromSavings { get; set; } = 0;
    public int BotEventsPaidFromPortfolio { get; set; } = 0;
    public decimal BotTotalEventCostPaid { get; set; } = 0;

    public const decimal UNIT_COST = 1_000_000;
    public const int MAX_YEARS = 10; // Extended to support all unlocks
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
    public decimal BotIndexFundValue => BotIndexFundUnits * AssetPrices.GetValueOrDefault("reksadana", UNIT_COST);
    public decimal BotGoldValue => BotGoldUnits * AssetPrices.GetValueOrDefault("emas", UNIT_COST);
    public decimal BotTotalDepositoValue => BotDepositos.Sum(d => d.CurrentValue);
    public decimal BotTotalBondValue => BotBonds.Sum(b => b.CurrentValue);
    public decimal BotNetWorth => BotCashBalance + BotSavingsBalance + BotIndexFundValue + BotGoldValue + BotTotalDepositoValue + BotTotalBondValue;

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
            Portfolio = Portfolio.Values.ToList(),
            Depositos = Depositos.ToList(),
            Bonds = Bonds.ToList(),
            AvailableStocks = AvailableStocks.ToList(),
            AvailableCryptos = AvailableCryptos.ToList(),
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
            ActiveEvent = AgeMode == AgeMode.Kids ? ActiveEvent?.Title : ActiveEvent?.TitleAdult,
            ActiveEventDescription = AgeMode == AgeMode.Kids ? ActiveEvent?.Description : ActiveEvent?.DescriptionAdult,
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
            PlayerPortfolioPercent = playerPortfolioPct
        };
    }

    private BotState ToBotState()
    {
        var totalInvested = 5_000_000m; // Initial cash (matched to player starting amount)
        var indexFundValue = BotIndexFundValue;
        var goldValue = BotGoldValue;
        var depositoValue = BotTotalDepositoValue;
        var bondValue = BotTotalBondValue;
        var botNetWorth = BotNetWorth;

        // Calculate portfolio percentages for pie chart
        var cashPct = botNetWorth > 0 ? (BotCashBalance / botNetWorth) * 100 : 0;
        var savingsPct = botNetWorth > 0 ? (BotSavingsBalance / botNetWorth) * 100 : 0;
        var depositoPct = botNetWorth > 0 ? (depositoValue / botNetWorth) * 100 : 0;
        var bondPct = botNetWorth > 0 ? (bondValue / botNetWorth) * 100 : 0;
        var indexPct = botNetWorth > 0 ? (indexFundValue / botNetWorth) * 100 : 0;
        var goldPct = botNetWorth > 0 ? (goldValue / botNetWorth) * 100 : 0;

        return new BotState
        {
            BotName = "Financial Advisor Bot",
            Strategy = "Emerging Market Balanced",
            CashBalance = BotCashBalance,
            SavingsBalance = BotSavingsBalance,
            Depositos = BotDepositos.ToList(),
            Bonds = BotBonds.ToList(),
            IndexFundUnits = BotIndexFundUnits,
            IndexFundCost = BotIndexFundCost,
            GoldUnits = BotGoldUnits,
            GoldCost = BotGoldCost,
            IndexFundValue = indexFundValue,
            GoldValue = goldValue,
            TotalDepositoValue = depositoValue,
            TotalBondValue = bondValue,
            NetWorth = botNetWorth,
            TotalInvested = totalInvested + (CurrentYear - 1) * YEARLY_INCOME, // Including yearly income received
            TotalProfit = botNetWorth - totalInvested - (CurrentYear - 1) * YEARLY_INCOME,
            ProfitPercent = totalInvested > 0 ? ((botNetWorth - totalInvested - (CurrentYear - 1) * YEARLY_INCOME) / (totalInvested + (CurrentYear - 1) * YEARLY_INCOME)) * 100 : 0,
            // Portfolio percentages for pie chart
            CashPercent = cashPct,
            SavingsPercent = savingsPct,
            DepositoPercent = depositoPct,
            BondPercent = bondPct,
            IndexFundPercent = indexPct,
            GoldPercent = goldPct,
            // Event tracking
            EventsPaidFromCash = BotEventsPaidFromCash,
            EventsPaidFromSavings = BotEventsPaidFromSavings,
            EventsPaidFromPortfolio = BotEventsPaidFromPortfolio,
            TotalEventCostPaid = BotTotalEventCostPaid,
            TargetAllocation = "5% Savings, 25% Deposito, 20% Bonds, 30% Index Fund, 20% Gold"
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
