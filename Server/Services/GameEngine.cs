using InvestmentGame.Shared.Models;
using Microsoft.Extensions.Logging;

namespace InvestmentGame.Server.Services;

/// <summary>
/// Holds shared market state for a multiplayer room so all players see the same prices.
/// Uses a seeded Random to produce deterministic price sequences per (year, month).
/// </summary>
public class RoomMarketState
{
    public int SharedSeed { get; set; }
    public List<StockInfo> AvailableStocks { get; set; } = new();
    public List<IndexInfo> AvailableIndices { get; set; } = new();
    public List<CryptoInfo> AvailableCryptos { get; set; } = new();
    public List<CrowdfundingProject> AvailableCrowdfunding { get; set; } = new();
    public Dictionary<string, decimal> InitialPrices { get; set; } = new();
    public Dictionary<int, int> EventMonthPerYear { get; set; } = new(); // year → month (7-10)
    public Dictionary<int, int> EventIndexPerYear { get; set; } = new(); // year → event list index
    public Dictionary<int, decimal> EventCostPerYear { get; set; } = new(); // year → cost

    /// <summary>
    /// Get a deterministic Random for a given year+month, so all players see the same prices.
    /// </summary>
    public Random GetRandomForMonth(int year, int month)
    {
        return new Random(SharedSeed ^ (year * 100 + month));
    }
}

public class GameEngine
{
    private readonly Dictionary<string, AssetDefinition> _assets;
    private readonly List<RandomEvent> _events;
    private List<DepositoRate> _depositoRates;
    private List<BondRate> _bondRates;
    private readonly List<StockInfo> _allStocks;
    private readonly List<CryptoInfo> _allCryptos;
    private readonly List<CrowdfundingProject> _allCrowdfundingProjects;
    private readonly Random _random = new();
    private readonly Dictionary<string, GameSession> _sessions = new();
    private readonly Dictionary<string, RoomMarketState> _roomMarketStates = new(); // roomCode → market state
    private readonly object _lock = new();
    private readonly ILogger<GameEngine> _logger;
    private readonly StockDataService _stockData;
    private readonly GoldDataService _goldData;
    private readonly IndexDataService _indexData;
    private readonly DepositoDataService _depositoData;
    private readonly BondDataService _bondData;
    private readonly CryptoDataService _cryptoData;
    private readonly BehaviorLogService _behaviorLog;

    public GameEngine(ILogger<GameEngine> logger, StockDataService stockData, GoldDataService goldData, IndexDataService indexData, DepositoDataService depositoData, BondDataService bondData, CryptoDataService cryptoData, BehaviorLogService behaviorLog)
    {
        _logger = logger;
        _stockData = stockData;
        _goldData = goldData;
        _indexData = indexData;
        _depositoData = depositoData;
        _bondData = bondData;
        _cryptoData = cryptoData;
        _behaviorLog = behaviorLog;
        _assets = InitializeAssets();
        _events = InitializeEvents();
        _depositoRates = RefreshDepositoRates(1); // Game year 1 = 2006
        _bondRates = RefreshBondRates(1);
        _allStocks = InitializeStocks();
        _allCryptos = InitializeCryptos();
        _allCrowdfundingProjects = InitializeCrowdfundingProjects();
    }

    private Dictionary<string, AssetDefinition> InitializeAssets()
    {
        return new Dictionary<string, AssetDefinition>
        {
            // === SAVINGS ACCOUNT - Unlocked at month 0 ===
            // Based on mechanics: risk_level=1, interest 0.5%-6%, tax 20%, daily balance calculation
            ["tabungan"] = new AssetDefinition
            {
                Type = "tabungan",
                Category = "savings",
                DisplayName = "Tabungan",
                DisplayNameAdult = "Tabungan",
                DisplayNameEN = "Savings",
                DisplayNameAdultEN = "Savings Account",
                // Light Mode (Kids) - Simple, no regulatory references
                Description = "Simpanan bank yang aman seperti celengan!",
                DescriptionEN = "A safe place to keep your money that grows!",
                WhatIsIt = "Tabungan itu seperti celengan digital di bank! Kamu taruh uang, lalu bank kasih bonus kecil setiap bulan. Uangmu AMAN BANGET dan bisa diambil kapan saja!",
                WhatIsItEN = "Think of a savings account like a piggy bank at the bank! You put money in, and the bank gives you a small bonus each month. Your money is super safe and you can take it out anytime!",
                RiskExplanation = "SUPER AMAN! Kayak simpan uang di brankas. Uangnya dijamin bertambah terus!",
                RiskExplanationEN = "Super safe! Just like keeping money in a vault. Your money will definitely grow!",
                BestFor = "Buat pemula dan uang darurat!",
                BestForEN = "For beginners and emergency money!",
                ExpectedReturn = "Uangmu pasti nambah sedikit-sedikit setiap bulan!",
                ExpectedReturnEN = "Your money grows a little each month!",
                // Normal Mode (Adult) - Full details with regulations
                DescriptionAdult = "Simpanan bank dengan jaminan keamanan dari pemerintah",
                DescriptionAdultEN = "Bank savings account with government-backed safety guarantee",
                WhatIsItAdult = "Tabungan adalah rekening simpanan di bank yang menghasilkan bunga dengan likuiditas tinggi. Di Indonesia, simpanan hingga Rp 2 miliar dijamin oleh LPS (Lembaga Penjamin Simpanan) sesuai UU No. 24 Tahun 2004.",
                WhatIsItAdultEN = "A savings account is a deposit account at a financial institution that earns interest while keeping your money safe. Balances up to Rp 2 billion are insured by LPS (Deposit Insurance Corporation) under Indonesian law.",
                RiskExplanationAdult = "Risiko sangat rendah. Pokok dijamin LPS hingga Rp 2 miliar dengan syarat 3T (Tercatat, Tingkat bunga wajar, Tidak merugikan bank). Bunga dipotong pajak 20%.",
                RiskExplanationAdultEN = "Very low risk. Principal guaranteed by LPS up to Rp 2 billion subject to 3T requirements. Interest is taxed at 20%.",
                BestForAdult = "Dana darurat, tabungan jangka pendek, dan pemula yang belajar menabung.",
                BestForAdultEN = "Emergency funds, short-term savings, and beginners learning to save.",
                ExpectedReturnAdult = "Bunga: 0.5% - 6% per tahun (setelah pajak 20%)",
                ExpectedReturnAdultEN = "Interest rate: 0.5% - 6% p.a. (after 20% tax)",
                RealRules = "Diawasi OJK. Dijamin LPS hingga Rp 2 miliar. Bunga dihitung harian, dibayar bulanan. Pajak bunga 20%.",
                BasePrice = 1_000_000,
                MinReturn = 0.0004m, // ~0.5% annual / 12
                MaxReturn = 0.005m,  // ~6% annual / 12
                AlwaysPositive = true,
                RiskLevel = "Sangat Rendah",
                UnlockYear = 1,
                UnlockMonth = 1,
                MinimumInvestment = 100_000,
                IsFixedIncome = true
            },

            // === CERTIFICATE OF DEPOSIT - Unlocked at month 6 ===
            // Based on mechanics: risk_level=1, tenors 1-24 months, rates 2.5%-6%, tax 20%, early withdrawal penalty
            ["deposito"] = new AssetDefinition
            {
                Type = "deposito",
                Category = "deposito",
                DisplayName = "Deposito",
                DisplayNameAdult = "Deposito Berjangka",
                DisplayNameEN = "Fixed Deposit",
                DisplayNameAdultEN = "Certificate of Deposit",
                // Light Mode (Kids) - Simple, no regulatory references
                Description = "Tabungan terkunci dengan bunga lebih tinggi!",
                DescriptionEN = "A locked savings account that earns more interest!",
                WhatIsIt = "Deposito itu kayak menabung tapi dikunci! Kamu taruh uang untuk waktu tertentu (1-24 bulan), dan dapat bunga lebih besar dari tabungan biasa! Makin lama dikunci, makin besar hadiahnya! Ada juga Deposito Syariah yang pakai sistem bagi hasil (nisbah), bukan bunga!",
                WhatIsItEN = "A fixed deposit is like saving in a locked box! You put money in for a specific time (1-24 months), and you get bigger bonuses than regular savings! The longer you lock it, the bigger the reward! There's also Shariah Deposits that use profit-sharing (nisbah), not interest!",
                RiskExplanation = "AMAN! Tapi uangmu dikunci. Kalau diambil sebelum waktunya, bonus bunganya berkurang!",
                RiskExplanationEN = "Safe! But your money is locked. If you take it early, you lose some of your bonus!",
                BestFor = "Uang yang nggak akan dipakai dalam beberapa bulan!",
                BestForEN = "Money you won't need for several months!",
                ExpectedReturn = "Bunga 2.5% - 6% per tahun, lebih besar dari tabungan biasa!",
                ExpectedReturnEN = "Interest 2.5% - 6% per year, more than regular savings!",
                // Normal Mode (Adult) - Full details with regulations
                DescriptionAdult = "Deposito berjangka dengan bunga tetap yang dijamin",
                DescriptionAdultEN = "Fixed-term deposit offering guaranteed interest returns",
                WhatIsItAdult = "Deposito Berjangka adalah simpanan berjangka dengan bunga tetap untuk tenor tertentu (1-24 bulan). Dijamin LPS hingga Rp 2 miliar. Tersedia dalam dua jenis:\n\n• Konvensional (BRI): Menggunakan sistem bunga tetap.\n• Syariah (Bank Muamalat): Menggunakan akad Mudharabah Mutlaqah — bukan bunga, melainkan bagi hasil (nisbah) antara nasabah dan bank. Disertifikasi halal oleh DSN-MUI. Diawasi OJK.",
                WhatIsItAdultEN = "A Certificate of Deposit (CD) is a time deposit for specified terms (1-24 months). Guaranteed by LPS up to Rp 2 billion. Available in two types:\n\n• Conventional (BRI): Fixed interest rate system.\n• Shariah (Bank Muamalat): Uses Mudharabah Mutlaqah contract — profit-sharing (nisbah) between depositor and bank, NOT interest. The nisbah ratio determines the depositor's share of bank profits. Certified halal by DSN-MUI. Supervised by OJK.",
                RiskExplanationAdult = "Risiko sangat rendah. Pokok dan bunga dijamin LPS. Pencairan dini dikenakan penalti (kehilangan bunga + 1% dari pokok). Bunga dipotong pajak 20%.",
                RiskExplanationAdultEN = "Very low risk. Principal and interest guaranteed by LPS. Early withdrawal incurs penalty (forfeited interest + 1% of principal). Interest taxed at 20%.",
                BestForAdult = "Tabungan jangka menengah dengan return yang pasti. Ideal untuk dana yang tidak akan digunakan dalam periode tertentu.",
                BestForAdultEN = "Medium-term savings with predictable returns. Ideal for planned expenses.",
                ExpectedReturnAdult = "Bunga: 2.5% - 6% per tahun tergantung tenor (setelah pajak 20%)",
                ExpectedReturnAdultEN = "Interest: 2.5% - 6% p.a. depending on term (after 20% tax)",
                RealRules = "Diawasi OJK. Dijamin LPS. Tenor: 1, 3, 6, 12, 24 bulan (Syariah: 1, 3, 6, 12 bulan). Minimum Rp 1 juta. Pajak bunga/bagi hasil 20%. Opsi ARO tersedia. Syariah: akad Mudharabah Mutlaqah, nisbah bervariasi per tenor, disertifikasi DSN-MUI.",
                BasePrice = 1_000_000,
                MinReturn = 0,
                MaxReturn = 0,
                AlwaysPositive = true,
                RiskLevel = "Rendah",
                UnlockYear = 1,
                UnlockMonth = 6,
                MinimumInvestment = 1_000_000,
                IsFixedIncome = true
            },

            // === INDEX FUND - Unlocked at year 2 ===
            // Based on mechanics: risk_level=3, types: money market to equity, NOT covered by LPS, OJK supervised
            ["reksadana"] = new AssetDefinition
            {
                Type = "reksadana",
                Category = "index",
                DisplayName = "Reksa Dana Indeks",
                DisplayNameAdult = "Reksa Dana Indeks",
                DisplayNameEN = "Index Fund",
                DisplayNameAdultEN = "Index Fund (Mutual Fund)",
                // Light Mode (Kids) - Simple, no regulatory references
                Description = "Investasi yang mengikuti pasar saham!",
                DescriptionEN = "An investment that follows the stock market!",
                WhatIsIt = "Reksa Dana Indeks itu kayak beli sedikit-sedikit dari BANYAK perusahaan sekaligus! Dikelola ahlinya, jadi kamu tinggal santai! Seperti beli sepotong pizza daripada satu pizza utuh!",
                WhatIsItEN = "An index fund is like buying a little bit of LOTS of companies at the same time! Experts manage it for you, so you can relax! It's like buying a pizza slice instead of the whole pizza!",
                RiskExplanation = "Bisa naik-turun, tapi lebih stabil karena terdiri dari banyak perusahaan! Berbeda dari tabungan, nilainya bisa turun kadang-kadang.",
                RiskExplanationEN = "Goes up and down, but more stable because it has many companies! Unlike savings, the value can go down sometimes.",
                BestFor = "Kamu yang mau investasi tapi nggak mau pusing pilih saham satu-satu!",
                BestForEN = "You who want to invest but don't want to pick individual stocks!",
                ExpectedReturn = "Rata-rata naik 8-12% per tahun mengikuti pasar, tapi kadang turun juga!",
                ExpectedReturnEN = "Usually grows 8-12% per year with the market, but sometimes goes down too!",
                // Normal Mode (Adult) - Full details with regulations
                DescriptionAdult = "Investasi pasif yang mengikuti pergerakan indeks pasar saham",
                DescriptionAdultEN = "Low-cost passive investment tracking market index performance",
                WhatIsItAdult = "Reksa Dana Indeks adalah reksa dana yang mengikuti indeks pasar (IHSG, LQ45, IDX30). Diawasi OJK, aset disimpan di Bank Kustodian terpisah dari Manajer Investasi. PENTING: TIDAK dijamin LPS karena bukan produk perbankan.",
                WhatIsItAdultEN = "An index fund is a mutual fund tracking a market index (IHSG, LQ45, IDX30). Supervised by OJK with assets held by Custodian Bank separate from Investment Manager. IMPORTANT: NOT covered by LPS as it's not a banking product.",
                RiskExplanationAdult = "Risiko menengah. Return mengikuti kinerja pasar. Diversifikasi mengurangi risiko saham individual. NAV dihitung harian. Tidak ada jaminan pemerintah.",
                RiskExplanationAdultEN = "Moderate risk. Returns follow market performance. Diversification reduces individual stock risk. Daily NAV calculation. No government guarantee.",
                BestForAdult = "Investor jangka panjang yang mencari return pasar dengan usaha minimal dan biaya rendah. Dijual melalui APERD (Agen Penjual Efek Reksa Dana).",
                BestForAdultEN = "Long-term investors seeking market returns with minimal effort and lower fees. Sold through licensed selling agents (APERD).",
                ExpectedReturnAdult = "Return historis: 8-12% per tahun, bisa -25% hingga +35% di tahun volatil. Biaya: subscription 0-2%, redemption 0-2%, management 0.1-2.5%/tahun.",
                ExpectedReturnAdultEN = "Historical return: 8-12% p.a., can range -25% to +35% in volatile years. Fees: subscription 0-2%, redemption 0-2%, management 0.1-2.5%/year.",
                RealRules = "Diawasi OJK. TIDAK dijamin LPS. Minimum Rp 10.000. Settlement: beli T+1, jual T+7. Pajak gain 0%.",
                BasePrice = 1_000_000,
                MinReturn = -0.03m,
                MaxReturn = 0.04m,
                AlwaysPositive = false,
                RiskLevel = "Sedang",
                UnlockYear = 2,
                UnlockMonth = 1,
                MinimumInvestment = 100_000,
                IsFixedIncome = false
            },

            // === GOVERNMENT BONDS - Unlocked at year 3 ===
            // Based on mechanics: risk_level=1, 100% government guarantee, ORI/SBR/SR/ST variants, coupon 5.5-7%
            ["obligasi"] = new AssetDefinition
            {
                Type = "obligasi",
                Category = "bond",
                DisplayName = "Obligasi Negara",
                DisplayNameAdult = "Surat Berharga Negara",
                DisplayNameEN = "Government Bonds",
                DisplayNameAdultEN = "Government Bonds (Retail SBN)",
                // Light Mode (Kids) - Simple, no regulatory references
                Description = "Pinjamkan uangmu ke negara, dapat bunga tiap bulan!",
                DescriptionEN = "Lend money to the government, earn interest every month!",
                WhatIsIt = "Bayangin kamu minjemin uang ke Pak Presiden buat bangun jalan & sekolah! Tiap bulan, negara kasih kamu uang terima kasih (namanya kupon)! Ada juga Sukuk (obligasi syariah) yang pakai aset nyata, bukan utang — jadi halal!",
                WhatIsItEN = "Imagine lending money to the President to build roads and schools! Every month, the government gives you thank-you money (called coupons)! There's also Sukuk (shariah bonds) that use real assets, not debt — so it's halal!",
                RiskExplanation = "AMAN BANGET! Pemerintah Indonesia yang bayar, dan pemerintah SELALU membayar kembali!",
                RiskExplanationEN = "Super safe! The Indonesian government pays it, and the government ALWAYS pays back!",
                BestFor = "Kamu yang mau dapat uang jajan bulanan dari investasi!",
                BestForEN = "You who want to get monthly pocket money from investing!",
                ExpectedReturn = "Dapat bonus 5-7% per tahun, dibayar tiap bulan dari pemerintah!",
                ExpectedReturnEN = "Get 5-7% bonus per year, paid monthly from the government!",
                // Normal Mode (Adult) - Full details with regulations
                DescriptionAdult = "Surat utang negara dengan kupon tetap yang dijamin 100% oleh pemerintah",
                DescriptionAdultEN = "Government debt securities with fixed coupons, 100% government guaranteed",
                WhatIsItAdult = "Surat Berharga Negara (SBN) Ritel diterbitkan Kementerian Keuangan RI. Tersedia dua jenis:\n\n• ORI (Konvensional): Obligasi ritel dengan kupon tetap, tradeable di pasar sekunder.\n• SR/Sukuk Ritel (Syariah): Surat berharga berbasis aset (akad Ijarah — sale and leaseback). Bukan utang berbunga, melainkan imbal hasil dari aset yang mendasari. Disertifikasi DSN-MUI. Dijamin 100% APBN sama seperti ORI.",
                WhatIsItAdultEN = "Retail Government Bonds (SBN) are issued by the Ministry of Finance. Available in two types:\n\n• ORI (Conventional): Retail bonds with fixed coupons, tradeable in secondary market.\n• SR/Sukuk Ritel (Shariah): Asset-backed securities using Ijarah (sale and leaseback) contract. NOT interest-based debt — returns come from underlying assets. Certified by DSN-MUI. 100% backed by APBN same as ORI.",
                RiskExplanationAdult = "Risiko sangat rendah. Dijamin 100% oleh Pemerintah RI (APBN). Kupon tetap (ORI/SR) atau mengambang dengan floor (SBR/ST). Kupon dipotong pajak 10%.",
                RiskExplanationAdultEN = "Very low risk. 100% guaranteed by Indonesian Government (APBN). Fixed coupon (ORI/SR) or floating with floor (SBR/ST). Coupon taxed at 10%.",
                BestForAdult = "Investor konservatif yang mencari pendapatan rutin bulanan dengan jaminan pemerintah. Harus WNI dengan e-KTP dan SID.",
                BestForAdultEN = "Conservative investors seeking regular monthly income with government guarantee. Must be Indonesian citizen with e-KTP and SID.",
                ExpectedReturnAdult = "Kupon: 5.5% - 7% per tahun, dibayar bulanan (setelah pajak 10%). Tenor: 2-6 tahun.",
                ExpectedReturnAdultEN = "Coupon: 5.5% - 7% p.a., paid monthly (after 10% tax). Tenor: 2-6 years.",
                RealRules = "Diawasi OJK/DJPPR. Dijamin 100% Pemerintah RI. Minimum Rp 1 juta, maksimum Rp 5 miliar. Kupon bulanan. ORI: pajak kupon 10%. SR (Sukuk): akad Ijarah, pajak kupon 15%, fatwa DSN-MUI.",
                BasePrice = 1_000_000,
                MinReturn = 0,
                MaxReturn = 0,
                AlwaysPositive = true,
                RiskLevel = "Rendah",
                UnlockYear = 3,
                UnlockMonth = 1,
                MinimumInvestment = 1_000_000,
                IsFixedIncome = true
            },

            // === INDIVIDUAL STOCKS - Unlocked at year 4 ===
            // Based on mechanics: risk_level=5, lot size 100, T+2 settlement, SIPF protection up to Rp 100M
            ["saham"] = new AssetDefinition
            {
                Type = "saham",
                Category = "stock",
                DisplayName = "Saham",
                DisplayNameAdult = "Saham",
                DisplayNameEN = "Stocks",
                DisplayNameAdultEN = "Individual Stocks",
                // Light Mode (Kids) - Simple, no regulatory references
                Description = "Jadi pemilik perusahaan besar Indonesia!",
                DescriptionEN = "Become a part-owner of a big company!",
                WhatIsIt = "Saham itu kayak BELI SEBAGIAN PERUSAHAAN! Misal beli saham BCA, kamu jadi pemilik kecil Bank BCA! Kalau BCA untung, kamu dapat bagian (dividen)! Seperti naik roller coaster - bisa naik tinggi, bisa turun cepat!",
                WhatIsItEN = "Stocks are like buying a small piece of a big company! If you buy BCA stocks, you become a tiny owner of Bank BCA! When the company makes money, you get a share (dividend)! It's like a roller coaster - can go way up or down fast!",
                RiskExplanation = "HATI-HATI BANGET! Harganya bisa NAIK TINGGI atau TURUN CEPAT dalam sehari! Seperti roller coaster!",
                RiskExplanationEN = "Be super careful! Prices can jump way up or drop really fast in just one day! Like a roller coaster!",
                BestFor = "Buat yang udah paham investasi dan siap deg-degan naik roller coaster!",
                BestForEN = "For people who understand investing and are ready for the roller coaster ride!",
                ExpectedReturn = "Bisa dapat BANYAK (bahkan 2x lipat), tapi bisa juga rugi setengahnya! Plus dapat dividen kalau perusahaan untung!",
                ExpectedReturnEN = "Can make a LOT (even double), but can also lose half! Plus get dividends if company is profitable!",
                // Normal Mode (Adult) - Full details with regulations
                DescriptionAdult = "Kepemilikan ekuitas di perusahaan publik yang terdaftar di bursa",
                DescriptionAdultEN = "Equity ownership in publicly traded companies for capital appreciation and dividends",
                WhatIsItAdult = "Saham adalah kepemilikan di perusahaan publik yang terdaftar di Bursa Efek Indonesia (BEI). Diawasi OJK, dilindungi SIPF (Securities Investor Protection Fund) hingga Rp 100 juta. Return dari capital gain dan dividen.",
                WhatIsItAdultEN = "Stocks represent ownership in companies listed on Indonesia Stock Exchange (IDX). Supervised by OJK, protected by SIPF (Securities Investor Protection Fund) up to Rp 100 million. Returns come from capital gains and dividends.",
                RiskExplanationAdult = "Risiko tinggi. Harga volatil, bisa berfluktuasi 35% dalam sehari (auto rejection limit). SIPF melindungi dari kegagalan broker, BUKAN dari kerugian pasar. Risiko saham individual lebih tinggi dari reksa dana.",
                RiskExplanationAdultEN = "High risk. Prices are volatile, can fluctuate up to 35% daily (auto rejection limit). SIPF protects against broker failure, NOT market losses. Individual stock risk exceeds diversified funds.",
                BestForAdult = "Investor berpengalaman dengan toleransi risiko tinggi dan kemampuan menganalisis perusahaan. Butuh rekening efek di broker berlisensi.",
                BestForAdultEN = "Experienced investors with high risk tolerance who can analyze companies. Requires securities account at licensed broker.",
                ExpectedReturnAdult = "Return sangat bervariasi. Rata-rata historis: 10-15%/tahun, bisa -50% hingga +100%. Dividen 2-10%/tahun. Pajak dividen 10%, pajak jual 0.1%.",
                ExpectedReturnAdultEN = "Returns highly variable. Historical average: 10-15%/year, can range -50% to +100%. Dividends 2-10%/year. Dividend tax 10%, sell tax 0.1%.",
                RealRules = "Diawasi OJK. Dilindungi SIPF hingga Rp 100 juta. 1 lot = 100 lembar. Trading 09:00-16:00 WIB. Settlement T+2. Fee beli ~0.15%, jual ~0.25% + pajak 0.1%.",
                BasePrice = 1_000_000,
                MinReturn = -0.10m,
                MaxReturn = 0.15m,
                AlwaysPositive = false,
                RiskLevel = "Tinggi",
                UnlockYear = 4,
                UnlockMonth = 1,
                MinimumInvestment = 100_000,
                IsFixedIncome = false
            },

            // === GOLD - Unlocked at year 5 ===
            // Based on mechanics: risk_level=2, digital/physical, 1:1 physical backing, BAPPEBTI/OJK regulated
            ["emas"] = new AssetDefinition
            {
                Type = "emas",
                Category = "gold",
                DisplayName = "Emas",
                DisplayNameAdult = "Investasi Emas",
                DisplayNameEN = "Gold",
                DisplayNameAdultEN = "Gold Investment",
                // Light Mode (Kids) - Simple, no regulatory references
                Description = "Beli emas yang berkilau dan berharga!",
                DescriptionEN = "Buy shiny gold that's valuable!",
                WhatIsIt = "Emas itu logam kuning berkilau yang SEMUA ORANG DI DUNIA mau! Dari zaman nenek moyang, raja dan ratu pakai emas. Sampai sekarang, emas selalu berharga! Seperti harta karun yang nilainya bertahan ribuan tahun!",
                WhatIsItEN = "Gold is a shiny yellow metal that EVERYONE in the world wants! Since ancient kings and queens wore it. Even today, gold is always valuable! Like a treasure that has kept its value for thousands of years!",
                RiskExplanation = "Cukup aman! Harga emas bisa naik-turun, tapi lebih stabil dari saham. Seperti perosotan pelan, bukan roller coaster.",
                RiskExplanationEN = "Pretty safe! Gold prices can go up and down, but more stable than stocks. Like a gentle slide, not a roller coaster.",
                BestFor = "Buat yang SABAR! Emas cocok disimpan lama seperti kata nenek: 'Simpan emas untuk hari hujan!'",
                BestForEN = "For patient people! Gold is great to save for a long time, like grandma says: 'Save gold for rainy days!'",
                ExpectedReturn = "Bisa naik 8-15% per tahun kalau sabar, tapi kadang turun sedikit juga!",
                ExpectedReturnEN = "Can go up 8-15% per year if you're patient, but sometimes goes down a little too!",
                // Normal Mode (Adult) - Full details with regulations
                DescriptionAdult = "Investasi emas fisik atau digital sebagai lindung nilai inflasi",
                DescriptionAdultEN = "Physical or digital gold investment for inflation hedging and wealth preservation",
                WhatIsItAdult = "Investasi emas tersedia dalam bentuk digital (didukung 1:1 emas fisik) atau batangan fisik (Antam). Diawasi BAPPEBTI/OJK (POJK No. 17/2024). Harga mengikuti harga emas dunia yang dikonversi ke IDR. Platform harus menyimpan minimal 10.000 gram emas sebelum berdagang.",
                WhatIsItAdultEN = "Gold investment is available as digital (1:1 physical backing) or physical bars (Antam). Regulated by BAPPEBTI/OJK (POJK No. 17/2024). Prices track London Gold Fix converted to IDR. Platforms must store minimum 10,000 grams before trading.",
                RiskExplanationAdult = "Risiko rendah-menengah. Harga berfluktuasi mengikuti pasar global dan kurs USD/IDR. Lindung nilai inflasi yang baik. Aset dijamin emas fisik di depository berlisensi.",
                RiskExplanationAdultEN = "Low-medium risk. Prices fluctuate with global markets and USD/IDR rate. Good inflation hedge. Assets backed by physical gold in licensed depository.",
                BestForAdult = "Investor jangka panjang yang mencari lindung nilai inflasi dan diversifikasi portofolio. Platform berlisensi: Treasury, IndoGold, Pluang, Pegadaian.",
                BestForAdultEN = "Long-term investors seeking inflation hedge and portfolio diversification. Licensed platforms: Treasury, IndoGold, Pluang, Pegadaian.",
                ExpectedReturnAdult = "Return historis: 8-15%/tahun dalam IDR, bisa -10% hingga +25%. Spread beli-jual 1.5-3%. Pajak: PPh22 beli 0.25%, jual 1.5% (dengan NPWP).",
                ExpectedReturnAdultEN = "Historical return: 8-15%/year in IDR, can range -10% to +25%. Buy-sell spread 1.5-3%. Tax: PPh22 buy 0.25%, sell 1.5% (with NPWP).",
                RealRules = "Diawasi BAPPEBTI/OJK. Emas digital minimal Rp 5.000 (0.0001 gram). Didukung 1:1 emas fisik. Bisa ditebus jadi emas batangan (min 1 gram, fee 2%).",
                BasePrice = 1_200_000, // Current real gold price ~Rp 1.2M per gram (as of 2024)
                MinReturn = -0.002m, // ~-2% annual / 12
                MaxReturn = 0.012m, // ~15% annual / 12
                AlwaysPositive = false,
                RiskLevel = "Sedang",
                UnlockYear = 5,
                UnlockMonth = 1,
                MinimumInvestment = 50_000,
                IsFixedIncome = false
            },

            // === CRYPTO - Unlocked at year 12 (calendar 2017, when real data begins) ===
            // Based on mechanics: risk_level=5, OJK regulated since Jan 2025, NO guarantee, 24/7 trading
            ["crypto"] = new AssetDefinition
            {
                Type = "crypto",
                Category = "crypto",
                DisplayName = "Crypto",
                DisplayNameAdult = "Aset Kripto",
                DisplayNameEN = "Crypto",
                DisplayNameAdultEN = "Cryptocurrency",
                // Light Mode (Kids) - Simple, no regulatory references
                Description = "Uang digital masa depan - sangat berisiko!",
                DescriptionEN = "Digital money of the future - very risky!",
                WhatIsIt = "Crypto itu UANG DIGITAL yang hidup di internet! Bitcoin, Ethereum, dll. Bisa trading 24 jam nonstop! Seperti mengumpulkan kartu trading digital langka yang nilainya bisa berubah SUPER cepat!",
                WhatIsItEN = "Crypto is DIGITAL MONEY that lives on the internet! Bitcoin, Ethereum, and more. You can trade 24/7! Like collecting rare digital trading cards whose value can change SUPER fast!",
                RiskExplanation = "⚠️ BAHAYA LEVEL MAX! Bisa NAIK 2x lipat atau TURUN 80% dalam sebulan! Seperti roller coaster paling menyeramkan! TIDAK ADA perlindungan kalau hilang!",
                RiskExplanationEN = "⚠️ MAXIMUM DANGER! Can go up 2x or drop 80% in just one month! Like the scariest roller coaster! NO protection if you lose money!",
                BestFor = "⚠️ HANYA buat yang punya UANG LEBIH yang SIAP HILANG SEPENUHNYA! Banyak orang dewasa kehilangan segalanya di crypto!",
                BestForEN = "⚠️ ONLY for people with EXTRA money they're ready to COMPLETELY LOSE! Many adults have lost everything in crypto!",
                ExpectedReturn = "GILA-GILAAN! Bisa untung 10x lipat, bisa juga rugi semuanya!",
                ExpectedReturnEN = "Crazy! Can make 10x profit, or lose everything!",
                // Normal Mode (Adult) - Full details with regulations
                DescriptionAdult = "Aset keuangan digital berbasis teknologi blockchain - risiko sangat tinggi",
                DescriptionAdultEN = "Digital financial assets based on blockchain technology - very high risk",
                WhatIsItAdult = "Aset Kripto adalah aset keuangan digital berbasis blockchain. Sejak Januari 2025 diawasi OJK (dipindahkan dari BAPPEBTI) berdasarkan POJK No. 27/2024. TIDAK ADA jaminan pemerintah. Harus menggunakan exchange berlisensi OJK (Indodax, Tokocrypto, Pintu, dll).",
                WhatIsItAdultEN = "Cryptocurrency is a digital financial asset based on blockchain. Since January 2025, regulated by OJK (transferred from BAPPEBTI) under POJK No. 27/2024. NO government guarantee. Must use OJK-licensed exchanges (Indodax, Tokocrypto, Pintu, etc.).",
                RiskExplanationAdult = "⚠️ Risiko SANGAT TINGGI. Sangat volatil dengan potensi keuntungan besar atau kehilangan total. TIDAK ADA perlindungan pemerintah. Exchange bisa diretas. Hanya aset di Daftar Aset Kripto yang disetujui.",
                RiskExplanationAdultEN = "⚠️ VERY HIGH risk. Highly volatile with potential for massive gains or TOTAL loss. NO government protection. Exchanges can be hacked. Only assets on approved Crypto Asset List.",
                BestForAdult = "Investor spekulatif dengan toleransi risiko sangat tinggi. Disarankan maksimal 5-10% dari total portofolio. Siap kehilangan 100% investasi.",
                BestForAdultEN = "Speculative investors with very high risk tolerance. Recommended maximum 5-10% of total portfolio. Be prepared to lose 100% of investment.",
                ExpectedReturnAdult = "Sangat bervariasi: -80% hingga +300% per tahun (BTC/ETH), altcoin bisa lebih ekstrem. Tidak ada jaminan return. Pajak: PPN 0.11%, PPh 0.1%.",
                ExpectedReturnAdultEN = "Extremely variable: -80% to +300% annually (BTC/ETH), altcoins can be more extreme. No guaranteed returns. Tax: VAT 0.11%, Income 0.1%.",
                RealRules = "Diawasi OJK sejak Jan 2025 (POJK No. 27/2024). TIDAK DIJAMIN pemerintah. Trading 24/7. Exchange berlisensi: Indodax, Tokocrypto, Pintu, Rekeningku. Pajak: PPN 0.11%, PPh 0.1%.",
                BasePrice = 1_000_000,
                MinReturn = -0.20m,
                MaxReturn = 0.30m,
                AlwaysPositive = false,
                RiskLevel = "Sangat Tinggi",
                UnlockYear = 12,
                UnlockMonth = 1,
                MinimumInvestment = 50_000,
                IsFixedIncome = false
            },

            // === CROWD FUNDING - Unlocked at year 6 ===
            // Based on mechanics: risk_level=4, OJK regulated, investor limits based on income, 3-year typical holding
            ["crowdfunding"] = new AssetDefinition
            {
                Type = "crowdfunding",
                Category = "crowdfunding",
                DisplayName = "Urun Dana",
                DisplayNameAdult = "Securities Crowdfunding",
                DisplayNameEN = "Crowdfunding",
                DisplayNameAdultEN = "Equity Crowdfunding",
                // Light Mode (Kids) - Simple, no regulatory references
                Description = "Patungan modal untuk bisnis UMKM!",
                DescriptionEN = "Pooling money together to help small businesses!",
                WhatIsIt = "Crowd Funding itu patungan banyak orang untuk modal bisnis kecil! Kamu bisa jadi pemilik bagian dari bisnis mereka! Seperti jadi pendukung awal warung es temanmu - kalau sukses, kamu dapat bagian keuntungan!",
                WhatIsItEN = "Crowdfunding is when many people pool money to help small businesses grow! You become a part-owner! Like being an early supporter of your friend's lemonade stand - if it succeeds, you share the profits!",
                RiskExplanation = "⚠️ BERISIKO TINGGI! Kebanyakan bisnis baru GAGAL. Uangmu bisa hilang semuanya! Tapi kadang-kadang, hanya kadang-kadang, kamu menemukan berlian!",
                RiskExplanationEN = "⚠️ VERY RISKY! Most new businesses FAIL. You can lose ALL your money! But sometimes, just sometimes, you find a diamond!",
                BestFor = "Kamu yang mau dukung bisnis lokal dan SIAP kehilangan semua uang investasi!",
                BestForEN = "For people who want to support local businesses and are READY to lose all invested money!",
                ExpectedReturn = "Kalau bisnisnya sukses besar, bisa untung 10x lipat! Tapi kebanyakan gagal, uangmu hilang.",
                ExpectedReturnEN = "If business is super successful, can make 10x profit! But most fail, and you lose your money.",
                // Normal Mode (Adult) - Full details with regulations
                DescriptionAdult = "Investasi ekuitas di startup dan UMKM melalui platform urun dana",
                DescriptionAdultEN = "Equity investment in startups and SMEs through securities crowdfunding platforms",
                WhatIsItAdult = "Securities Crowdfunding (Urun Dana) memungkinkan investor membeli saham di startup dan UMKM melalui platform berlisensi OJK (POJK No. 17/2025). Jenis efek: saham, sukuk, obligasi. TIDAK ADA jaminan pemerintah.",
                WhatIsItAdultEN = "Securities Crowdfunding allows investors to purchase shares in startups and SMEs through OJK-licensed platforms (POJK No. 17/2025). Securities types: equity, sukuk, bonds. NO government guarantee.",
                RiskExplanationAdult = "⚠️ Risiko tinggi. Tingkat kegagalan startup >50%. Investasi tidak likuid dengan lock-up period 1-3 tahun. Pasar sekunder sangat terbatas (diskon ~20%). Bisa kehilangan 100%.",
                RiskExplanationAdultEN = "⚠️ High risk. Startup failure rate >50%. Illiquid investment with 1-3 year lock-up. Very limited secondary market (~20% discount). Can lose 100%.",
                BestForAdult = "Investor dengan toleransi risiko tinggi yang mencari potensi pertumbuhan tinggi. Batas investasi: 5% pendapatan tahunan (<Rp 500 juta) atau 10% (>Rp 500 juta).",
                BestForAdultEN = "Investors with high risk tolerance seeking high-growth potential. Investment limit: 5% annual income (<Rp 500M) or 10% (>Rp 500M).",
                ExpectedReturnAdult = "Sangat bervariasi. Potensi 2-10x jika sukses (IPO/akuisisi). Dividen 5-20% jika untung. Tapi banyak yang rugi total (kebangkrutan 20%).",
                ExpectedReturnAdultEN = "Highly variable. Potential 2-10x if successful (IPO/acquisition). Dividends 5-20% if profitable. But many result in total loss (20% bankruptcy rate).",
                RealRules = "Diawasi OJK (POJK No. 17/2025). TIDAK DIJAMIN pemerintah. Platform harus punya modal Rp 25 miliar. Minimum investasi Rp 100.000. Batas tahunan penerbit Rp 10 miliar.",
                BasePrice = 1_000_000,
                MinReturn = -0.10m,
                MaxReturn = 0.08m,
                AlwaysPositive = false,
                RiskLevel = "Tinggi",
                UnlockYear = 6,
                UnlockMonth = 6,
                MinimumInvestment = 100_000,
                IsFixedIncome = false
            }
        };
    }

    private List<DepositoRate> RefreshDepositoRates(int gameYear)
    {
        var rates = new List<DepositoRate>();

        // Conventional (BRI): 5 tenors
        int[] convTenors = { 1, 3, 6, 12, 24 };
        foreach (var tenor in convTenors)
        {
            var rate = _depositoData.GetConventionalRate(gameYear, tenor);
            if (rate == null) continue;
            rates.Add(new DepositoRate
            {
                PeriodMonths = tenor,
                PeriodName = $"{tenor} Bulan",
                AnnualRate = rate.Value,
                PenaltyRate = 0.50m,
                MinimumDeposit = 1_000_000,
                IsShariah = false,
                BankName = "BRI"
            });
        }

        // Shariah (Bank Muamalat): 4 tenors (no 24m)
        int[] shariahTenors = { 1, 3, 6, 12 };
        foreach (var tenor in shariahTenors)
        {
            var rate = _depositoData.GetShariahRate(gameYear, tenor);
            var nisbah = _depositoData.GetShariahNisbah(gameYear, tenor);
            if (rate == null) continue;
            rates.Add(new DepositoRate
            {
                PeriodMonths = tenor,
                PeriodName = $"{tenor} Bulan",
                AnnualRate = rate.Value,
                PenaltyRate = 0.50m,
                MinimumDeposit = 1_000_000,
                IsShariah = true,
                NisbahRatio = nisbah,
                BankName = "Bank Muamalat"
            });
        }

        // Fallback if no data loaded (shouldn't happen with valid JSON)
        if (rates.Count == 0)
        {
            rates.Add(new DepositoRate { PeriodMonths = 12, PeriodName = "12 Bulan", AnnualRate = 0.060m, PenaltyRate = 0.50m, MinimumDeposit = 1_000_000, BankName = "BRI" });
        }

        return rates;
    }

    private List<BondRate> RefreshBondRates(int gameYear)
    {
        var rates = new List<BondRate>();

        // ORI (conventional) - available from Y1 (2006)
        var ori = _bondData.GetORI(gameYear);
        if (ori != null)
        {
            rates.Add(new BondRate
            {
                PeriodMonths = ori.Value.tenorYears * 12,
                PeriodName = $"ORI ({ori.Value.tenorYears} Tahun)",
                BondType = "ORI",
                CouponRate = ori.Value.couponRate,
                MinimumInvestment = 1_000_000,
                IsShariah = false,
                SeriesName = ori.Value.series
            });
        }

        // SR (shariah Sukuk Ritel) - available from Y4 (2009)
        var sr = _bondData.GetSR(gameYear);
        if (sr != null)
        {
            rates.Add(new BondRate
            {
                PeriodMonths = sr.Value.tenorYears * 12,
                PeriodName = $"SR ({sr.Value.tenorYears} Tahun Syariah)",
                BondType = "SR",
                CouponRate = sr.Value.couponRate,
                MinimumInvestment = 1_000_000,
                IsShariah = true,
                SeriesName = sr.Value.series,
                AkadType = sr.Value.akad
            });
        }

        // Fallback if no data
        if (rates.Count == 0)
        {
            rates.Add(new BondRate { PeriodMonths = 36, PeriodName = "ORI (3 Tahun)", BondType = "ORI", CouponRate = 0.060m, MinimumInvestment = 1_000_000, SeriesName = "ORI" });
        }

        return rates;
    }

    private List<StockInfo> InitializeStocks()
    {
        // Full pool of Indonesian stocks with real historical prices from CSV
        // Each game session randomly picks 3 shariah + 1 non-shariah from this pool
        // Game Y4M1 = Calendar Jan 2009 (stocks unlock at Y4)
        // ICBP excluded: data only starts Oct 2010
        var y = 4;
        var m = 1;

        var stocks = new List<(string ticker, string name, string sector, decimal fallback, bool shariah)>
        {
            // Non-shariah
            ("BBCA", "Bank Central Asia", "Banking", 1_700, false),
            ("BBRI", "Bank Rakyat Indonesia", "Banking", 2_200, false),
            ("BMRI", "Bank Mandiri", "Banking", 1_150, false),
            ("BBNI", "Bank Negara Indonesia", "Banking", 460, false),
            ("HMSP", "HM Sampoerna", "Tobacco", 4_000, false),
            ("GGRM", "Gudang Garam", "Tobacco", 2_475, false),
            // Shariah
            ("TLKM", "Telkom Indonesia", "Telecom", 3_100, true),
            ("ASII", "Astra International", "Automotive", 5_600, true),
            ("UNVR", "Unilever Indonesia", "Consumer", 3_750, true),
            ("INDF", "Indofood Sukses", "Consumer", 650, true),
            ("KLBF", "Kalbe Farma", "Pharma", 225, true),
            ("SMGR", "Semen Indonesia", "Materials", 2_400, true),
            ("UNTR", "United Tractors", "Heavy Equipment", 2_550, true),
            ("PGAS", "Perusahaan Gas Negara", "Energy", 1_700, true),
            ("INTP", "Indocement Tunggal", "Materials", 2_425, true),
            ("PTBA", "Bukit Asam", "Mining", 3_050, true),
            ("ADRO", "Adaro Energy", "Mining", 600, true),
            ("JSMR", "Jasa Marga", "Infrastructure", 525, true),
            ("ANTM", "Aneka Tambang", "Mining", 575, true),
        };

        return stocks.Select(s =>
        {
            var price = _stockData.GetPrice(s.ticker, y, m) ?? s.fallback;
            var div = _stockData.GetDividend(s.ticker, y);
            return new StockInfo
            {
                Ticker = s.ticker,
                CompanyName = s.name,
                Sector = s.sector,
                CurrentPrice = price,
                PreviousPrice = price,
                AnnualDividendPerShare = div?.amount ?? 0,
                DividendType = div?.type ?? "None",
                IsShariahCompliant = s.shariah
            };
        }).ToList();
    }

    /// <summary>
    /// Randomly select 4 stocks from the pool: 3 shariah + 1 non-shariah.
    /// Clones the StockInfo objects so each session has its own copies.
    /// </summary>
    private List<StockInfo> SelectRandomStocks(Random rng)
    {
        var shariah = _allStocks.Where(s => s.IsShariahCompliant).OrderBy(_ => rng.Next()).Take(3);
        var nonShariah = _allStocks.Where(s => !s.IsShariahCompliant).OrderBy(_ => rng.Next()).Take(1);

        return shariah.Concat(nonShariah).Select(s => new StockInfo
        {
            Ticker = s.Ticker,
            CompanyName = s.CompanyName,
            Sector = s.Sector,
            CurrentPrice = s.CurrentPrice,
            PreviousPrice = s.PreviousPrice,
            LastPriceUpdateMonth = 0,
            AnnualDividendPerShare = s.AnnualDividendPerShare,
            DividendType = s.DividendType,
            IsShariahCompliant = s.IsShariahCompliant
        }).ToList();
    }

    /// <summary>
    /// Randomly select 2 index funds: 1 conventional (from IHSG/LQ45) + 1 shariah (JII).
    /// Index prices start from game year 2 (calendar 2007) when reksadana unlocks.
    /// </summary>
    private List<IndexInfo> SelectRandomIndices(Random rng)
    {
        var conventional = _indexData.GetConventionalIndices();
        var shariah = _indexData.GetShariahIndices();

        var selectedConv = conventional[rng.Next(conventional.Count)];
        var selectedShariah = shariah[rng.Next(shariah.Count)];

        // Reksadana unlocks at Y2M1 = calendar 2007 January
        var initYear = 2;
        var initMonth = 1;

        var indices = new List<IndexInfo>();

        var convPrice = _indexData.GetPrice(selectedConv, initYear, initMonth) ?? 1000m;
        indices.Add(new IndexInfo
        {
            IndexId = selectedConv,
            DisplayName = selectedConv switch
            {
                "IHSG" => "IHSG (IDX Composite)",
                "LQ45" => "LQ45 (45 Most Liquid)",
                _ => selectedConv
            },
            IsShariah = false,
            CurrentPrice = convPrice,
            PreviousPrice = convPrice,
            PriceHistory = _indexData.GetPriceHistory(selectedConv, initYear, initMonth)
        });

        var shariahPrice = _indexData.GetPrice(selectedShariah, initYear, initMonth) ?? 300m;
        indices.Add(new IndexInfo
        {
            IndexId = selectedShariah,
            DisplayName = selectedShariah switch
            {
                "JII" => "JII (Jakarta Islamic Index)",
                _ => selectedShariah
            },
            IsShariah = true,
            CurrentPrice = shariahPrice,
            PreviousPrice = shariahPrice,
            PriceHistory = _indexData.GetPriceHistory(selectedShariah, initYear, initMonth)
        });

        return indices;
    }

    private List<CryptoInfo> InitializeCryptos()
    {
        // Real historical crypto prices from JSON data (USD→IDR converted)
        // Data starts at game year 12 (calendar 2017)
        var symbols = new[] { "BTC", "XRP", "XLM" };
        var cryptos = new List<CryptoInfo>();
        foreach (var symbol in symbols)
        {
            var price = _cryptoData.GetPrice(symbol, 12, 1) ?? 0; // Y12M1 = Jan 2017
            cryptos.Add(new CryptoInfo
            {
                Symbol = symbol,
                Name = _cryptoData.GetCoinName(symbol),
                CurrentPrice = price,
                PreviousPrice = price
            });
        }
        return cryptos;
    }

    private List<CrowdfundingProject> InitializeCrowdfundingProjects()
    {
        // Real crowdfunding projects from CSV data (Data/CrowdFunding/indonesia_crowdfunding_projects.csv)
        // ROI is annualized, RiskLevel is 1-10 score mapped to failure probability 5%-25%
        return new List<CrowdfundingProject>
        {
            new() { ProjectId = "URD-001", ProjectName = "Proyek Agriculture Modal Kerja 1", ProjectType = "Agriculture", ExpectedReturn = 0.2025m, RiskLevel = 6, LockUpMonths = 6, MinimumInvestment = 100_000, IsActive = true },
            new() { ProjectId = "URD-002", ProjectName = "Proyek Food & Beverage Modal Kerja 2", ProjectType = "Food & Beverage", ExpectedReturn = 0.0993m, RiskLevel = 1, LockUpMonths = 6, MinimumInvestment = 100_000, IsActive = true },
            new() { ProjectId = "URD-003", ProjectName = "Proyek Retail Pengembangan 3", ProjectType = "Retail", ExpectedReturn = 0.1952m, RiskLevel = 4, LockUpMonths = 10, MinimumInvestment = 100_000, IsActive = true },
            new() { ProjectId = "URD-004", ProjectName = "Proyek Manufacturing Ekspansi 4", ProjectType = "Manufacturing", ExpectedReturn = 0.0991m, RiskLevel = 2, LockUpMonths = 5, MinimumInvestment = 100_000, IsActive = true },
            new() { ProjectId = "URD-005", ProjectName = "Proyek Technology Pengembangan 5", ProjectType = "Technology", ExpectedReturn = 0.1717m, RiskLevel = 4, LockUpMonths = 9, MinimumInvestment = 100_000, IsActive = true },
            new() { ProjectId = "URD-006", ProjectName = "Proyek Logistics Investasi 6", ProjectType = "Logistics", ExpectedReturn = 0.2093m, RiskLevel = 5, LockUpMonths = 6, MinimumInvestment = 100_000, IsActive = true },
            new() { ProjectId = "URD-007", ProjectName = "Proyek Education Modal Kerja 7", ProjectType = "Education", ExpectedReturn = 0.1231m, RiskLevel = 3, LockUpMonths = 5, MinimumInvestment = 100_000, IsActive = true },
            new() { ProjectId = "URD-008", ProjectName = "Proyek Healthcare Modal Kerja 8", ProjectType = "Healthcare", ExpectedReturn = 0.1194m, RiskLevel = 1, LockUpMonths = 4, MinimumInvestment = 100_000, IsActive = true },
            new() { ProjectId = "URD-009", ProjectName = "Proyek Renewable Energy Modal Kerja 9", ProjectType = "Renewable Energy", ExpectedReturn = 0.3531m, RiskLevel = 10, LockUpMonths = 21, MinimumInvestment = 100_000, IsActive = true },
            new() { ProjectId = "URD-010", ProjectName = "Proyek Property Pengembangan 10", ProjectType = "Property", ExpectedReturn = 0.1218m, RiskLevel = 3, LockUpMonths = 4, MinimumInvestment = 100_000, IsActive = true },
        };
    }

    private List<RandomEvent> InitializeEvents()
    {
        return new List<RandomEvent>
        {
            new RandomEvent
            {
                Title = "Ada yang Sakit!",
                TitleAdult = "Darurat Medis",
                Description = "Wah, ada keluarga yang sakit dan harus ke rumah sakit! Harus bayar biaya dokter dan obat!",
                DescriptionAdult = "Anggota keluarga membutuhkan rawat inap. Biaya medis tidak sepenuhnya ditanggung asuransi.",
                TitleEN = "Someone is Sick!",
                TitleAdultEN = "Medical Emergency",
                DescriptionEN = "Oh no, a family member is sick and needs to go to the hospital! You have to pay for the doctor and medicine!",
                DescriptionAdultEN = "A family member requires hospitalization. Medical expenses not fully covered by insurance.",
                Cost = 5_000_000,
                Impact = "Kesehatan"
            },
            new RandomEvent
            {
                Title = "Motor/Mobil Rewel!",
                TitleAdult = "Perbaikan Kendaraan",
                Description = "Waduh, motor atau mobil tiba-tiba mogok! Harus ke bengkel dan ganti suku cadang!",
                DescriptionAdult = "Kerusakan besar pada kendaraan yang membutuhkan biaya perbaikan signifikan.",
                TitleEN = "Vehicle Trouble!",
                TitleAdultEN = "Vehicle Repair",
                DescriptionEN = "Oh no, your vehicle suddenly broke down! You need to go to the mechanic and replace parts!",
                DescriptionAdultEN = "Major vehicle breakdown requiring significant repair costs.",
                Cost = 3_000_000,
                Impact = "Transportasi"
            },
            new RandomEvent
            {
                Title = "Atap Bocor!",
                TitleAdult = "Perbaikan Rumah",
                Description = "Hujan deres dan atap rumah bocor! Harus cepat diperbaiki!",
                DescriptionAdult = "Kerusakan atap yang membutuhkan perbaikan segera saat musim hujan.",
                TitleEN = "Leaky Roof!",
                TitleAdultEN = "Home Repair",
                DescriptionEN = "Heavy rain and the roof is leaking! It needs to be fixed right away!",
                DescriptionAdultEN = "Roof damage requiring immediate repair during rainy season.",
                Cost = 7_000_000,
                Impact = "Rumah"
            },
            new RandomEvent
            {
                Title = "Waktunya Sekolah!",
                TitleAdult = "Biaya Pendidikan",
                Description = "Naik kelas! Tapi harus bayar uang pangkal dan beli seragam baru!",
                DescriptionAdult = "Biaya sekolah dan seragam baru untuk tahun ajaran baru.",
                TitleEN = "School Time!",
                TitleAdultEN = "Education Expenses",
                DescriptionEN = "Moving up a grade! But you need to pay school fees and buy new uniforms!",
                DescriptionAdultEN = "School fees and new uniform costs for new academic year.",
                Cost = 4_000_000,
                Impact = "Pendidikan"
            },
            new RandomEvent
            {
                Title = "Bayar Pajak!",
                TitleAdult = "Pembayaran Pajak",
                Description = "Waktunya bayar pajak tahunan! Sebagai warga negara yang baik!",
                DescriptionAdult = "Ditemukan kekurangan bayar pajak tahunan saat pelaporan.",
                TitleEN = "Tax Time!",
                TitleAdultEN = "Tax Payment",
                DescriptionEN = "Time to pay annual taxes! Being a good citizen!",
                DescriptionAdultEN = "Annual tax underpayment discovered during filing.",
                Cost = 2_500_000,
                Impact = "Pajak"
            },
            new RandomEvent
            {
                Title = "Banyak Kondangan!",
                TitleAdult = "Kewajiban Sosial",
                Description = "Wow ada 3 teman nikahan bulan ini! Harus beli kado dan kasih amplop!",
                DescriptionAdult = "Banyak undangan pernikahan yang membutuhkan kado dan sumbangan.",
                TitleEN = "Wedding Season!",
                TitleAdultEN = "Social Obligations",
                DescriptionEN = "Wow, 3 friends are getting married this month! You need to buy gifts and give cash envelopes!",
                DescriptionAdultEN = "Multiple wedding invitations requiring gifts and cash contributions.",
                Cost = 2_000_000,
                Impact = "Sosial"
            },
            new RandomEvent
            {
                Title = "HP Rusak!",
                TitleAdult = "Penggantian Perangkat",
                Description = "Yah HP jatuh dan rusak parah! Harus beli baru!",
                DescriptionAdult = "Smartphone rusak total dan tidak bisa diperbaiki, perlu diganti.",
                TitleEN = "Phone Broken!",
                TitleAdultEN = "Device Replacement",
                DescriptionEN = "Oh no, your phone fell and is badly damaged! You need to buy a new one!",
                DescriptionAdultEN = "Essential smartphone damaged beyond repair, needs replacement.",
                Cost = 3_500_000,
                Impact = "Elektronik"
            },
            new RandomEvent
            {
                Title = "Iuran Komplek!",
                TitleAdult = "Iuran Lingkungan",
                Description = "Waktunya bayar iuran RT, keamanan, dan kebersihan!",
                DescriptionAdult = "Iuran tahunan keamanan dan pemeliharaan lingkungan jatuh tempo.",
                TitleEN = "Community Fees!",
                TitleAdultEN = "Community Fees",
                DescriptionEN = "Time to pay neighborhood, security, and cleaning fees!",
                DescriptionAdultEN = "Annual neighborhood security and maintenance fees due.",
                Cost = 1_500_000,
                Impact = "Lingkungan"
            }
        };
    }

    public Dictionary<string, AssetDefinition> GetAssets() => _assets;
    public List<DepositoRate> GetDepositoRates() => _depositoRates;
    public List<BondRate> GetBondRates() => _bondRates;

    // Allocation % of an asset-class value against total net worth after a trade
    private static decimal AllocationPct(GameSession s, decimal classValue)
    {
        var nw = s.NetWorth;
        return nw > 0 ? classValue / nw * 100m : 0m;
    }

    private static decimal ComputeTotalProfitLoss(GameSession s)
    {
        return s.TotalSavingsInterestEarned
            + s.TotalDepositoInterestEarned + s.Depositos.Sum(d => d.CurrentValue - d.Principal)
            + s.TotalBondCouponEarned
            + s.TotalDividendEarned
            + s.TotalRealizedPortfolioGainLoss + s.Portfolio.Values.Sum(p => p.ProfitLoss)
            + s.TotalRealizedCrowdfundingGainLoss
            + s.CrowdfundingInvestments.Where(c => !c.HasFailed).Sum(c => c.CurrentValue - c.InvestedAmount);
    }

    public GameSession CreateSession(string playerId, string connectionId, AgeMode ageMode, Language language = Language.Indonesian)
    {
        lock (_lock)
        {
            var session = new GameSession
            {
                PlayerId = playerId,
                ConnectionId = connectionId,
                AgeMode = ageMode,
                Language = language,
                ShowIntro = true,
                IntroAssetType = "tabungan"
            };
            session.InitializePrices(_assets);

            // Set per-session rates for game year 1
            session.CurrentDepositoRates = RefreshDepositoRates(1);
            session.CurrentBondRates = RefreshBondRates(1);

            session.EventMonthForYear = _random.Next(7, 11);
            session.EventOccurredThisYear = false;

            // Randomly select 4 stocks: 3 shariah + 1 non-shariah
            session.InitializeStocks(SelectRandomStocks(_random));

            // Randomly select 2 indices: 1 conventional + 1 shariah
            session.AvailableIndices = SelectRandomIndices(_random);

            // Initialize cryptos
            var cryptos = _allCryptos.Select(c => new CryptoInfo
            {
                Symbol = c.Symbol,
                Name = c.Name,
                CurrentPrice = c.CurrentPrice,
                PreviousPrice = c.PreviousPrice
            }).ToList();
            session.InitializeCryptos(cryptos);

            // Initialize crowdfunding projects (select 3 random projects)
            var crowdfunding = _allCrowdfundingProjects.OrderBy(_ => _random.Next()).Take(3).Select(p => new CrowdfundingProject
            {
                ProjectId = p.ProjectId,
                ProjectName = p.ProjectName,
                ProjectType = p.ProjectType,
                MinimumInvestment = p.MinimumInvestment,
                ExpectedReturn = p.ExpectedReturn,
                RiskLevel = p.RiskLevel,
                LockUpMonths = p.LockUpMonths,
                IsActive = true
            }).ToList();
            session.InitializeCrowdfunding(crowdfunding);

            // Savings account is unlocked from the start
            session.UnlockedAssets.Add("tabungan");
            session.AddLogEntry(ageMode == AgeMode.Kids
                ? "Selamat datang di Tjoean! Mari belajar investasi!"
                : "Welcome to Tjoean Investment Simulator. Let's learn to invest wisely.");

            // Initialize bot with balanced strategy
            // Target: 10% Savings, 10% Deposito, 15% Bonds, 30% Index Fund, 15% Stocks, 20% Gold
            // Bot starts with 10% in savings (always available)
            var initialSavings = session.BotCashBalance * 0.10m; // 500K
            session.BotSavingsBalance = initialSavings;
            session.BotCashBalance -= initialSavings;

            _sessions[connectionId] = session;
            _behaviorLog.StartSession(connectionId, playerId);
            return session;
        }
    }

    public GameSession? GetSession(string connectionId)
    {
        lock (_lock)
        {
            return _sessions.GetValueOrDefault(connectionId);
        }
    }

    public void RemoveSession(string connectionId)
    {
        lock (_lock)
        {
            _sessions.Remove(connectionId);
        }
    }

    // === MULTIPLAYER SESSION MANAGEMENT ===

    public RoomMarketState CreateRoomMarketState(string roomCode)
    {
        lock (_lock)
        {
            var seed = _random.Next();
            var rng = new Random(seed);

            var state = new RoomMarketState { SharedSeed = seed };

            // Randomly select 4 stocks: 3 shariah + 1 non-shariah (shared for all room players)
            state.AvailableStocks = SelectRandomStocks(rng);

            // Shared index funds (1 conventional + 1 shariah)
            state.AvailableIndices = SelectRandomIndices(rng);

            // Shared cryptos
            state.AvailableCryptos = _allCryptos.Select(c => new CryptoInfo
            {
                Symbol = c.Symbol,
                Name = c.Name,
                CurrentPrice = c.CurrentPrice,
                PreviousPrice = c.PreviousPrice
            }).ToList();

            // Shared crowdfunding projects
            state.AvailableCrowdfunding = _allCrowdfundingProjects.OrderBy(_ => rng.Next()).Take(3).Select(p => new CrowdfundingProject
            {
                ProjectId = p.ProjectId,
                ProjectName = p.ProjectName,
                ProjectType = p.ProjectType,
                MinimumInvestment = p.MinimumInvestment,
                ExpectedReturn = p.ExpectedReturn,
                RiskLevel = p.RiskLevel,
                LockUpMonths = p.LockUpMonths,
                IsActive = true
            }).ToList();

            // Pre-generate initial prices
            foreach (var asset in _assets)
            {
                if (!asset.Value.IsFixedIncome && asset.Value.Category != "savings" && asset.Value.Category != "deposito" && asset.Value.Category != "bond")
                {
                    state.InitialPrices[asset.Key] = asset.Value.BasePrice;
                }
            }

            // Pre-generate event months and types for all years
            for (int year = 2; year <= GameSession.MAX_YEARS + 1; year++)
            {
                state.EventMonthPerYear[year] = rng.Next(7, 11);
                state.EventIndexPerYear[year] = rng.Next(_events.Count);
                var costPercent = 0.20m + (decimal)(rng.NextDouble() * 0.25);
                var cost = Math.Round(GameSession.YEARLY_INCOME * costPercent / 100_000) * 100_000;
                state.EventCostPerYear[year] = Math.Max(2_000_000, Math.Min(4_500_000, cost));
            }

            _roomMarketStates[roomCode] = state;
            return state;
        }
    }

    public GameSession CreateMultiplayerSession(string playerId, string connectionId, string roomCode, AgeMode ageMode, Language language)
    {
        lock (_lock)
        {
            if (!_roomMarketStates.TryGetValue(roomCode, out var marketState))
                throw new InvalidOperationException($"No market state for room {roomCode}");

            var session = new GameSession
            {
                PlayerId = playerId,
                ConnectionId = connectionId,
                AgeMode = ageMode,
                Language = language,
                RoomCode = roomCode,
                IsMultiplayer = true,
                IsPaused = true  // Start paused — MP unlock popup for tabungan will sync all players
            };

            // Use shared initial prices
            foreach (var kvp in marketState.InitialPrices)
            {
                session.AssetPrices[kvp.Key] = kvp.Value;
                session.PreviousPrices[kvp.Key] = kvp.Value;
            }

            // Use shared event month for year 1
            if (marketState.EventMonthPerYear.TryGetValue(2, out var eventMonth))
            {
                session.EventMonthForYear = eventMonth;
            }
            else
            {
                session.EventMonthForYear = _random.Next(7, 11);
            }
            session.EventOccurredThisYear = false;

            // Clone shared stocks for this player
            session.InitializeStocks(marketState.AvailableStocks.Select(s => new StockInfo
            {
                Ticker = s.Ticker, CompanyName = s.CompanyName, Sector = s.Sector,
                CurrentPrice = s.CurrentPrice, PreviousPrice = s.PreviousPrice,
                LastPriceUpdateMonth = 0, AnnualDividendPerShare = s.AnnualDividendPerShare,
                DividendType = s.DividendType, IsShariahCompliant = s.IsShariahCompliant
            }).ToList());

            // Clone shared indices
            session.AvailableIndices = marketState.AvailableIndices.Select(i => new IndexInfo
            {
                IndexId = i.IndexId, DisplayName = i.DisplayName, IsShariah = i.IsShariah,
                CurrentPrice = i.CurrentPrice, PreviousPrice = i.PreviousPrice,
                PriceHistory = new List<decimal>(i.PriceHistory)
            }).ToList();

            // Clone shared cryptos
            session.InitializeCryptos(marketState.AvailableCryptos.Select(c => new CryptoInfo
            {
                Symbol = c.Symbol, Name = c.Name,
                CurrentPrice = c.CurrentPrice, PreviousPrice = c.PreviousPrice,
                PriceHistory = new List<decimal>(c.PriceHistory)
            }).ToList());

            // Clone shared crowdfunding
            session.InitializeCrowdfunding(marketState.AvailableCrowdfunding.Select(p => new CrowdfundingProject
            {
                ProjectId = p.ProjectId, ProjectName = p.ProjectName, ProjectType = p.ProjectType,
                Description = p.Description, FundingGoal = p.FundingGoal, CurrentFunding = 0,
                MinimumInvestment = p.MinimumInvestment, DaysRemaining = p.DaysRemaining,
                ExpectedReturn = p.ExpectedReturn, RiskLevel = p.RiskLevel, IsActive = true
            }).ToList());

            // Set per-session rates for game year 1
            session.CurrentDepositoRates = RefreshDepositoRates(1);
            session.CurrentBondRates = RefreshBondRates(1);

            session.UnlockedAssets.Add("tabungan");
            session.AddLogEntry(ageMode == AgeMode.Kids
                ? "Selamat datang di Tjoean! Mari belajar investasi!"
                : "Welcome to Tjoean Investment Simulator. Let's learn to invest wisely.");

            // Initialize bot
            var initialSavings = session.BotCashBalance * 0.05m;
            session.BotSavingsBalance = initialSavings;
            session.BotCashBalance -= initialSavings;

            _sessions[connectionId] = session;
            _behaviorLog.StartSession(connectionId, playerId);
            return session;
        }
    }

    public List<GameSession> GetRoomSessions(string roomCode)
    {
        lock (_lock)
        {
            return _sessions.Values.Where(s => s.RoomCode == roomCode).ToList();
        }
    }

    public List<LeaderboardEntry> GetLeaderboard(string roomCode)
    {
        lock (_lock)
        {
            var sessions = _sessions.Values.Where(s => s.RoomCode == roomCode).ToList();
            var entries = new List<LeaderboardEntry>();

            foreach (var s in sessions)
            {
                entries.Add(new LeaderboardEntry
                {
                    PlayerName = s.PlayerId,
                    NetWorth = s.NetWorth,
                    IsBot = false,
                    TotalProfit = s.NetWorth - 5_000_000 - (s.CurrentYear - 1) * GameSession.YEARLY_INCOME
                });
            }

            // Add bot from the first session (all bots have same market conditions)
            if (sessions.Count > 0)
            {
                var firstSession = sessions[0];
                // Use bot net worth minus player event deduction for fair comparison
                var botDisplayNW = firstSession.BotNetWorth - firstSession.PlayerTotalEventCostPaid;
                entries.Add(new LeaderboardEntry
                {
                    PlayerName = "Financial Advisor Bot",
                    NetWorth = botDisplayNW,
                    IsBot = true,
                    TotalProfit = botDisplayNW - 5_000_000 - (firstSession.CurrentYear - 1) * GameSession.YEARLY_INCOME
                });
            }

            // Sort and rank
            entries = entries.OrderByDescending(e => e.NetWorth).ToList();
            for (int i = 0; i < entries.Count; i++)
            {
                entries[i].Rank = i + 1;
            }

            return entries;
        }
    }

    public bool AreAllRoomPlayersFinished(string roomCode)
    {
        lock (_lock)
        {
            var sessions = _sessions.Values.Where(s => s.RoomCode == roomCode).ToList();
            return sessions.Count > 0 && sessions.All(s => s.IsGameOver);
        }
    }

    public RoomMarketState? GetRoomMarketState(string roomCode)
    {
        lock (_lock)
        {
            return _roomMarketStates.GetValueOrDefault(roomCode);
        }
    }

    public void RemoveRoomMarketState(string roomCode)
    {
        lock (_lock)
        {
            _roomMarketStates.Remove(roomCode);
        }
    }

    public void DismissIntro(string connectionId)
    {
        lock (_lock)
        {
            var session = GetSession(connectionId);
            if (session == null) return;
            session.ShowIntro = false;
            session.IntroAssetType = null;
        }
    }

    // === SAVINGS ACCOUNT OPERATIONS ===
    public bool DepositToSavings(string connectionId, decimal amount)
    {
        lock (_lock)
        {
            var session = GetSession(connectionId);
            if (session == null || session.IsGameOver || session.IsPaused || session.IsEventPending)
                return false;

            if (amount < 100_000 || session.CashBalance < amount)
                return false;

            session.CashBalance -= amount;

            if (session.SavingsAccount == null)
            {
                session.SavingsAccount = new SavingsAccount
                {
                    Balance = amount,
                    InterestRate = 0.01m // 1% annual
                };
            }
            else
            {
                session.SavingsAccount.Balance += amount;
            }

            session.AddLogEntry(session.Language == Language.Indonesian
                ? $"Taruh uang ke tabungan Rp {amount:N0}"
                : $"Deposited Rp {amount:N0} to savings account");
            return true;
        }
    }

    public bool WithdrawFromSavings(string connectionId, decimal amount)
    {
        lock (_lock)
        {
            var session = GetSession(connectionId);
            if (session == null || session.IsGameOver || session.SavingsAccount == null)
                return false;

            if (amount > session.SavingsAccount.Balance)
                return false;

            session.SavingsAccount.Balance -= amount;
            session.CashBalance += amount;

            if (session.SavingsAccount.Balance <= 0)
                session.SavingsAccount = null;

            session.AddLogEntry(session.Language == Language.Indonesian
                ? $"Ambil uang dari tabungan Rp {amount:N0}"
                : $"Withdrew Rp {amount:N0} from savings account");
            return true;
        }
    }

    // === DEPOSITO OPERATIONS ===
    public bool BuyDeposito(string connectionId, int periodMonths, decimal amount, bool autoRollOver = false, bool isShariah = false)
    {
        lock (_lock)
        {
            var session = GetSession(connectionId);
            if (session == null || session.IsGameOver || session.IsPaused || session.IsEventPending)
                return false;

            var rate = session.CurrentDepositoRates.FirstOrDefault(r => r.PeriodMonths == periodMonths && r.IsShariah == isShariah);
            if (rate == null) return false;

            if (amount < rate.MinimumDeposit || session.CashBalance < amount)
                return false;

            session.CashBalance -= amount;

            var deposito = new DepositoItem
            {
                Principal = amount,
                PeriodMonths = periodMonths,
                InterestRate = rate.AnnualRate,
                StartYear = session.CurrentYear,
                StartMonth = session.CurrentMonth,
                MonthsRemaining = periodMonths,
                AutoRollOver = autoRollOver,
                IsShariah = rate.IsShariah,
                NisbahRatio = rate.NisbahRatio
            };

            session.Depositos.Add(deposito);
            _behaviorLog.LogAction(connectionId, "deposito", "BUY", amount, rate.IsShariah, AllocationPct(session, session.TotalDepositoValue));
            var bankLabel = rate.IsShariah ? "Syariah" : "Konvensional";
            session.AddLogEntry(session.Language == Language.Indonesian
                ? $"Buka Deposito {bankLabel} {rate.PeriodName} Rp {amount:N0} ({(rate.IsShariah ? "bagi hasil" : "bunga")} {rate.AnnualRate * 100:F2}%/tahun)"
                : $"Opened {(rate.IsShariah ? "Shariah" : "Conventional")} {rate.PeriodName} CD of Rp {amount:N0} at {rate.AnnualRate * 100:F2}% p.a.");
            return true;
        }
    }

    public bool WithdrawDeposito(string connectionId, string depositoId, bool earlyWithdraw)
    {
        lock (_lock)
        {
            var session = GetSession(connectionId);
            if (session == null || session.IsGameOver)
                return false;

            var deposito = session.Depositos.FirstOrDefault(d => d.Id == depositoId);
            if (deposito == null) return false;

            decimal withdrawAmount;
            if (deposito.IsMatured || !earlyWithdraw)
            {
                withdrawAmount = deposito.MaturityValue;
                session.TotalDepositoInterestEarned += withdrawAmount - deposito.Principal;
                session.AddLogEntry(session.Language == Language.Indonesian
                    ? $"Deposito jatuh tempo! Terima Rp {withdrawAmount:N0}"
                    : $"CD matured! Received Rp {withdrawAmount:N0}");
            }
            else
            {
                var rate = session.CurrentDepositoRates.FirstOrDefault(r => r.PeriodMonths == deposito.PeriodMonths);
                var penaltyRate = rate?.PenaltyRate ?? 0.5m;
                var earnedInterest = deposito.CurrentValue - deposito.Principal;
                var penalty = earnedInterest * penaltyRate;
                withdrawAmount = deposito.Principal + earnedInterest - penalty;
                session.TotalDepositoInterestEarned += withdrawAmount - deposito.Principal;
                session.AddLogEntry(session.Language == Language.Indonesian
                    ? $"Cairkan deposito lebih awal, kena denda. Terima Rp {withdrawAmount:N0}"
                    : $"Early CD withdrawal with {penaltyRate * 100}% penalty. Received Rp {withdrawAmount:N0}");
            }

            session.CashBalance += withdrawAmount;
            session.Depositos.Remove(deposito);
            _behaviorLog.LogAction(connectionId, "deposito", "SELL", withdrawAmount, deposito.IsShariah, AllocationPct(session, session.TotalDepositoValue));
            return true;
        }
    }

    public bool ToggleDepositoAutoRollOver(string connectionId, string depositoId)
    {
        lock (_lock)
        {
            var session = GetSession(connectionId);
            if (session == null || session.IsGameOver)
                return false;

            var deposito = session.Depositos.FirstOrDefault(d => d.Id == depositoId);
            if (deposito == null) return false;

            deposito.AutoRollOver = !deposito.AutoRollOver;

            session.AddLogEntry(session.Language == Language.Indonesian
                ? $"Roll Over Otomatis: {(deposito.AutoRollOver ? "AKTIF" : "NONAKTIF")} untuk deposito {deposito.PeriodMonths} bulan"
                : $"Auto Roll Over: {(deposito.AutoRollOver ? "ENABLED" : "DISABLED")} for {deposito.PeriodMonths}-month CD");

            return true;
        }
    }

    // === BOND OPERATIONS ===
    public bool BuyBond(string connectionId, string bondType, decimal amount)
    {
        lock (_lock)
        {
            var session = GetSession(connectionId);
            if (session == null || session.IsGameOver || session.IsPaused || session.IsEventPending)
                return false;

            var rate = session.CurrentBondRates.FirstOrDefault(r => r.BondType == bondType);
            if (rate == null) return false;

            if (amount < rate.MinimumInvestment || session.CashBalance < amount)
                return false;

            session.CashBalance -= amount;

            var bond = new BondItem
            {
                BondName = rate.PeriodName,
                Principal = amount,
                PeriodMonths = rate.PeriodMonths,
                CouponRate = rate.CouponRate,
                StartYear = session.CurrentYear,
                StartMonth = session.CurrentMonth,
                MonthsRemaining = rate.PeriodMonths,
                IsShariah = rate.IsShariah,
                SeriesName = rate.SeriesName,
                AkadType = rate.AkadType
            };

            session.Bonds.Add(bond);
            _behaviorLog.LogAction(connectionId, "bond", "BUY", amount, rate.IsShariah, AllocationPct(session, session.TotalBondValue));
            var seriesLabel = rate.SeriesName != null ? $" ({rate.SeriesName})" : "";
            session.AddLogEntry(session.Language == Language.Indonesian
                ? $"Beli {rate.PeriodName}{seriesLabel} Rp {amount:N0} (kupon {rate.CouponRate * 100:F2}%/tahun)"
                : $"Purchased {rate.PeriodName}{seriesLabel} bond of Rp {amount:N0} at {rate.CouponRate * 100:F2}% coupon");
            return true;
        }
    }

    public bool WithdrawBond(string connectionId, string bondId)
    {
        lock (_lock)
        {
            var session = GetSession(connectionId);
            if (session == null || session.IsGameOver)
                return false;

            var bond = session.Bonds.FirstOrDefault(b => b.Id == bondId);
            if (bond == null || !bond.IsMatured) return false;

            var totalReturn = bond.Principal + bond.TotalCouponEarned;
            session.CashBalance += totalReturn;
            session.Bonds.Remove(bond);
            _behaviorLog.LogAction(connectionId, "bond", "SELL", totalReturn, bond.IsShariah, AllocationPct(session, session.TotalBondValue));

            session.AddLogEntry(session.Language == Language.Indonesian
                ? $"Obligasi jatuh tempo! Terima Rp {totalReturn:N0}"
                : $"Bond matured! Received Rp {totalReturn:N0}");
            return true;
        }
    }

    // === STOCK OPERATIONS ===
    public bool BuyStock(string connectionId, string ticker, int lots)
    {
        lock (_lock)
        {
            var session = GetSession(connectionId);
            if (session == null || session.IsGameOver || session.IsPaused || session.IsEventPending)
                return false;

            var stock = session.AvailableStocks.FirstOrDefault(s => s.Ticker == ticker);
            if (stock == null) return false;

            var totalCost = stock.CurrentPrice * lots * 100; // 1 lot = 100 shares
            if (session.CashBalance < totalCost)
                return false;

            session.CashBalance -= totalCost;

            var key = $"stock_{ticker}";
            if (!session.Portfolio.ContainsKey(key))
            {
                session.Portfolio[key] = new PortfolioItem
                {
                    AssetType = "saham",
                    DisplayName = $"{ticker} - {stock.CompanyName}",
                    Ticker = ticker,
                    Units = 0,
                    PricePerUnit = stock.CurrentPrice,
                    TotalCost = 0
                };
            }

            session.Portfolio[key].Units += lots * 100;
            session.Portfolio[key].TotalCost += totalCost;
            session.Portfolio[key].PricePerUnit = stock.CurrentPrice;
            _behaviorLog.LogAction(connectionId, $"stock_{ticker}", "BUY", totalCost, stock.IsShariahCompliant, AllocationPct(session, session.Portfolio[key].TotalValue));

            session.AddLogEntry(session.Language == Language.Indonesian
                ? $"Beli {lots} lot saham {ticker}"
                : $"Purchased {lots} lot(s) of {ticker}");
            return true;
        }
    }

    public bool SellStock(string connectionId, string ticker, int lots)
    {
        lock (_lock)
        {
            var session = GetSession(connectionId);
            if (session == null || session.IsGameOver || session.IsPaused)
                return false;

            var key = $"stock_{ticker}";
            if (!session.Portfolio.ContainsKey(key))
                return false;

            var portfolio = session.Portfolio[key];
            var sharesToSell = lots * 100;
            if (portfolio.Units < sharesToSell)
                return false;

            var stock = session.AvailableStocks.FirstOrDefault(s => s.Ticker == ticker);
            if (stock == null) return false;

            var saleValue = stock.CurrentPrice * sharesToSell;
            var costBasis = (portfolio.TotalCost / portfolio.Units) * sharesToSell;

            session.CashBalance += saleValue;
            portfolio.Units -= sharesToSell;
            portfolio.TotalCost -= costBasis;

            if (portfolio.Units <= 0)
                session.Portfolio.Remove(key);

            var profit = saleValue - costBasis;
            session.TotalRealizedPortfolioGainLoss += profit;
            var keyValueAfter = session.Portfolio.TryGetValue(key, out var pfAfter) ? pfAfter.TotalValue : 0m;
            _behaviorLog.LogAction(connectionId, $"stock_{ticker}", "SELL", saleValue, stock.IsShariahCompliant, AllocationPct(session, keyValueAfter));
            var profitText = profit >= 0 ? $"untung Rp {profit:N0}" : $"rugi Rp {Math.Abs(profit):N0}";
            session.AddLogEntry(session.Language == Language.Indonesian
                ? $"Jual {lots} lot saham {ticker} ({profitText})"
                : $"Sold {lots} lot(s) of {ticker} (P/L: {(profit >= 0 ? "+" : "")}Rp {profit:N0})");
            return true;
        }
    }

    // === GENERAL ASSET OPERATIONS (Index Fund, Gold, Crowdfunding) ===
    public bool BuyAsset(string connectionId, string assetType)
    {
        lock (_lock)
        {
            var session = GetSession(connectionId);
            if (session == null || session.IsGameOver || session.IsPaused || session.IsEventPending)
                return false;

            if (!_assets.ContainsKey(assetType))
                return false;

            if (!session.UnlockedAssets.Contains(assetType))
                return false;

            var asset = _assets[assetType];
            if (session.CashBalance < asset.MinimumInvestment)
                return false;

            var currentPrice = session.AssetPrices.GetValueOrDefault(assetType, asset.BasePrice);
            var unitsToBuy = (int)(GameSession.UNIT_COST / currentPrice);
            if (unitsToBuy <= 0) unitsToBuy = 1;

            session.CashBalance -= GameSession.UNIT_COST;

            if (!session.Portfolio.ContainsKey(assetType))
            {
                session.Portfolio[assetType] = new PortfolioItem
                {
                    AssetType = assetType,
                    DisplayName = (session.AgeMode, session.Language) switch { (AgeMode.Kids, Language.Indonesian) => asset.DisplayName, (AgeMode.Kids, Language.English) => asset.DisplayNameEN, (AgeMode.Adult, Language.Indonesian) => asset.DisplayNameAdult, _ => asset.DisplayNameAdultEN },
                    Units = 0,
                    PricePerUnit = currentPrice,
                    TotalCost = 0
                };
            }

            session.Portfolio[assetType].Units += unitsToBuy;
            session.Portfolio[assetType].TotalCost += GameSession.UNIT_COST;
            session.Portfolio[assetType].PricePerUnit = currentPrice;
            // Gold, crypto, crowdfunding are shariah-compliant; others on this path inherit from unlocked assets
            var isShariah = asset.Category is "gold" or "crypto" or "crowdfunding";
            _behaviorLog.LogAction(connectionId, assetType, "BUY", GameSession.UNIT_COST, isShariah, AllocationPct(session, session.Portfolio[assetType].TotalValue));

            session.AddLogEntry(session.Language == Language.Indonesian
                ? $"Beli {asset.DisplayName} Rp 1.000.000"
                : $"Purchased {asset.DisplayNameAdult} Rp 1,000,000");
            return true;
        }
    }

    public bool BuyGoldByGrams(string connectionId, decimal grams)
    {
        lock (_lock)
        {
            var session = GetSession(connectionId);
            if (session == null || session.IsGameOver || session.IsPaused || session.IsEventPending)
                return false;

            const string assetType = "emas";
            if (!session.UnlockedAssets.Contains(assetType))
                return false;

            var asset = _assets[assetType];
            var currentPrice = session.AssetPrices.GetValueOrDefault(assetType, asset.BasePrice);
            var totalCost = currentPrice * grams;

            if (session.CashBalance < totalCost)
                return false;

            session.CashBalance -= totalCost;

            if (!session.Portfolio.ContainsKey(assetType))
            {
                session.Portfolio[assetType] = new PortfolioItem
                {
                    AssetType = assetType,
                    DisplayName = (session.AgeMode, session.Language) switch { (AgeMode.Kids, Language.Indonesian) => asset.DisplayName, (AgeMode.Kids, Language.English) => asset.DisplayNameEN, (AgeMode.Adult, Language.Indonesian) => asset.DisplayNameAdult, _ => asset.DisplayNameAdultEN },
                    Units = 0,
                    PricePerUnit = currentPrice,
                    TotalCost = 0
                };
            }

            session.Portfolio[assetType].Units += (int)Math.Floor(grams); // Store grams as units
            session.Portfolio[assetType].TotalCost += totalCost;
            session.Portfolio[assetType].PricePerUnit = currentPrice;
            _behaviorLog.LogAction(connectionId, "emas", "BUY", totalCost, true, AllocationPct(session, session.Portfolio[assetType].TotalValue));

            session.AddLogEntry(session.Language == Language.Indonesian
                ? $"Beli {grams}g Emas (Rp {totalCost:N0})"
                : $"Purchased {grams}g Gold for Rp {totalCost:N0}");
            return true;
        }
    }

    // === INDEX FUND OPERATIONS ===

    public bool BuyIndex(string connectionId, string indexId, decimal amount)
    {
        lock (_lock)
        {
            var session = GetSession(connectionId);
            if (session == null || session.IsGameOver || session.IsPaused || session.IsEventPending)
                return false;

            if (!session.UnlockedAssets.Contains("reksadana"))
                return false;

            var idx = session.AvailableIndices.FirstOrDefault(i => i.IndexId == indexId);
            if (idx == null || idx.CurrentPrice <= 0)
                return false;

            if (amount < 100_000m || session.CashBalance < amount)
                return false;

            var units = amount / idx.CurrentPrice;
            session.CashBalance -= amount;

            var portfolioKey = $"index_{indexId}";
            if (!session.Portfolio.ContainsKey(portfolioKey))
            {
                session.Portfolio[portfolioKey] = new PortfolioItem
                {
                    AssetType = "reksadana",
                    DisplayName = $"RD Indeks {idx.DisplayName}",
                    Ticker = indexId,
                    Units = 0,
                    PricePerUnit = idx.CurrentPrice,
                    TotalCost = 0
                };
            }

            session.Portfolio[portfolioKey].Units += units;
            session.Portfolio[portfolioKey].TotalCost += amount;
            session.Portfolio[portfolioKey].PricePerUnit = idx.CurrentPrice;
            _behaviorLog.LogAction(connectionId, $"index_{indexId}", "BUY", amount, idx.IsShariah, AllocationPct(session, session.Portfolio[portfolioKey].TotalValue));

            session.AddLogEntry(session.Language == Language.Indonesian
                ? $"Beli RD Indeks {indexId} Rp {amount:N0} ({units:F4} unit)"
                : $"Purchased Index Fund {indexId} Rp {amount:N0} ({units:F4} units)");
            return true;
        }
    }

    public bool SellIndex(string connectionId, string indexId, decimal units)
    {
        lock (_lock)
        {
            var session = GetSession(connectionId);
            if (session == null || session.IsGameOver || session.IsPaused)
                return false;

            var portfolioKey = $"index_{indexId}";
            if (!session.Portfolio.ContainsKey(portfolioKey))
                return false;

            var portfolio = session.Portfolio[portfolioKey];
            if (portfolio.Units <= 0 || units > portfolio.Units)
                return false;

            var idx = session.AvailableIndices.FirstOrDefault(i => i.IndexId == indexId);
            if (idx == null) return false;

            var saleValue = units * idx.CurrentPrice;
            var costBasis = (portfolio.TotalCost / portfolio.Units) * units;

            session.CashBalance += saleValue;
            portfolio.Units -= units;
            portfolio.TotalCost -= costBasis;

            var profit = saleValue - costBasis;
            session.TotalRealizedPortfolioGainLoss += profit;

            if (portfolio.Units <= 0)
                session.Portfolio.Remove(portfolioKey);

            var indexValueAfter = session.Portfolio.TryGetValue(portfolioKey, out var pfAfter) ? pfAfter.TotalValue : 0m;
            _behaviorLog.LogAction(connectionId, $"index_{indexId}", "SELL", saleValue, idx.IsShariah, AllocationPct(session, indexValueAfter));
            session.AddLogEntry(session.Language == Language.Indonesian
                ? $"Jual RD Indeks {indexId} ({units:F4} unit, {(profit >= 0 ? "untung" : "rugi")} Rp {Math.Abs(profit):N0})"
                : $"Sold Index Fund {indexId} ({units:F4} units, P/L: {(profit >= 0 ? "+" : "")}Rp {profit:N0})");
            return true;
        }
    }

    public bool SellAllIndex(string connectionId, string indexId)
    {
        lock (_lock)
        {
            var session = GetSession(connectionId);
            if (session == null) return false;

            var portfolioKey = $"index_{indexId}";
            if (!session.Portfolio.ContainsKey(portfolioKey))
                return false;

            return SellIndex(connectionId, indexId, session.Portfolio[portfolioKey].Units);
        }
    }

    public bool SellAsset(string connectionId, string assetType)
    {
        lock (_lock)
        {
            var session = GetSession(connectionId);
            if (session == null || session.IsGameOver || session.IsPaused)
                return false;

            if (!session.Portfolio.ContainsKey(assetType))
                return false;

            var portfolio = session.Portfolio[assetType];
            if (portfolio.Units <= 0)
                return false;

            var currentPrice = session.AssetPrices.GetValueOrDefault(assetType, _assets[assetType].BasePrice);
            var unitsToSell = (int)(GameSession.UNIT_COST / currentPrice);
            if (unitsToSell <= 0) unitsToSell = 1;
            if (unitsToSell > (int)portfolio.Units) unitsToSell = (int)portfolio.Units;

            var saleValue = unitsToSell * currentPrice;
            var costBasis = (portfolio.TotalCost / portfolio.Units) * unitsToSell;

            session.CashBalance += saleValue;
            portfolio.Units -= unitsToSell;
            portfolio.TotalCost -= costBasis;

            if (portfolio.Units <= 0)
                session.Portfolio.Remove(assetType);

            var profit = saleValue - costBasis;
            session.TotalRealizedPortfolioGainLoss += profit;
            var asset = _assets[assetType];
            var isShariah = asset.Category is "gold" or "crypto" or "crowdfunding";
            var valueAfter = session.Portfolio.TryGetValue(assetType, out var pfA) ? pfA.TotalValue : 0m;
            _behaviorLog.LogAction(connectionId, assetType, "SELL", saleValue, isShariah, AllocationPct(session, valueAfter));
            var profitText = profit >= 0 ? $"untung Rp {profit:N0}" : $"rugi Rp {Math.Abs(profit):N0}";
            session.AddLogEntry(session.Language == Language.Indonesian
                ? $"Jual {asset.DisplayName} ({profitText})"
                : $"Sold {asset.DisplayNameAdult} (P/L: {(profit >= 0 ? "+" : "")}Rp {profit:N0})");
            return true;
        }
    }

    public bool SellAllAssets(string connectionId, string assetType)
    {
        lock (_lock)
        {
            var session = GetSession(connectionId);
            if (session == null || session.IsGameOver || session.IsPaused)
                return false;

            if (!session.Portfolio.ContainsKey(assetType))
                return false;

            var portfolio = session.Portfolio[assetType];
            if (portfolio.Units <= 0)
                return false;

            var currentPrice = session.AssetPrices.GetValueOrDefault(assetType, _assets[assetType].BasePrice);
            var saleValue = portfolio.Units * currentPrice;
            var costBasis = portfolio.TotalCost;

            session.CashBalance += saleValue;

            var profit = saleValue - costBasis;
            session.TotalRealizedPortfolioGainLoss += profit;
            var asset = _assets[assetType];
            var isShariahSell = asset.Category is "gold" or "crypto" or "crowdfunding";
            _behaviorLog.LogAction(connectionId, assetType, "SELL", saleValue, isShariahSell, 0m);
            var profitText = profit >= 0 ? $"untung Rp {profit:N0}" : $"rugi Rp {Math.Abs(profit):N0}";
            session.AddLogEntry(session.Language == Language.Indonesian
                ? $"Jual semua {asset.DisplayName} ({profitText})"
                : $"Sold all {asset.DisplayNameAdult} (P/L: {(profit >= 0 ? "+" : "")}Rp {profit:N0})");

            session.Portfolio.Remove(assetType);
            return true;
        }
    }

    // === CROWDFUNDING OPERATIONS ===
    public bool BuyCrowdfunding(string connectionId, string projectId, decimal amount)
    {
        lock (_lock)
        {
            var session = GetSession(connectionId);
            if (session == null || session.IsGameOver || session.IsPaused || session.IsEventPending)
                return false;

            if (!session.UnlockedAssets.Contains("crowdfunding"))
                return false;

            var project = session.AvailableCrowdfunding.FirstOrDefault(p => p.ProjectId == projectId && p.IsActive);
            if (project == null) return false;

            if (amount < project.MinimumInvestment || session.CashBalance < amount)
                return false;

            session.CashBalance -= amount;

            // Create a new crowdfunding investment with lock-up period
            var investment = new CrowdfundingInvestment
            {
                ProjectId = project.ProjectId,
                ProjectName = project.ProjectName,
                ProjectType = project.ProjectType,
                InvestedAmount = amount,
                ExpectedReturn = project.ExpectedReturn,
                StartYear = session.CurrentYear,
                StartMonth = session.CurrentMonth,
                LockUpMonths = project.LockUpMonths,
                MonthsRemaining = project.LockUpMonths
            };

            session.CrowdfundingInvestments.Add(investment);
            session.TotalCrowdfundingInvested += amount;
            _behaviorLog.LogAction(connectionId, $"crowdfunding_{projectId}", "BUY", amount, true, AllocationPct(session, session.TotalCrowdfundingValue));

            session.AddLogEntry(session.Language == Language.Indonesian
                ? $"Investasi di {project.ProjectName} Rp {amount:N0} (terkunci {project.LockUpMonths} bulan)"
                : $"Invested in {project.ProjectName} Rp {amount:N0} (locked for {project.LockUpMonths} months)");
            return true;
        }
    }

    // === CRYPTO OPERATIONS ===
    public bool BuyCrypto(string connectionId, string symbol, decimal amount)
    {
        lock (_lock)
        {
            var session = GetSession(connectionId);
            if (session == null || session.IsGameOver || session.IsPaused || session.IsEventPending)
                return false;

            if (!session.UnlockedAssets.Contains("crypto"))
                return false;

            var crypto = session.AvailableCryptos.FirstOrDefault(c => c.Symbol == symbol);
            if (crypto == null) return false;

            if (session.CashBalance < amount)
                return false;

            session.CashBalance -= amount;

            var key = $"crypto_{symbol}";
            var units = amount / crypto.CurrentPrice; // Calculate fractional units

            if (!session.Portfolio.ContainsKey(key))
            {
                session.Portfolio[key] = new PortfolioItem
                {
                    AssetType = "crypto",
                    DisplayName = $"{crypto.Name}",
                    Ticker = symbol,
                    Units = 0,
                    PricePerUnit = crypto.CurrentPrice,
                    TotalCost = 0
                };
            }

            session.Portfolio[key].Units += units; // Store fractional units directly
            session.Portfolio[key].TotalCost += amount;
            session.Portfolio[key].PricePerUnit = crypto.CurrentPrice;
            _behaviorLog.LogAction(connectionId, $"crypto_{symbol}", "BUY", amount, true, AllocationPct(session, session.Portfolio[key].TotalValue));

            session.AddLogEntry(session.Language == Language.Indonesian
                ? $"Beli {units:F4} {crypto.Symbol} (Rp {amount:N0})"
                : $"Bought {units:F4} {crypto.Symbol} for Rp {amount:N0}");
            return true;
        }
    }

    public bool SellCrypto(string connectionId, string symbol, decimal amount)
    {
        lock (_lock)
        {
            var session = GetSession(connectionId);
            if (session == null || session.IsGameOver || session.IsPaused)
                return false;

            var key = $"crypto_{symbol}";
            if (!session.Portfolio.ContainsKey(key))
                return false;

            var portfolio = session.Portfolio[key];
            var crypto = session.AvailableCryptos.FirstOrDefault(c => c.Symbol == symbol);
            if (crypto == null) return false;

            var currentValue = portfolio.Units * crypto.CurrentPrice;
            if (amount > currentValue)
                amount = currentValue;

            var unitsToSell = amount / crypto.CurrentPrice;
            var costBasis = (portfolio.TotalCost / portfolio.Units) * unitsToSell;

            session.CashBalance += amount;
            portfolio.Units -= unitsToSell;
            portfolio.TotalCost -= costBasis;

            if (portfolio.Units <= 0.0001m) // Small threshold for floating point comparison
                session.Portfolio.Remove(key);

            var profit = amount - costBasis;
            session.TotalRealizedPortfolioGainLoss += profit;
            var cryptoValueAfter = session.Portfolio.TryGetValue(key, out var pfAfter) ? pfAfter.TotalValue : 0m;
            _behaviorLog.LogAction(connectionId, $"crypto_{symbol}", "SELL", amount, true, AllocationPct(session, cryptoValueAfter));
            session.AddLogEntry(session.Language == Language.Indonesian
                ? $"Jual {unitsToSell:F4} {crypto.Symbol} ({(profit >= 0 ? "untung" : "rugi")} Rp {Math.Abs(profit):N0})"
                : $"Sold {unitsToSell:F4} {crypto.Symbol} (P/L: {(profit >= 0 ? "+" : "")}Rp {profit:N0})");
            return true;
        }
    }

    // === EVENT HANDLING ===
    public bool PayEventFromCash(string connectionId)
    {
        lock (_lock)
        {
            var session = GetSession(connectionId);
            if (session == null || !session.IsEventPending || session.ActiveEvent == null)
                return false;

            var cost = session.EventCost ?? 0;
            if (session.CashBalance < cost)
                return false;

            session.CashBalance -= cost;
            session.PlayerTotalEventCostPaid += cost;
            var eventTitle = session.ActiveEvent.GetTitle(session.AgeMode, session.Language);
            session.AddLogEntry(session.Language == Language.Indonesian
                ? $"Bayar {eventTitle} dari kas: Rp {cost:N0}"
                : $"Paid {eventTitle} from cash: Rp {cost:N0}");
            ClearEvent(session);
            return true;
        }
    }

    public bool PayEventFromSavings(string connectionId)
    {
        lock (_lock)
        {
            var session = GetSession(connectionId);
            if (session == null || !session.IsEventPending || session.ActiveEvent == null)
                return false;

            var cost = session.EventCost ?? 0;
            if (session.SavingsAccount == null || session.SavingsAccount.Balance < cost)
                return false;

            session.SavingsAccount.Balance -= cost;
            if (session.SavingsAccount.Balance <= 0)
                session.SavingsAccount = null;

            session.PlayerTotalEventCostPaid += cost;
            var eventTitle = session.ActiveEvent.GetTitle(session.AgeMode, session.Language);
            session.AddLogEntry(session.Language == Language.Indonesian
                ? $"Bayar {eventTitle} dari tabungan: Rp {cost:N0}"
                : $"Paid {eventTitle} from savings: Rp {cost:N0}");
            ClearEvent(session);
            return true;
        }
    }

    public bool PayEventFromPortfolio(string connectionId, string assetType)
    {
        lock (_lock)
        {
            var session = GetSession(connectionId);
            if (session == null || !session.IsEventPending || session.ActiveEvent == null)
                return false;

            var cost = session.EventCost ?? 0;

            if (session.Portfolio.ContainsKey(assetType))
            {
                var portfolio = session.Portfolio[assetType];
                // Get current price based on asset type
                decimal currentPrice;
                if (assetType.StartsWith("index_") && !string.IsNullOrEmpty(portfolio.Ticker))
                {
                    var idx = session.AvailableIndices.FirstOrDefault(i => i.IndexId == portfolio.Ticker);
                    currentPrice = idx?.CurrentPrice ?? 1_000_000m;
                }
                else if (assetType.StartsWith("stock_") && !string.IsNullOrEmpty(portfolio.Ticker))
                {
                    var stock = session.AvailableStocks.FirstOrDefault(s => s.Ticker == portfolio.Ticker);
                    currentPrice = stock?.CurrentPrice ?? 1_000_000m;
                }
                else if (assetType.StartsWith("crypto_") && !string.IsNullOrEmpty(portfolio.Ticker))
                {
                    var crypto = session.AvailableCryptos.FirstOrDefault(c => c.Symbol == portfolio.Ticker);
                    currentPrice = crypto?.CurrentPrice ?? 1_000_000m;
                }
                else
                {
                    currentPrice = session.AssetPrices.GetValueOrDefault(assetType, _assets.GetValueOrDefault(assetType)?.BasePrice ?? 1_000_000);
                }
                var portfolioValue = portfolio.Units * currentPrice;

                if (portfolioValue >= cost)
                {
                    // Use decimal for fractional-unit assets (index, crypto), int for whole-unit assets (stocks)
                    decimal unitsNeeded;
                    if (assetType.StartsWith("index_") || assetType.StartsWith("crypto_"))
                        unitsNeeded = Math.Ceiling(cost / currentPrice * 10000m) / 10000m;
                    else
                        unitsNeeded = (int)Math.Ceiling(cost / currentPrice);
                    if (unitsNeeded > portfolio.Units) unitsNeeded = portfolio.Units;

                    var saleValue = unitsNeeded * currentPrice;
                    var costBasis = portfolio.Units > 0 ? (portfolio.TotalCost / portfolio.Units) * unitsNeeded : 0;
                    var saleProfit = saleValue - costBasis;
                    portfolio.Units -= unitsNeeded;
                    portfolio.TotalCost -= costBasis;

                    if (portfolio.Units <= 0)
                        session.Portfolio.Remove(assetType);

                    if (saleValue > cost)
                        session.CashBalance += (saleValue - cost);

                    session.TotalRealizedPortfolioGainLoss += saleProfit;
                    session.PlayerTotalEventCostPaid += cost;
                    var displayName = _assets.GetValueOrDefault(assetType)?.DisplayName ?? assetType;
                    var evtTitle = session.ActiveEvent.GetTitle(session.AgeMode, session.Language);
                    session.AddLogEntry(session.Language == Language.Indonesian
                        ? $"Bayar {evtTitle} dari {displayName}: Rp {cost:N0}"
                        : $"Paid {evtTitle} from portfolio: Rp {cost:N0}");
                    ClearEvent(session);
                    return true;
                }
            }

            return false;
        }
    }

    private void ClearEvent(GameSession session)
    {
        session.ActiveEvent = null;
        session.EventCost = null;
        session.IsEventPending = false;
        session.IsPaused = false;
        session.EventPendingAt = null;
    }

    /// <summary>
    /// Process one game tick. Returns (unlockOccurred, unlockAssetType, autoPayOccurred).
    /// </summary>
    public (bool UnlockOccurred, string? UnlockAssetType, bool AutoPayOccurred) ProcessTick(string connectionId)
    {
        lock (_lock)
        {
            var session = GetSession(connectionId);
            if (session == null || session.IsGameOver || session.IsPaused || session.IsEventPending)
                return (false, null, false);

            if (session.ShowIntro)
                return (false, null, false);

            session.MonthProgress += 20;
            session.NewUnlockMessage = null;

            string? unlockAssetType = null;
            if (session.MonthProgress >= 100)
            {
                session.MonthProgress = 0;
                unlockAssetType = ProcessMonthEnd(session);

                // If unlock occurred in multiplayer, pause all room sessions
                // and sync all unlocked assets to other sessions so they don't re-trigger
                if (unlockAssetType != null && session.IsMultiplayer && session.RoomCode != null)
                {
                    foreach (var s in _sessions.Values.Where(s => s.RoomCode == session.RoomCode))
                    {
                        s.IsPaused = true;
                        if (s != session)
                        {
                            foreach (var asset in session.UnlockedAssets)
                            {
                                if (!s.UnlockedAssets.Contains(asset))
                                    s.UnlockedAssets.Add(asset);
                            }
                        }
                    }
                }
            }

            return (unlockAssetType != null, unlockAssetType, false);
        }
    }

    /// <summary>
    /// Process month-end logic. Returns the unlocked asset type if a new asset was unlocked, otherwise null.
    /// </summary>
    private string? ProcessMonthEnd(GameSession session)
    {
        // Update savings interest (monthly)
        if (session.SavingsAccount != null)
        {
            var monthlyInterest = session.SavingsAccount.Balance * (session.SavingsAccount.InterestRate / 12);
            session.SavingsAccount.Balance += monthlyInterest;
            session.TotalSavingsInterestEarned += monthlyInterest;
        }

        // Update depositos
        foreach (var deposito in session.Depositos.ToList())
        {
            deposito.MonthsRemaining--;
            if (deposito.IsMatured)
            {
                if (deposito.AutoRollOver)
                {
                    // Automatically re-invest the maturity value
                    var maturityValue = deposito.MaturityValue;
                    session.TotalDepositoInterestEarned += maturityValue - deposito.Principal;
                    var rate = session.CurrentDepositoRates.FirstOrDefault(r => r.PeriodMonths == deposito.PeriodMonths && r.IsShariah == deposito.IsShariah);
                    if (rate != null)
                    {
                        // Reset the deposito with new principal (maturity value)
                        deposito.Principal = maturityValue;
                        deposito.InterestRate = rate.AnnualRate; // Use current rate in case it changed
                        deposito.NisbahRatio = rate.NisbahRatio; // Update nisbah for shariah
                        deposito.StartYear = session.CurrentYear;
                        deposito.StartMonth = session.CurrentMonth;
                        deposito.MonthsRemaining = deposito.PeriodMonths;

                        session.AddLogEntry(session.Language == Language.Indonesian
                            ? $"🔄 Deposito di-roll over otomatis! Principal baru: Rp {maturityValue:N0}"
                            : $"🔄 CD automatically rolled over! New principal: Rp {maturityValue:N0}");
                    }
                    else
                    {
                        // If rate not found, treat as normal maturity
                        session.CashBalance += maturityValue;
                        session.AddLogEntry(session.Language == Language.Indonesian
                            ? $"Deposito jatuh tempo! +Rp {maturityValue:N0}"
                            : $"CD matured! +Rp {maturityValue:N0}");
                        session.Depositos.Remove(deposito);
                    }
                }
                else
                {
                    // Normal maturity - return to cash
                    session.TotalDepositoInterestEarned += deposito.MaturityValue - deposito.Principal;
                    session.CashBalance += deposito.MaturityValue;
                    session.AddLogEntry(session.Language == Language.Indonesian
                        ? $"Deposito jatuh tempo! +Rp {deposito.MaturityValue:N0}"
                        : $"CD matured! +Rp {deposito.MaturityValue:N0}");
                    session.Depositos.Remove(deposito);
                }
            }
        }

        // Update bonds - pay monthly coupon
        foreach (var bond in session.Bonds.ToList())
        {
            var monthlyCoupon = bond.Principal * bond.CouponRate / 12;
            session.CashBalance += monthlyCoupon;
            session.TotalBondCouponEarned += monthlyCoupon;

            bond.MonthsRemaining--;
            if (bond.IsMatured)
            {
                session.CashBalance += bond.Principal;
                session.AddLogEntry(session.Language == Language.Indonesian
                    ? $"Obligasi jatuh tempo! Pokok kembali Rp {bond.Principal:N0}"
                    : $"Bond matured! Principal returned: Rp {bond.Principal:N0}");
                session.Bonds.Remove(bond);
            }
        }

        // Advance month BEFORE fetching prices so prices match the new month
        session.CurrentMonth++;

        // Update market prices for fluctuating assets
        UpdateMarketPrices(session);

        // Update stock prices every month (real historical data)
        UpdateStockPrices(session);

        // Update crypto prices monthly
        UpdateCryptoPrices(session);

        // Update portfolio values
        UpdatePortfolioValues(session);

        // Process crowdfunding investments (check maturity and failures)
        ProcessCrowdfundingMonthEnd(session);

        // Randomize event month at the start of each year (between month 7-10)
        if (session.CurrentMonth == 1 && session.EventMonthForYear == 0)
        {
            if (session.IsMultiplayer && session.RoomCode != null &&
                _roomMarketStates.TryGetValue(session.RoomCode, out var ms))
            {
                session.EventMonthForYear = ms.EventMonthPerYear.GetValueOrDefault(session.CurrentYear, _random.Next(7, 11));
            }
            else
            {
                session.EventMonthForYear = _random.Next(7, 11);
            }
            session.EventOccurredThisYear = false;
        }

        // Check for random event (specific event years only)
        if (GameSession.EventYears.Contains(session.CurrentYear) && session.CurrentMonth == session.EventMonthForYear && !session.EventOccurredThisYear)
        {
            TriggerMonthlyEvent(session);
            session.EventOccurredThisYear = true;
            return null;
        }

        if (session.CurrentMonth > GameSession.MONTHS_PER_YEAR)
        {
            session.CurrentMonth = 1;
            session.CurrentYear++;

            // Refresh deposito and bond rates for the new year (per-session, real historical data)
            session.CurrentDepositoRates = RefreshDepositoRates(session.CurrentYear);
            session.CurrentBondRates = RefreshBondRates(session.CurrentYear);

            if (session.IsMultiplayer && session.RoomCode != null &&
                _roomMarketStates.TryGetValue(session.RoomCode, out var rms))
            {
                session.EventMonthForYear = rms.EventMonthPerYear.GetValueOrDefault(session.CurrentYear, _random.Next(7, 11));
            }
            else
            {
                session.EventMonthForYear = _random.Next(7, 11);
            }
            session.EventOccurredThisYear = false;

            session.CashBalance += GameSession.YEARLY_INCOME;
            session.AddLogEntry(session.Language == Language.Indonesian
                ? $"Terima gaji tahunan: Rp {GameSession.YEARLY_INCOME:N0}"
                : $"Annual income received: Rp {GameSession.YEARLY_INCOME:N0}");

            // Refresh dividend data for the year just completed (CurrentYear-1),
            // since UpdateStockPrices' month==1 refresh doesn't trigger (month goes 13→wrap→1, never seen by UpdateStockPrices)
            foreach (var stock in session.AvailableStocks)
            {
                var div = _stockData.GetDividend(stock.Ticker, session.CurrentYear - 1);
                stock.AnnualDividendPerShare = div?.amount ?? 0;
                stock.DividendType = div?.type ?? "None";
            }

            // Pay dividends for stocks held
            PayStockDividends(session);

            // Now refresh to new year's dividend data for UI display
            foreach (var stock in session.AvailableStocks)
            {
                var div = _stockData.GetDividend(stock.Ticker, session.CurrentYear);
                stock.AnnualDividendPerShare = div?.amount ?? 0;
                stock.DividendType = div?.type ?? "None";
            }

            var yearEndUnlock = CheckAssetUnlocks(session);
            if (yearEndUnlock != null)
            {
                ProcessBotMonthEnd(session);
                return yearEndUnlock;
            }
        }

        // Check for mid-year unlock (deposito at month 6)
        string? midYearUnlock = null;
        if (session.CurrentYear == 1 && session.CurrentMonth == 6)
        {
            midYearUnlock = CheckAssetUnlocks(session);
        }

        if (session.CurrentYear > GameSession.MAX_YEARS)
        {
            session.IsGameOver = true;
            session.AddLogEntry(session.Language == Language.Indonesian
                ? $"Permainan selesai! Kekayaan akhir: Rp {session.NetWorth:N0}"
                : $"Game complete! Final net worth: Rp {session.NetWorth:N0}");
            _behaviorLog.EndSession(session.ConnectionId, ComputeTotalProfitLoss(session));
        }

        // Process bot month end (same month as player)
        ProcessBotMonthEnd(session);
        return midYearUnlock;
    }

    private void UpdatePortfolioValues(GameSession session)
    {
        foreach (var portfolio in session.Portfolio.Values)
        {
            if (portfolio.AssetType == "saham")
            {
                var ticker = portfolio.Ticker;
                var stock = session.AvailableStocks.FirstOrDefault(s => s.Ticker == ticker);
                if (stock != null)
                    portfolio.PricePerUnit = stock.CurrentPrice;
            }
            else if (portfolio.AssetType == "reksadana" && !string.IsNullOrEmpty(portfolio.Ticker))
            {
                // Index fund - price tracked via AvailableIndices
                var idx = session.AvailableIndices.FirstOrDefault(i => i.IndexId == portfolio.Ticker);
                if (idx != null)
                    portfolio.PricePerUnit = idx.CurrentPrice;
            }
            else if (portfolio.AssetType == "crypto")
            {
                var symbol = portfolio.Ticker;
                var crypto = session.AvailableCryptos.FirstOrDefault(c => c.Symbol == symbol);
                if (crypto != null)
                    portfolio.PricePerUnit = crypto.CurrentPrice;
            }
            else if (session.AssetPrices.ContainsKey(portfolio.AssetType))
            {
                portfolio.PricePerUnit = session.AssetPrices[portfolio.AssetType];
            }
        }
    }

    /// <summary>
    /// Process crowdfunding investments each month:
    /// - Decrement lock-up period
    /// - Check for 20% chance of project failure each month
    /// - Return principal + returns on maturity
    /// </summary>
    private void ProcessCrowdfundingMonthEnd(GameSession session)
    {
        // Clear any previous failure message
        session.CrowdfundingFailureMessage = null;

        var failedProjects = new List<string>();

        foreach (var investment in session.CrowdfundingInvestments.ToList())
        {
            // Skip already failed investments
            if (investment.HasFailed) continue;

            investment.MonthsRemaining--;

            // Risk-based failure: risk score 1 → 5% annual, score 10 → 25% annual
            var project = _allCrowdfundingProjects.FirstOrDefault(p => p.ProjectId == investment.ProjectId);
            int riskScore = project?.RiskLevel ?? 5;
            double annualFailRate = 0.05 + (riskScore - 1) * (0.20 / 9.0);
            double monthlyFailRate = 1.0 - Math.Pow(1.0 - annualFailRate, 1.0 / 12.0);

            if (_random.NextDouble() < monthlyFailRate)
            {
                investment.HasFailed = true;
                var failureReasons = new[]
                {
                    "kebangkrutan karena salah kelola keuangan",
                    "gagal mencapai target pendanaan",
                    "pasar tidak menerima produk",
                    "masalah regulasi pemerintah",
                    "persaingan bisnis yang ketat",
                    "pandemi atau bencana alam",
                    "penipuan oleh pengelola"
                };
                investment.FailureReason = failureReasons[_random.Next(failureReasons.Length)];
                session.TotalRealizedCrowdfundingGainLoss -= investment.InvestedAmount; // Total loss

                failedProjects.Add(investment.ProjectName);

                session.AddLogEntry(session.Language == Language.Indonesian
                    ? $"😢 GAGAL! Proyek {investment.ProjectName} bangkrut! Investasi Rp {investment.InvestedAmount:N0} hilang!"
                    : $"❌ FAILURE! {investment.ProjectName} failed due to {investment.FailureReason}. Investment of Rp {investment.InvestedAmount:N0} lost!");
            }
            else if (investment.IsMatured)
            {
                // Investment matured successfully - return with profit
                var totalReturn = investment.InvestedAmount * (1 + investment.ExpectedReturn * investment.LockUpMonths / 12);
                session.CashBalance += totalReturn;
                session.TotalRealizedCrowdfundingGainLoss += totalReturn - investment.InvestedAmount;

                session.AddLogEntry(session.Language == Language.Indonesian
                    ? $"🎉 Sukses! Investasi {investment.ProjectName} jatuh tempo! +Rp {totalReturn:N0}"
                    : $"✅ SUCCESS! {investment.ProjectName} matured! +Rp {totalReturn:N0}");

                session.CrowdfundingInvestments.Remove(investment);
            }
        }

        // Set failure message for UI notification
        if (failedProjects.Any())
        {
            session.CrowdfundingFailureMessage = session.Language == Language.Indonesian
                ? $"😢 Oh tidak! Proyek {string.Join(", ", failedProjects)} GAGAL! Uangmu hilang!"
                : $"❌ Project failure: {string.Join(", ", failedProjects)}. Your investment is lost.";
        }
    }

    /// <summary>
    /// Pay annual dividends for all stocks held by player.
    /// Uses real per-share dividend data: Dividend = Units × AnnualDividendPerShare
    /// </summary>
    private void PayStockDividends(GameSession session)
    {
        decimal totalDividends = 0;
        var dividendDetails = new List<string>();

        foreach (var portfolio in session.Portfolio.Values.Where(p => p.AssetType == "saham"))
        {
            var stock = session.AvailableStocks.FirstOrDefault(s => s.Ticker == portfolio.Ticker);
            if (stock != null && stock.AnnualDividendPerShare > 0)
            {
                // Dividend = shares × annual dividend per share (from real historical data)
                var dividend = Math.Round(portfolio.Units * stock.AnnualDividendPerShare, 0);
                if (dividend > 0)
                {
                    totalDividends += dividend;
                    dividendDetails.Add($"{stock.Ticker}: Rp {dividend:N0} ({stock.DividendType})");
                }
            }
        }

        if (totalDividends > 0)
        {
            session.CashBalance += totalDividends;
            session.TotalDividendEarned += totalDividends;
            session.AddLogEntry(session.Language == Language.Indonesian
                ? $"Terima dividen saham! +Rp {totalDividends:N0} ({string.Join(", ", dividendDetails)})"
                : $"Stock dividends received: +Rp {totalDividends:N0} ({string.Join(", ", dividendDetails)})");
        }
    }

    /// <summary>
    /// Process bot's monthly updates including:
    /// - Savings interest
    /// - Deposito maturity
    /// - Bond maturity and coupon payments
    /// - Crypto price tracking
    /// - CrowdFunding maturity
    /// - Investment rebalancing when assets unlock
    /// - Yearly income allocation
    /// - Market-aware stock picking with momentum + volatility filtering
    /// Bot uses aggressive stock-heavy strategy with future data advantage:
    /// 5% Savings, 20% Deposito, 5% Bonds, 10% Index Fund, 40% Stocks, 5% Gold, 10% Crypto, 5% CrowdFunding
    /// </summary>
    private void ProcessBotMonthEnd(GameSession session)
    {
        // Apply savings interest (1% annually = 0.0833% monthly)
        if (session.BotSavingsBalance > 0)
        {
            var monthlyInterest = session.BotSavingsBalance * (0.01m / 12);
            session.BotSavingsBalance += monthlyInterest;
        }

        // Grow bot stock value using real price changes
        if (session.BotStockValue > 0 && !string.IsNullOrEmpty(session.BotStockTicker))
        {
            var currentPrice = _stockData.GetPrice(session.BotStockTicker, session.CurrentYear, session.CurrentMonth);
            var prevMonth = session.CurrentMonth - 1;
            var prevYear = session.CurrentYear;
            if (prevMonth < 1) { prevMonth = 12; prevYear--; }
            var prevPrice = _stockData.GetPrice(session.BotStockTicker, prevYear, prevMonth);

            if (currentPrice.HasValue && prevPrice.HasValue && prevPrice.Value > 0)
            {
                var changeRatio = currentPrice.Value / prevPrice.Value;
                session.BotStockValue *= changeRatio;
            }
        }

        // Bot uses future data to sell before crashes and switch to better stocks
        if (session.TotalGameMonths >= 37)
        {
            BotFutureAwareStockTrading(session);

            // Re-buy stocks if bot sold (sitting in cash) and a good entry appears
            if (session.BotStockValue == 0 && string.IsNullOrEmpty(session.BotStockTicker)
                && session.AvailableStocks.Count > 0)
            {
                var bestStock = PickBestStock(session);
                var bestPrice = _stockData.GetPrice(bestStock.Ticker, session.CurrentYear, session.CurrentMonth);
                // Look 3 months ahead — only re-enter if positive return ahead
                var fm = session.CurrentMonth + 3;
                var fy = session.CurrentYear;
                if (fm > 12) { fm -= 12; fy++; }
                var futurePrice = _stockData.GetPrice(bestStock.Ticker, fy, fm);
                if (bestPrice.HasValue && futurePrice.HasValue && bestPrice.Value > 0
                    && (futurePrice.Value - bestPrice.Value) / bestPrice.Value > 0.05m)
                {
                    var reserve = GetBotDynamicReserve(session);
                    var stockAmount = Math.Max(0, session.BotCashBalance - reserve);
                    stockAmount = Math.Min(stockAmount, session.BotNetWorth * 0.40m); // 40% target
                    if (stockAmount >= 100_000m)
                    {
                        session.BotStockTicker = bestStock.Ticker;
                        session.BotStockCost = stockAmount;
                        session.BotStockValue = stockAmount;
                        session.BotCashBalance -= stockAmount;
                    }
                }
            }
        }

        // Process bot depositos - check maturity (reinvest immediately)
        bool hadMaturity = false;
        foreach (var deposito in session.BotDepositos.ToList())
        {
            deposito.MonthsRemaining--;
            if (deposito.IsMatured)
            {
                session.BotCashBalance += deposito.MaturityValue;
                session.BotDepositos.Remove(deposito);
                hadMaturity = true;
            }
        }

        // Process bot bonds - check maturity (reinvest immediately)
        foreach (var bond in session.BotBonds.ToList())
        {
            bond.MonthsRemaining--;
            if (bond.IsMatured)
            {
                session.BotCashBalance += bond.CurrentValue;
                session.BotBonds.Remove(bond);
                hadMaturity = true;
            }
        }

        // Process bot crowdfunding investments - check maturity
        foreach (var cf in session.BotCrowdfundingInvestments.ToList())
        {
            cf.MonthsRemaining--;
            if (cf.IsMatured)
            {
                if (!cf.HasFailed)
                    session.BotCashBalance += cf.CurrentValue;
                session.BotCrowdfundingInvestments.Remove(cf);
                hadMaturity = true;
            }
        }

        var totalMonths = session.TotalGameMonths;

        // Month 1 (game start): Park cash in savings immediately
        if (totalMonths == 1 && session.BotSavingsBalance == 0)
        {
            var savingsAmount = session.BotCashBalance - 500_000m;
            if (savingsAmount > 0)
            {
                session.BotSavingsBalance += savingsAmount;
                session.BotCashBalance -= savingsAmount;
            }
        }

        // Month 6: Deposito unlocks - 20% target allocation (largest fixed-income)
        if (totalMonths == 6 && session.BotDepositos.Count == 0)
        {
            var available = session.BotCashBalance + session.BotSavingsBalance - 500_000m;
            var depositoAmount = Math.Min(available * 0.20m, 2_000_000m);
            depositoAmount = Math.Max(depositoAmount, 1_000_000m);
            if (depositoAmount >= 1_000_000m && available >= depositoAmount)
            {
                if (session.BotCashBalance < depositoAmount)
                {
                    var fromSavings = Math.Min(session.BotSavingsBalance, depositoAmount - session.BotCashBalance);
                    session.BotSavingsBalance -= fromSavings;
                    session.BotCashBalance += fromSavings;
                }
                var rate = session.CurrentDepositoRates.FirstOrDefault(r => r.PeriodMonths == 12);
                if (rate != null)
                {
                    session.BotDepositos.Add(new DepositoItem
                    {
                        Principal = depositoAmount,
                        PeriodMonths = 12,
                        InterestRate = rate.AnnualRate,
                        StartYear = session.CurrentYear,
                        StartMonth = session.CurrentMonth,
                        MonthsRemaining = 12
                    });
                    session.BotCashBalance -= depositoAmount;
                }
            }
        }

        // Month 13: Index Fund unlocks - 20% target allocation
        if (totalMonths == 13 && session.BotIndexFundUnits == 0)
        {
            var reserve = GetBotDynamicReserve(session);
            var indexAmount = Math.Max(0, session.BotCashBalance - reserve);
            indexAmount = Math.Min(indexAmount, 5_000_000m);
            if (indexAmount >= 100_000m)
            {
                var convIdx = session.AvailableIndices.FirstOrDefault(i => !i.IsShariah);
                var price = convIdx?.CurrentPrice ?? session.AssetPrices.GetValueOrDefault("reksadana", 1_000_000m);
                if (price > 0)
                {
                    var units = indexAmount / price;
                    session.BotIndexFundUnits = units;
                    session.BotIndexFundCost = indexAmount;
                    session.BotCashBalance -= indexAmount;
                }
            }
        }

        // Month 25: Bonds unlock - 10% target, invest in SR (shariah) or ORI (conventional)
        if (totalMonths == 25 && session.BotBonds.Count == 0)
        {
            var reserve = GetBotDynamicReserve(session);
            var available = Math.Max(0, session.BotCashBalance - reserve);
            var bondAmount = Math.Min(available * 0.50m, 2_000_000m);
            if (bondAmount >= 1_000_000m)
            {
                var rate = session.CurrentBondRates.FirstOrDefault(r => r.BondType == "SR")
                        ?? session.CurrentBondRates.FirstOrDefault(r => r.BondType == "ORI");
                if (rate != null)
                {
                    session.BotBonds.Add(new BondItem
                    {
                        BondName = rate.PeriodName,
                        Principal = bondAmount,
                        PeriodMonths = rate.PeriodMonths,
                        CouponRate = rate.CouponRate,
                        StartYear = session.CurrentYear,
                        StartMonth = session.CurrentMonth,
                        MonthsRemaining = rate.PeriodMonths,
                        IsShariah = rate.IsShariah,
                        SeriesName = rate.SeriesName,
                        AkadType = rate.AkadType
                    });
                    session.BotCashBalance -= bondAmount;
                }
            }
        }

        // Month 37 (Year 4): Stocks unlock - 40% target, future-aware stock picking
        if (totalMonths == 37 && session.BotStockCost == 0)
        {
            var reserve = GetBotDynamicReserve(session);
            var stockAmount = Math.Max(0, session.BotCashBalance - reserve);
            stockAmount = Math.Min(stockAmount, session.BotNetWorth * 0.40m); // 40% of net worth
            if (stockAmount >= 100_000m && session.AvailableStocks.Count > 0)
            {
                var bestStock = PickBestStock(session);
                session.BotStockTicker = bestStock.Ticker;
                session.BotStockCost = stockAmount;
                session.BotStockValue = stockAmount;
                session.BotCashBalance -= stockAmount;
            }
        }

        // Month 49: Gold unlocks - 10% target allocation
        if (totalMonths == 49 && session.BotGoldUnits == 0)
        {
            var reserve = GetBotDynamicReserve(session);
            var goldAmount = Math.Max(0, session.BotCashBalance - reserve);
            goldAmount = Math.Min(goldAmount, 3_000_000m);
            if (goldAmount >= 50_000m)
            {
                var price = session.AssetPrices.GetValueOrDefault("emas", 1_200_000m);
                var units = goldAmount / price;
                session.BotGoldUnits = units;
                session.BotGoldCost = goldAmount;
                session.BotCashBalance -= goldAmount;
            }
        }

        // Month 66 (Y6M6): CrowdFunding unlocks - 5% target, pick medium-risk project
        if (totalMonths == 66 && session.BotCrowdfundingInvestments.Count == 0)
        {
            var reserve = GetBotDynamicReserve(session);
            var cfAmount = Math.Max(0, session.BotCashBalance - reserve);
            cfAmount = Math.Min(cfAmount, 1_500_000m);
            if (cfAmount >= 100_000m && session.AvailableCrowdfunding.Count > 0)
            {
                // Pick a medium-risk project with reasonable tenor
                var project = session.AvailableCrowdfunding
                    .Where(p => p.IsActive && p.RiskLevel >= 3 && p.RiskLevel <= 6)
                    .OrderBy(p => p.LockUpMonths)
                    .FirstOrDefault() ?? session.AvailableCrowdfunding.FirstOrDefault(p => p.IsActive);
                if (project != null)
                {
                    session.BotCrowdfundingInvestments.Add(new CrowdfundingInvestment
                    {
                        ProjectId = project.ProjectId,
                        ProjectName = project.ProjectName,
                        ProjectType = project.ProjectType,
                        InvestedAmount = cfAmount,
                        ExpectedReturn = project.ExpectedReturn,
                        StartYear = session.CurrentYear,
                        StartMonth = session.CurrentMonth,
                        LockUpMonths = project.LockUpMonths,
                        MonthsRemaining = project.LockUpMonths,
                        HasFailed = false
                    });
                    session.BotCashBalance -= cfAmount;
                }
            }
        }

        // Month 133 (Y12M1): Crypto unlocks - 10% target, invest in BTC
        if (totalMonths == 133 && session.BotCryptoUnits == 0)
        {
            var reserve = GetBotDynamicReserve(session);
            var cryptoAmount = Math.Max(0, session.BotCashBalance - reserve);
            // Cap at 10% of current net worth
            var maxCrypto = session.BotNetWorth * 0.10m;
            cryptoAmount = Math.Min(cryptoAmount, maxCrypto);
            cryptoAmount = Math.Min(cryptoAmount, 5_000_000m);
            if (cryptoAmount >= 100_000m)
            {
                var btc = session.AvailableCryptos.FirstOrDefault(c => c.Symbol == "BTC");
                var price = btc?.CurrentPrice ?? 0;
                if (price > 0)
                {
                    var units = cryptoAmount / price;
                    session.BotCryptoUnits = units;
                    session.BotCryptoCost = cryptoAmount;
                    session.BotCashBalance -= cryptoAmount;
                }
            }
        }

        // On year end (when player receives income), bot also receives and reinvests
        if (session.CurrentMonth == 1 && session.CurrentYear > 1)
        {
            session.BotCashBalance += GameSession.YEARLY_INCOME;

            // Pay bot stock dividends (same as player)
            if (session.BotStockValue > 0 && !string.IsNullOrEmpty(session.BotStockTicker))
            {
                var stock = session.AvailableStocks.FirstOrDefault(s => s.Ticker == session.BotStockTicker);
                if (stock != null && stock.AnnualDividendPerShare > 0)
                {
                    var effectiveShares = stock.CurrentPrice > 0 ? session.BotStockValue / stock.CurrentPrice : 0;
                    var botDividend = Math.Round(effectiveShares * stock.AnnualDividendPerShare, 0);
                    if (botDividend > 0)
                    {
                        session.BotCashBalance += botDividend;
                    }
                }
            }

            // Enforce 20% deposito floor: if deposito < 20% of net worth, top up
            EnforceBotDepositoFloor(session);

            RebalanceBotPortfolio(session);

            // Generate advisory tips for the player at year-end
            GenerateAdvisorTips(session);
        }
        // Continuous deployment: if maturity freed up cash or excess cash accumulated
        else if (hadMaturity || session.BotCashBalance > GetBotDynamicReserve(session) + 1_000_000m)
        {
            RebalanceBotPortfolio(session);
        }
    }

    /// <summary>
    /// Future-aware stock picking: bot peeks at the next 12 months of real historical data
    /// to select the stock with the best forward return. This makes the bot extremely hard to beat.
    /// </summary>
    private StockInfo PickBestStock(GameSession session)
    {
        var candidates = new List<(StockInfo stock, decimal forwardReturn)>();

        foreach (var stock in session.AvailableStocks)
        {
            var currentPrice = _stockData.GetPrice(stock.Ticker, session.CurrentYear, session.CurrentMonth);
            if (!currentPrice.HasValue || currentPrice.Value <= 0) continue;

            // Look ahead 12 months to pick the best performer
            var futureYear = session.CurrentYear + 1;
            var futurePrice = _stockData.GetPrice(stock.Ticker, futureYear, session.CurrentMonth);
            if (!futurePrice.HasValue)
            {
                // Try 6 months ahead as fallback
                var fm = session.CurrentMonth + 6;
                var fy = session.CurrentYear;
                if (fm > 12) { fm -= 12; fy++; }
                futurePrice = _stockData.GetPrice(stock.Ticker, fy, fm);
            }

            decimal forwardReturn = 0;
            if (futurePrice.HasValue && currentPrice.Value > 0)
                forwardReturn = (futurePrice.Value - currentPrice.Value) / currentPrice.Value;

            // Also add dividend yield bonus
            if (stock.AnnualDividendPerShare > 0 && currentPrice.Value > 0)
                forwardReturn += stock.AnnualDividendPerShare / currentPrice.Value;

            candidates.Add((stock, forwardReturn));
        }

        if (candidates.Count > 0)
            return candidates.OrderByDescending(c => c.forwardReturn).First().stock;

        return session.AvailableStocks.OrderByDescending(s => s.AnnualDividendPerShare).First();
    }

    /// <summary>
    /// Bot uses future data to decide whether to sell stocks before a crash.
    /// If the stock price will drop >15% in the next 3 months, sell and re-buy later.
    /// Also switches to a better-performing stock if available.
    /// </summary>
    private void BotFutureAwareStockTrading(GameSession session)
    {
        if (session.BotStockValue <= 0 || string.IsNullOrEmpty(session.BotStockTicker)) return;

        var currentPrice = _stockData.GetPrice(session.BotStockTicker, session.CurrentYear, session.CurrentMonth);
        if (!currentPrice.HasValue || currentPrice.Value <= 0) return;

        // Look ahead 3 months for crash detection
        var fm = session.CurrentMonth + 3;
        var fy = session.CurrentYear;
        if (fm > 12) { fm -= 12; fy++; }
        var futurePrice3M = _stockData.GetPrice(session.BotStockTicker, fy, fm);

        if (futurePrice3M.HasValue && futurePrice3M.Value > 0)
        {
            var forwardChange = (futurePrice3M.Value - currentPrice.Value) / currentPrice.Value;

            // Sell if crash coming (>15% drop ahead)
            if (forwardChange < -0.15m)
            {
                session.BotCashBalance += session.BotStockValue;
                session.BotStockValue = 0;
                session.BotStockCost = 0;
                session.BotStockTicker = string.Empty;
                return;
            }
        }

        // Check if a different stock will outperform — switch if >20% better forward return
        var bestStock = PickBestStock(session);
        if (bestStock.Ticker != session.BotStockTicker)
        {
            var bestPrice = _stockData.GetPrice(bestStock.Ticker, session.CurrentYear, session.CurrentMonth);
            var bestFuture = _stockData.GetPrice(bestStock.Ticker, fy, fm);
            var currentFuture = futurePrice3M;

            if (bestPrice.HasValue && bestFuture.HasValue && bestPrice.Value > 0 &&
                currentPrice.HasValue && currentFuture.HasValue && currentPrice.Value > 0)
            {
                var bestReturn = (bestFuture.Value - bestPrice.Value) / bestPrice.Value;
                var currentReturn = (currentFuture.Value - currentPrice.Value) / currentPrice.Value;

                if (bestReturn - currentReturn > 0.20m)
                {
                    // Switch: sell current, buy best
                    session.BotCashBalance += session.BotStockValue;
                    var investAmount = session.BotStockValue;
                    session.BotStockTicker = bestStock.Ticker;
                    session.BotStockCost = investAmount;
                    session.BotStockValue = investAmount;
                    session.BotCashBalance -= investAmount;
                }
            }
        }
    }

    /// <summary>
    /// Enforce 20% deposito floor: ensure bot always has at least 20% of net worth in deposito.
    /// Called at year-end before rebalancing.
    /// </summary>
    private void EnforceBotDepositoFloor(GameSession session)
    {
        var botNetWorth = session.BotNetWorth;
        if (botNetWorth <= 0) return;

        var depositoValue = session.BotTotalDepositoValue;
        var targetDeposito = botNetWorth * 0.20m;
        var deficit = targetDeposito - depositoValue;

        if (deficit < 1_000_000m) return; // Close enough or already above target

        var available = session.BotCashBalance - GetBotDynamicReserve(session);
        var depositoTopUp = Math.Min(deficit, available);
        if (depositoTopUp < 1_000_000m) return;

        var monthsRemaining = (GameSession.MAX_YEARS * 12) - session.TotalGameMonths;
        int tenor = monthsRemaining >= 24 ? 24 : monthsRemaining >= 12 ? 12 : Math.Max(1, monthsRemaining);

        var rate = session.CurrentDepositoRates.FirstOrDefault(r => r.PeriodMonths == tenor)
                ?? session.CurrentDepositoRates.FirstOrDefault(r => r.PeriodMonths == 12);
        if (rate != null)
        {
            session.BotDepositos.Add(new DepositoItem
            {
                Principal = depositoTopUp,
                PeriodMonths = tenor,
                InterestRate = rate.AnnualRate,
                StartYear = session.CurrentYear,
                StartMonth = session.CurrentMonth,
                MonthsRemaining = tenor
            });
            session.BotCashBalance -= depositoTopUp;
        }
    }

    /// <summary>
    /// Generate contextual advisory tips for the player at year-end.
    /// Tips are based on portfolio analysis and market data conditions.
    /// </summary>
    private void GenerateAdvisorTips(GameSession session)
    {
        session.AdvisorTips.Clear();
        if (session.CurrentYear < 2) return; // No tips in first year

        var netWorth = session.NetWorth;
        if (netWorth <= 0) return;

        var cashRatio = session.CashBalance / netWorth;
        var savingsRatio = session.TotalSavingsValue / netWorth;

        // Tip 1: Too much idle cash (>70% in cash+savings at Y2+)
        if (cashRatio + savingsRatio > 0.70m && session.CurrentYear >= 2)
        {
            session.AdvisorTips.Add(new AdvisorTip
            {
                Message = "Uang idle sebaiknya diinvestasikan. Pertimbangkan Deposito atau Reksa Dana untuk imbal hasil lebih baik.",
                MessageEN = "Idle cash should be invested. Consider Deposito or Index Funds for better returns.",
                Category = "suggestion"
            });
        }

        // Tip 2: Deposit rate dropped >1% year-over-year
        if (session.CurrentYear >= 3)
        {
            var currentRate = _depositoData.GetConventionalRate(session.CurrentYear, 12);
            var prevRate = _depositoData.GetConventionalRate(session.CurrentYear - 1, 12);
            if (currentRate.HasValue && prevRate.HasValue && (prevRate.Value - currentRate.Value) > 0.01m)
            {
                session.AdvisorTips.Add(new AdvisorTip
                {
                    Message = "Suku bunga deposito turun tahun ini. Obligasi bisa menjadi alternatif yang lebih menguntungkan.",
                    MessageEN = "Deposit rates dropped this year. Bonds could be a more profitable alternative.",
                    Category = "info"
                });
            }
        }

        // Tip 3: Stock 12-month momentum >20%
        foreach (var stock in session.AvailableStocks)
        {
            var currentPrice = _stockData.GetPrice(stock.Ticker, session.CurrentYear, session.CurrentMonth);
            var price12MAgo = _stockData.GetPrice(stock.Ticker, session.CurrentYear - 1, session.CurrentMonth);
            if (currentPrice.HasValue && price12MAgo.HasValue && price12MAgo.Value > 0)
            {
                var momentum = (currentPrice.Value - price12MAgo.Value) / price12MAgo.Value;
                if (momentum > 0.20m)
                {
                    session.AdvisorTips.Add(new AdvisorTip
                    {
                        Message = $"Saham {stock.Ticker} naik {(momentum * 100):F0}% dalam 12 bulan terakhir. Hati-hati valuasi tinggi.",
                        MessageEN = $"Stock {stock.Ticker} rose {(momentum * 100):F0}% in the last 12 months. Watch for high valuation.",
                        Category = "warning"
                    });
                    break; // Only show one stock tip
                }
            }
        }

        // Tip 4: Gold price YoY >30%
        var goldNow = _goldData.GetPrice(session.CurrentYear, session.CurrentMonth);
        var goldPrev = _goldData.GetPrice(session.CurrentYear - 1, session.CurrentMonth);
        if (goldNow.HasValue && goldPrev.HasValue && goldPrev.Value > 0)
        {
            var goldChange = (goldNow.Value - goldPrev.Value) / goldPrev.Value;
            if (goldChange > 0.30m)
            {
                session.AdvisorTips.Add(new AdvisorTip
                {
                    Message = "Harga emas naik tajam tahun ini. Ini bisa jadi waktu tepat untuk mengambil keuntungan.",
                    MessageEN = "Gold prices surged this year. This could be a good time to take profits.",
                    Category = "warning"
                });
            }
        }

        // Tip 5: Portfolio concentration (>60% in one asset class)
        var portfolioValue = session.TotalPortfolioValue;
        var depositoValue = session.TotalDepositoValue;
        var bondValue = session.TotalBondValue;
        var assetValues = new[] { session.CashBalance + session.TotalSavingsValue, depositoValue, bondValue, portfolioValue };
        foreach (var v in assetValues)
        {
            if (netWorth > 0 && v / netWorth > 0.60m)
            {
                session.AdvisorTips.Add(new AdvisorTip
                {
                    Message = "Portofolio Anda terkonsentrasi pada satu jenis aset. Diversifikasi bisa mengurangi risiko.",
                    MessageEN = "Your portfolio is concentrated in one asset class. Diversification can reduce risk.",
                    Category = "suggestion"
                });
                break;
            }
        }

        // Keep max 3 tips to avoid overwhelming
        if (session.AdvisorTips.Count > 3)
            session.AdvisorTips = session.AdvisorTips.Take(3).ToList();
    }

    /// <summary>
    /// Calculate dynamic cash reserve based on event timing.
    /// Keep more liquid before events, minimal otherwise.
    /// </summary>
    private decimal GetBotDynamicReserve(GameSession session)
    {
        var isEventYear = GameSession.EventYears.Contains(session.CurrentYear);

        if (!isEventYear)
            return 500_000m;

        // Event already occurred this year - minimal reserve
        if (session.EventOccurredThisYear)
            return 500_000m;

        // Pre-event months (1-6): keep reserve for upcoming event
        if (session.CurrentMonth <= 6)
            return 3_000_000m;

        // Event window (7-10) but not yet occurred: keep reserve
        if (session.CurrentMonth <= 10)
            return 3_000_000m;

        // Post-event window (11-12): event should have occurred, minimal reserve
        return 500_000m;
    }

    /// <summary>
    /// Rebalance bot portfolio with aggressive stock-heavy strategy.
    /// Target: 40% Stocks, 20% Deposito, 10% Index Fund, 10% Crypto, 5% Bonds, 5% Gold, 5% CrowdFunding, 5% Savings
    /// Uses dynamic event-aware reserve and redistributes locked allocations.
    /// </summary>
    private void RebalanceBotPortfolio(GameSession session)
    {
        var availableCash = session.BotCashBalance;
        var totalMonths = session.TotalGameMonths;
        var monthsRemaining = (GameSession.MAX_YEARS * 12) - totalMonths;

        // Dynamic reserve based on event timing
        var reserveAmount = GetBotDynamicReserve(session);
        var investableAmount = Math.Max(0, availableCash - reserveAmount);

        if (investableAmount < 500_000m) return;

        // Calculate effective allocation percentages based on what's unlocked
        // Target: 40% Stocks, 20% Deposito, 10% Index Fund, 10% Crypto, 5% Bonds, 5% Gold, 5% CrowdFunding, 5% Savings
        bool indexUnlocked = totalMonths >= 13;
        bool bondsUnlocked = totalMonths >= 25;
        bool stocksUnlocked = totalMonths >= 37;
        bool goldUnlocked = totalMonths >= 49;
        bool crowdfundingUnlocked = totalMonths >= 66;
        bool cryptoUnlocked = totalMonths >= 133;
        bool depositoUnlocked = totalMonths >= 6;

        decimal stockPct = stocksUnlocked ? 0.40m : 0;
        decimal depositoPct = depositoUnlocked ? 0.20m : 0;
        decimal indexPct = indexUnlocked ? 0.10m : 0;
        decimal cryptoPct = cryptoUnlocked ? 0.10m : 0;
        decimal bondsPct = bondsUnlocked ? 0.05m : 0;
        decimal goldPct = goldUnlocked ? 0.05m : 0;
        decimal crowdfundingPct = crowdfundingUnlocked ? 0.05m : 0;
        decimal savingsPct = 0.05m;

        // Redistribute locked allocations to best available asset
        decimal totalAllocated = indexPct + stockPct + goldPct + bondsPct + depositoPct + cryptoPct + crowdfundingPct + savingsPct;
        decimal unallocated = 1.0m - totalAllocated;
        if (unallocated > 0.001m)
        {
            if (indexUnlocked)
                indexPct += unallocated; // Index Fund gets priority for redistribution
            else if (depositoUnlocked)
                depositoPct += unallocated;
            else
                savingsPct += unallocated;
        }

        // 1. Deposito (20% target - highest priority fixed income)
        if (depositoUnlocked && investableAmount >= 1_000_000m)
        {
            var depositoAmount = investableAmount * depositoPct;
            if (depositoAmount >= 1_000_000m)
            {
                int tenor = monthsRemaining >= 24 ? 24 : 12;
                if (monthsRemaining < 12) tenor = Math.Max(1, monthsRemaining);
                var rate = session.CurrentDepositoRates.FirstOrDefault(r => r.PeriodMonths == tenor)
                        ?? session.CurrentDepositoRates.FirstOrDefault(r => r.PeriodMonths == 12);
                if (rate != null)
                {
                    session.BotDepositos.Add(new DepositoItem
                    {
                        Principal = depositoAmount,
                        PeriodMonths = tenor,
                        InterestRate = rate.AnnualRate,
                        StartYear = session.CurrentYear,
                        StartMonth = session.CurrentMonth,
                        MonthsRemaining = tenor
                    });
                    session.BotCashBalance -= depositoAmount;
                    investableAmount -= depositoAmount;
                }
            }
        }

        // 2. Index Fund (20% target - core growth)
        if (indexUnlocked && investableAmount >= 100_000m)
        {
            var indexInvestment = investableAmount * (indexPct / Math.Max(0.01m, 1 - depositoPct));
            indexInvestment = Math.Min(indexInvestment, investableAmount);
            if (indexInvestment >= 100_000m)
            {
                var convIdx = session.AvailableIndices.FirstOrDefault(i => !i.IsShariah);
                var price = convIdx?.CurrentPrice ?? session.AssetPrices.GetValueOrDefault("reksadana", 1_000_000m);
                if (price > 0)
                {
                    var units = indexInvestment / price;
                    session.BotIndexFundUnits += units;
                    session.BotIndexFundCost += indexInvestment;
                    session.BotCashBalance -= indexInvestment;
                    investableAmount -= indexInvestment;
                }
            }
        }

        // 3. Stocks (40% target - aggressive, future-aware)
        if (stocksUnlocked && investableAmount >= 100_000m && session.BotStockCost > 0)
        {
            var stockInvestment = investableAmount * (stockPct / Math.Max(0.01m, 1 - depositoPct - indexPct));
            stockInvestment = Math.Min(stockInvestment, investableAmount);
            // Cap at 40% of net worth
            var stockCap = session.BotNetWorth * 0.40m - session.BotStockValue;
            if (stockCap > 0) stockInvestment = Math.Min(stockInvestment, stockCap);
            if (stockInvestment >= 100_000m)
            {
                session.BotStockCost += stockInvestment;
                session.BotStockValue += stockInvestment;
                session.BotCashBalance -= stockInvestment;
                investableAmount -= stockInvestment;
            }
        }

        // 4. Gold (10% target)
        if (goldUnlocked && investableAmount >= 50_000m)
        {
            var goldInvestment = investableAmount * (goldPct / Math.Max(0.01m, 1 - depositoPct - indexPct - stockPct));
            goldInvestment = Math.Min(goldInvestment, investableAmount);
            if (goldInvestment >= 50_000m)
            {
                var price = session.AssetPrices.GetValueOrDefault("emas", 1_200_000m);
                var units = goldInvestment / price;
                session.BotGoldUnits += units;
                session.BotGoldCost += goldInvestment;
                session.BotCashBalance -= goldInvestment;
                investableAmount -= goldInvestment;
            }
        }

        // 5. Bonds (10% target) - skip if < 24 months remaining
        if (bondsUnlocked && investableAmount >= 1_000_000m && monthsRemaining >= 24)
        {
            var bondAmount = investableAmount * (bondsPct / Math.Max(0.01m, 1 - depositoPct - indexPct - stockPct - goldPct));
            bondAmount = Math.Min(bondAmount, investableAmount);
            if (bondAmount >= 1_000_000m)
            {
                var rate = session.CurrentBondRates.FirstOrDefault(r => r.BondType == "SR")
                        ?? session.CurrentBondRates.FirstOrDefault(r => r.BondType == "ORI");
                if (rate != null)
                {
                    session.BotBonds.Add(new BondItem
                    {
                        BondName = rate.PeriodName,
                        Principal = bondAmount,
                        PeriodMonths = rate.PeriodMonths,
                        CouponRate = rate.CouponRate,
                        StartYear = session.CurrentYear,
                        StartMonth = session.CurrentMonth,
                        MonthsRemaining = rate.PeriodMonths,
                        IsShariah = rate.IsShariah,
                        SeriesName = rate.SeriesName,
                        AkadType = rate.AkadType
                    });
                    session.BotCashBalance -= bondAmount;
                    investableAmount -= bondAmount;
                }
            }
        }

        // 6. Crypto (10% target) - cap at 10% of net worth
        if (cryptoUnlocked && investableAmount >= 100_000m && session.BotCryptoUnits > 0)
        {
            var cryptoCap = session.BotNetWorth * 0.10m;
            var currentCryptoValue = session.BotCryptoValue;
            var cryptoRoom = cryptoCap - currentCryptoValue;
            if (cryptoRoom > 100_000m)
            {
                var cryptoInvestment = Math.Min(investableAmount * cryptoPct, cryptoRoom);
                if (cryptoInvestment >= 100_000m)
                {
                    var btc = session.AvailableCryptos.FirstOrDefault(c => c.Symbol == "BTC");
                    var price = btc?.CurrentPrice ?? 0;
                    if (price > 0)
                    {
                        var units = cryptoInvestment / price;
                        session.BotCryptoUnits += units;
                        session.BotCryptoCost += cryptoInvestment;
                        session.BotCashBalance -= cryptoInvestment;
                        investableAmount -= cryptoInvestment;
                    }
                }
            }
        }

        // 7. CrowdFunding (5% target) - reinvest if previous matured
        if (crowdfundingUnlocked && investableAmount >= 100_000m && session.BotCrowdfundingInvestments.Count == 0)
        {
            var cfInvestment = Math.Min(investableAmount * crowdfundingPct, 1_500_000m);
            if (cfInvestment >= 100_000m && session.AvailableCrowdfunding.Count > 0)
            {
                var project = session.AvailableCrowdfunding
                    .Where(p => p.IsActive && p.RiskLevel >= 3 && p.RiskLevel <= 6)
                    .OrderBy(p => p.LockUpMonths)
                    .FirstOrDefault() ?? session.AvailableCrowdfunding.FirstOrDefault(p => p.IsActive);
                if (project != null)
                {
                    session.BotCrowdfundingInvestments.Add(new CrowdfundingInvestment
                    {
                        ProjectId = project.ProjectId,
                        ProjectName = project.ProjectName,
                        ProjectType = project.ProjectType,
                        InvestedAmount = cfInvestment,
                        ExpectedReturn = project.ExpectedReturn,
                        StartYear = session.CurrentYear,
                        StartMonth = session.CurrentMonth,
                        LockUpMonths = project.LockUpMonths,
                        MonthsRemaining = project.LockUpMonths,
                        HasFailed = false
                    });
                    session.BotCashBalance -= cfInvestment;
                    investableAmount -= cfInvestment;
                }
            }
        }

        // 8. Savings - put small remainder as emergency buffer
        if (investableAmount >= 100_000m)
        {
            var savingsAmount = investableAmount * 0.50m;
            if (savingsAmount >= 100_000m)
            {
                session.BotSavingsBalance += savingsAmount;
                session.BotCashBalance -= savingsAmount;
            }
        }
    }

    /// <summary>
    /// Process bot event payment using balanced strategy
    /// Priority: Cash > Savings > Index Fund > Gold > Bonds > Deposito
    /// </summary>
    private void ProcessBotEventPayment(GameSession session, decimal eventCost)
    {
        var remaining = eventCost;
        session.BotTotalEventCostPaid += eventCost;

        // 1. Try to pay from cash first
        if (session.BotCashBalance >= remaining)
        {
            session.BotCashBalance -= remaining;
            session.BotEventsPaidFromCash++;
            return;
        }
        else if (session.BotCashBalance > 0)
        {
            remaining -= session.BotCashBalance;
            session.BotCashBalance = 0;
        }

        // 2. Try savings (emergency fund)
        if (session.BotSavingsBalance >= remaining)
        {
            session.BotSavingsBalance -= remaining;
            session.BotEventsPaidFromSavings++;
            return;
        }
        else if (session.BotSavingsBalance > 0)
        {
            remaining -= session.BotSavingsBalance;
            session.BotSavingsBalance = 0;
        }

        // 3. Try index fund (liquid asset)
        var indexFundValue = session.BotIndexFundValue;
        if (indexFundValue >= remaining)
        {
            var convIdx = session.AvailableIndices.FirstOrDefault(i => !i.IsShariah);
            var price = convIdx?.CurrentPrice ?? session.AssetPrices.GetValueOrDefault("reksadana", 1_000_000m);
            if (price > 0)
            {
                var unitsToSell = remaining / price;
                session.BotIndexFundUnits -= unitsToSell;
            }
            session.BotEventsPaidFromPortfolio++;
            return;
        }
        else if (indexFundValue > 0)
        {
            remaining -= indexFundValue;
            session.BotIndexFundUnits = 0;
        }

        // 4. Try stocks (liquid asset)
        if (session.BotStockValue >= remaining)
        {
            var ratio = remaining / session.BotStockValue;
            session.BotStockValue -= remaining;
            session.BotStockCost -= session.BotStockCost * ratio;
            session.BotEventsPaidFromPortfolio++;
            return;
        }
        else if (session.BotStockValue > 0)
        {
            remaining -= session.BotStockValue;
            session.BotStockValue = 0;
            session.BotStockCost = 0;
        }

        // 5. Try crypto (liquid digital asset)
        var cryptoValue = session.BotCryptoValue;
        if (cryptoValue >= remaining)
        {
            var btc = session.AvailableCryptos.FirstOrDefault(c => c.Symbol == "BTC");
            var price = btc?.CurrentPrice ?? 0;
            if (price > 0)
            {
                var unitsToSell = remaining / price;
                session.BotCryptoUnits -= unitsToSell;
            }
            session.BotEventsPaidFromPortfolio++;
            return;
        }
        else if (cryptoValue > 0)
        {
            remaining -= cryptoValue;
            session.BotCryptoUnits = 0;
        }

        // 6. Try gold (liquid commodity)
        var goldValue = session.BotGoldValue;
        if (goldValue >= remaining)
        {
            var price = session.AssetPrices.GetValueOrDefault("emas", 1_000_000m);
            var unitsToSell = remaining / price;
            session.BotGoldUnits -= unitsToSell;
            session.BotEventsPaidFromPortfolio++;
            return;
        }
        else if (goldValue > 0)
        {
            remaining -= goldValue;
            session.BotGoldUnits = 0;
        }

        // 8. Try bonds (early redemption with partial penalty)
        foreach (var bond in session.BotBonds.ToList())
        {
            if (remaining <= 0) break;

            // Early redemption returns principal + 50% of earned coupon
            var earnedCoupon = bond.TotalCouponEarned;
            var withdrawValue = bond.Principal + (earnedCoupon * 0.5m);

            session.BotBonds.Remove(bond);
            remaining -= withdrawValue;
            session.BotEventsPaidFromPortfolio++;
        }

        // 9. Last resort - liquidate depositos (early withdrawal with penalty)
        foreach (var deposito in session.BotDepositos.ToList())
        {
            if (remaining <= 0) break;

            // Early withdrawal with 50% interest penalty
            var earnedInterest = deposito.CurrentValue - deposito.Principal;
            var penalty = earnedInterest * 0.5m;
            var withdrawValue = deposito.Principal + earnedInterest - penalty;

            session.BotDepositos.Remove(deposito);
            remaining -= withdrawValue;
            session.BotEventsPaidFromPortfolio++;
        }
    }

    /// <summary>
    /// Check for newly unlocked assets. Returns the last unlocked asset type (for multiplayer sync),
    /// or null if no new unlocks occurred.
    /// </summary>
    private string? CheckAssetUnlocks(GameSession session)
    {
        var newUnlocks = new List<string>();
        var totalMonths = session.TotalGameMonths;
        string? lastUnlockedAssetType = null;

        foreach (var asset in _assets)
        {
            var unlockMonth = (asset.Value.UnlockYear - 1) * 12 + asset.Value.UnlockMonth;

            if (totalMonths >= unlockMonth && !session.UnlockedAssets.Contains(asset.Key))
            {
                session.UnlockedAssets.Add(asset.Key);
                newUnlocks.Add((session.AgeMode, session.Language) switch {
                    (AgeMode.Kids, Language.Indonesian) => asset.Value.DisplayName,
                    (AgeMode.Kids, Language.English) => asset.Value.DisplayNameEN,
                    (AgeMode.Adult, Language.Indonesian) => asset.Value.DisplayNameAdult,
                    _ => asset.Value.DisplayNameAdultEN
                });

                // Show intro for newly unlocked asset (skip in multiplayer — MP unlock popup handles it)
                if (!session.IsMultiplayer)
                {
                    session.ShowIntro = true;
                    session.IntroAssetType = asset.Key;
                }
                lastUnlockedAssetType = asset.Key;
            }
        }

        if (newUnlocks.Any())
        {
            var message = session.Language == Language.Indonesian
                ? $"BARU! Investasi terbuka: {string.Join(", ", newUnlocks)}"
                : $"NEW! Unlocked: {string.Join(", ", newUnlocks)}";
            session.NewUnlockMessage = message;
            session.AddLogEntry(message);
        }

        return lastUnlockedAssetType;
    }

    private Random GetRngForSession(GameSession session)
    {
        if (session.IsMultiplayer && session.RoomCode != null)
        {
            var marketState = _roomMarketStates.GetValueOrDefault(session.RoomCode);
            if (marketState != null)
                return marketState.GetRandomForMonth(session.CurrentYear, session.CurrentMonth);
        }
        return _random;
    }

    private void UpdateMarketPrices(GameSession session)
    {
        var rng = GetRngForSession(session);

        // Skip stock, crypto (handled separately), gold (real data), and reksadana (real index data)
        foreach (var asset in _assets.Where(a => !a.Value.IsFixedIncome
            && a.Value.Category != "stock" && a.Value.Category != "crypto"
            && a.Value.Category != "gold" && a.Value.Category != "index"))
        {
            if (!session.AssetPrices.ContainsKey(asset.Key)) continue;

            var prevPrice = session.AssetPrices[asset.Key];
            session.PreviousPrices[asset.Key] = prevPrice;

            decimal changePercent;
            if (asset.Value.AlwaysPositive)
            {
                changePercent = (decimal)(rng.NextDouble() * (double)(asset.Value.MaxReturn - asset.Value.MinReturn) + (double)asset.Value.MinReturn);
            }
            else
            {
                var range = asset.Value.MaxReturn - asset.Value.MinReturn;
                changePercent = asset.Value.MinReturn + (decimal)(rng.NextDouble() * (double)range);
            }

            var newPrice = prevPrice * (1 + changePercent);
            var minPrice = asset.Value.BasePrice * 0.3m;
            var maxPrice = asset.Value.BasePrice * 5m;
            newPrice = Math.Max(minPrice, Math.Min(maxPrice, newPrice));

            session.AssetPrices[asset.Key] = Math.Round(newPrice, 0);
        }

        // Update gold price from real data
        UpdateGoldPrice(session);

        // Update index prices from real data
        UpdateIndexPrices(session);
    }

    private void UpdateGoldPrice(GameSession session)
    {
        var goldPrice = _goldData.GetPrice(session.CurrentYear, session.CurrentMonth);
        if (goldPrice.HasValue)
        {
            if (session.AssetPrices.ContainsKey("emas"))
                session.PreviousPrices["emas"] = session.AssetPrices["emas"];
            session.AssetPrices["emas"] = goldPrice.Value;
        }
        // Update gold price history for mini chart
        session.GoldPriceHistory = _goldData.GetPriceHistory(session.CurrentYear, session.CurrentMonth);
    }

    private void UpdateIndexPrices(GameSession session)
    {
        foreach (var idx in session.AvailableIndices)
        {
            var newPrice = _indexData.GetPrice(idx.IndexId, session.CurrentYear, session.CurrentMonth);
            if (newPrice.HasValue)
            {
                idx.PreviousPrice = idx.CurrentPrice;
                idx.CurrentPrice = newPrice.Value;
            }
            idx.PriceHistory = _indexData.GetPriceHistory(idx.IndexId, session.CurrentYear, session.CurrentMonth);
        }
    }

    private void UpdateStockPrices(GameSession session)
    {
        foreach (var stock in session.AvailableStocks)
        {
            var newPrice = _stockData.GetPrice(stock.Ticker, session.CurrentYear, session.CurrentMonth);
            if (newPrice.HasValue)
            {
                stock.PreviousPrice = stock.CurrentPrice;
                stock.CurrentPrice = newPrice.Value;
            }

            // Update dividend data at the start of each year (month 1)
            if (session.CurrentMonth == 1 || stock.AnnualDividendPerShare == 0)
            {
                var div = _stockData.GetDividend(stock.Ticker, session.CurrentYear);
                stock.AnnualDividendPerShare = div?.amount ?? 0;
                stock.DividendType = div?.type ?? "None";
            }

            // Populate price history for mini chart (last 7 prices)
            stock.PriceHistory = _stockData.GetPriceHistory(stock.Ticker, session.CurrentYear, session.CurrentMonth);
        }
    }

    private void UpdateCryptoPrices(GameSession session)
    {
        foreach (var crypto in session.AvailableCryptos)
        {
            crypto.PreviousPrice = crypto.CurrentPrice;

            var newPrice = _cryptoData.GetPrice(crypto.Symbol, session.CurrentYear, session.CurrentMonth);
            if (newPrice.HasValue)
            {
                crypto.CurrentPrice = newPrice.Value;
            }

            // Update price history
            crypto.PriceHistory = _cryptoData.GetPriceHistory(crypto.Symbol, session.CurrentYear, session.CurrentMonth);
        }
    }

    private void TriggerMonthlyEvent(GameSession session)
    {
        RandomEvent evt;
        decimal randomizedCost;

        if (session.IsMultiplayer && session.RoomCode != null &&
            _roomMarketStates.TryGetValue(session.RoomCode, out var marketState))
        {
            // Use pre-generated event data for multiplayer
            var eventIdx = marketState.EventIndexPerYear.GetValueOrDefault(session.CurrentYear, _random.Next(_events.Count));
            evt = _events[eventIdx % _events.Count];
            randomizedCost = marketState.EventCostPerYear.GetValueOrDefault(session.CurrentYear, 3_000_000);
        }
        else
        {
            evt = _events[_random.Next(_events.Count)];
            var costPercent = 0.20m + (decimal)(_random.NextDouble() * 0.25);
            randomizedCost = Math.Round(GameSession.YEARLY_INCOME * costPercent / 100_000) * 100_000;
            randomizedCost = Math.Max(2_000_000, Math.Min(4_500_000, randomizedCost));
        }

        session.ActiveEvent = evt;
        session.EventCost = randomizedCost;

        session.IsEventPending = true;
        // In multiplayer: don't pause the game — show a 10s countdown instead
        // In solo: pause so player can choose how to pay
        if (!session.IsMultiplayer)
            session.IsPaused = true;
        else
            session.EventPendingAt = DateTime.UtcNow;

        var evtLogTitle = evt.GetTitle(session.AgeMode, session.Language);
        session.AddLogEntry(session.Language == Language.Indonesian
            ? $"EVENT BULAN {session.CurrentMonth}: {evtLogTitle}"
            : $"MONTH {session.CurrentMonth} EVENT: {evtLogTitle}");

        // Bot experiences the same event and pays automatically
        ProcessBotEventPayment(session, randomizedCost);

        var totalAssets = session.CashBalance + session.TotalSavingsValue + session.TotalPortfolioValue;
        if (totalAssets < randomizedCost)
        {
            session.IsGameOver = true;
            session.GameOverReason = session.Language == Language.Indonesian
                ? $"Tidak mampu membayar {evtLogTitle}. Total aset Rp {totalAssets:N0} tidak cukup untuk Rp {randomizedCost:N0}."
                : $"Unable to pay {evtLogTitle}. Total assets Rp {totalAssets:N0} insufficient for Rp {randomizedCost:N0}.";
            session.AddLogEntry($"GAME OVER: {session.GameOverReason}");
            _behaviorLog.EndSession(session.ConnectionId, ComputeTotalProfitLoss(session));
        }
    }

    // === MULTIPLAYER ROOM CONTROL ===

    public void PauseAllInRoom(string roomCode)
    {
        lock (_lock)
        {
            foreach (var s in _sessions.Values.Where(s => s.RoomCode == roomCode))
                s.IsPaused = true;
        }
    }

    public void ResumeAllInRoom(string roomCode)
    {
        lock (_lock)
        {
            foreach (var s in _sessions.Values.Where(s => s.RoomCode == roomCode))
            {
                // Don't resume sessions that have pending events - they handle their own unpause
                if (!s.IsEventPending)
                    s.IsPaused = false;
            }
        }
    }

    /// <summary>
    /// Auto-pay event for player using priority: Cash → Savings → Index Fund → Gold → Stocks.
    /// Returns true if payment was successful.
    /// </summary>
    private bool AutoPayEventForPlayer(GameSession session)
    {
        if (!session.IsEventPending || session.ActiveEvent == null) return false;

        var cost = session.EventCost ?? 0;
        var remaining = cost;
        var eventTitle = session.ActiveEvent.GetTitle(session.AgeMode, session.Language);

        // 1. Try Cash
        if (session.CashBalance >= remaining)
        {
            session.CashBalance -= remaining;
            session.PlayerTotalEventCostPaid += cost;
            session.AddLogEntry(session.Language == Language.Indonesian
                ? $"[Auto] Bayar {eventTitle} dari kas: Rp {cost:N0}"
                : $"[Auto] Paid {eventTitle} from cash: Rp {cost:N0}");
            ClearEvent(session);
            return true;
        }
        else if (session.CashBalance > 0)
        {
            remaining -= session.CashBalance;
            session.CashBalance = 0;
        }

        // 2. Try Savings
        if (session.SavingsAccount != null && session.SavingsAccount.Balance >= remaining)
        {
            session.SavingsAccount.Balance -= remaining;
            if (session.SavingsAccount.Balance <= 0) session.SavingsAccount = null;
            session.PlayerTotalEventCostPaid += cost;
            session.AddLogEntry(session.Language == Language.Indonesian
                ? $"[Auto] Bayar {eventTitle} dari tabungan: Rp {cost:N0}"
                : $"[Auto] Paid {eventTitle} from savings: Rp {cost:N0}");
            ClearEvent(session);
            return true;
        }
        else if (session.SavingsAccount != null && session.SavingsAccount.Balance > 0)
        {
            remaining -= session.SavingsAccount.Balance;
            session.SavingsAccount = null;
        }

        // 3. Try Index Funds (portfolio keys: index_{indexId})
        var indexPortfolios = session.Portfolio.Where(p => p.Key.StartsWith("index_")).ToList();
        foreach (var (key, indexFund) in indexPortfolios)
        {
            if (indexFund.TotalValue >= remaining)
            {
                var idx = session.AvailableIndices.FirstOrDefault(i => i.IndexId == indexFund.Ticker);
                var price = idx?.CurrentPrice ?? 1_000_000m;
                if (price <= 0) continue;
                var unitsNeeded = remaining / price;
                if (unitsNeeded > indexFund.Units) unitsNeeded = indexFund.Units;
                var saleValue = unitsNeeded * price;
                var costBasis = (indexFund.TotalCost / indexFund.Units) * unitsNeeded;
                indexFund.Units -= unitsNeeded;
                indexFund.TotalCost -= costBasis;
                if (indexFund.Units <= 0) session.Portfolio.Remove(key);
                if (saleValue > remaining) session.CashBalance += saleValue - remaining;
                session.TotalRealizedPortfolioGainLoss += saleValue - costBasis;
                session.PlayerTotalEventCostPaid += cost;
                session.AddLogEntry(session.Language == Language.Indonesian
                    ? $"[Auto] Bayar {eventTitle} dari reksa dana: Rp {cost:N0}"
                    : $"[Auto] Paid {eventTitle} from index fund: Rp {cost:N0}");
                ClearEvent(session);
                return true;
            }
        }

        // 4. Try Gold
        if (session.Portfolio.TryGetValue("emas", out var gold) && gold.TotalValue >= remaining)
        {
            var price = session.AssetPrices.GetValueOrDefault("emas", 1_200_000m);
            var unitsNeeded = Math.Ceiling(remaining / price);
            if (unitsNeeded > gold.Units) unitsNeeded = gold.Units;
            var saleValue = (decimal)unitsNeeded * price;
            var costBasis = (gold.TotalCost / (gold.Units + (decimal)unitsNeeded)) * (decimal)unitsNeeded;
            gold.Units -= (decimal)unitsNeeded;
            gold.TotalCost -= costBasis;
            if (gold.Units <= 0) session.Portfolio.Remove("emas");
            if (saleValue > remaining) session.CashBalance += saleValue - remaining;
            session.TotalRealizedPortfolioGainLoss += saleValue - costBasis;
            session.PlayerTotalEventCostPaid += cost;
            session.AddLogEntry(session.Language == Language.Indonesian
                ? $"[Auto] Bayar {eventTitle} dari emas: Rp {cost:N0}"
                : $"[Auto] Paid {eventTitle} from gold: Rp {cost:N0}");
            ClearEvent(session);
            return true;
        }

        // 5. Try Stocks (highest value first)
        var stockItems = session.Portfolio.Where(p => p.Key == "saham" || p.Value.AssetType == "saham")
            .OrderByDescending(p => p.Value.TotalValue).ToList();
        foreach (var kvp in stockItems)
        {
            var stock = kvp.Value;
            if (stock.TotalValue >= remaining)
            {
                var price = stock.PricePerUnit;
                var unitsNeeded = (int)Math.Ceiling(remaining / price);
                if (unitsNeeded > (int)stock.Units) unitsNeeded = (int)stock.Units;
                var saleValue = unitsNeeded * price;
                var costBasis = (stock.TotalCost / (stock.Units + unitsNeeded)) * unitsNeeded;
                stock.Units -= unitsNeeded;
                stock.TotalCost -= costBasis;
                if (stock.Units <= 0) session.Portfolio.Remove(kvp.Key);
                if (saleValue > remaining) session.CashBalance += saleValue - remaining;
                session.TotalRealizedPortfolioGainLoss += saleValue - costBasis;
                session.PlayerTotalEventCostPaid += cost;
                session.AddLogEntry(session.Language == Language.Indonesian
                    ? $"[Auto] Bayar {eventTitle} dari saham: Rp {cost:N0}"
                    : $"[Auto] Paid {eventTitle} from stocks: Rp {cost:N0}");
                ClearEvent(session);
                return true;
            }
        }

        // Cannot pay — game over
        var totalAssets = session.CashBalance + session.TotalSavingsValue + session.TotalPortfolioValue;
        session.IsGameOver = true;
        session.GameOverReason = session.Language == Language.Indonesian
            ? $"Tidak mampu membayar {eventTitle}. Total aset Rp {totalAssets:N0} tidak cukup untuk Rp {cost:N0}."
            : $"Unable to pay {eventTitle}. Total assets Rp {totalAssets:N0} insufficient for Rp {cost:N0}.";
        session.AddLogEntry($"GAME OVER: {session.GameOverReason}");
        _behaviorLog.EndSession(session.ConnectionId, ComputeTotalProfitLoss(session));
        ClearEvent(session);
        return false;
    }

    /// <summary>
    /// Check if player's event has timed out (10s) and auto-pay if so.
    /// Returns true if auto-pay was triggered.
    /// </summary>
    public bool CheckAndAutoPayEvent(string connectionId)
    {
        lock (_lock)
        {
            var session = GetSession(connectionId);
            if (session == null || !session.IsEventPending || session.EventPendingAt == null)
                return false;

            if ((DateTime.UtcNow - session.EventPendingAt.Value).TotalSeconds < 10)
                return false;

            return AutoPayEventForPlayer(session);
        }
    }

    /// <summary>
    /// Get portfolio summaries for all players in a room (for host dashboard).
    /// </summary>
    public List<PlayerSummary> GetAllPlayerPortfolios(string roomCode)
    {
        lock (_lock)
        {
            var sessions = _sessions.Values.Where(s => s.RoomCode == roomCode).ToList();
            return sessions.Select(s =>
            {
                var nw = s.NetWorth;
                var breakdown = new Dictionary<string, decimal>();

                // Break down Portfolio into individual asset types
                decimal stocksValue = 0, indexValue = 0, goldValue = 0, cryptoValue = 0;
                foreach (var p in s.Portfolio.Values)
                {
                    switch (p.AssetType)
                    {
                        case "saham": stocksValue += p.TotalValue; break;
                        case "reksadana": indexValue += p.TotalValue; break;
                        case "emas": goldValue += p.TotalValue; break;
                        case "crypto": cryptoValue += p.TotalValue; break;
                    }
                }

                if (nw > 0)
                {
                    if (s.CashBalance > 0) breakdown["Cash"] = Math.Round((s.CashBalance / nw) * 100, 1);
                    if (s.TotalSavingsValue > 0) breakdown["Savings"] = Math.Round((s.TotalSavingsValue / nw) * 100, 1);
                    if (s.TotalDepositoValue > 0) breakdown["Deposito"] = Math.Round((s.TotalDepositoValue / nw) * 100, 1);
                    if (s.TotalBondValue > 0) breakdown["Bond"] = Math.Round((s.TotalBondValue / nw) * 100, 1);
                    if (stocksValue > 0) breakdown["Stocks"] = Math.Round((stocksValue / nw) * 100, 1);
                    if (indexValue > 0) breakdown["Index"] = Math.Round((indexValue / nw) * 100, 1);
                    if (goldValue > 0) breakdown["Gold"] = Math.Round((goldValue / nw) * 100, 1);
                    if (cryptoValue > 0) breakdown["Crypto"] = Math.Round((cryptoValue / nw) * 100, 1);
                    if (s.TotalCrowdfundingValue > 0) breakdown["Crowdfunding"] = Math.Round((s.TotalCrowdfundingValue / nw) * 100, 1);
                }
                var initialCapital = 5_000_000m + (s.CurrentYear - 1) * GameSession.YEARLY_INCOME;
                return new PlayerSummary
                {
                    ConnectionId = s.ConnectionId,
                    PlayerName = s.PlayerId,
                    NetWorth = nw,
                    IsConnected = true,
                    CurrentYear = s.CurrentYear,
                    CurrentMonth = s.CurrentMonth,
                    PortfolioBreakdown = breakdown,
                    TotalGainLoss = nw - initialCapital,
                    SavingsInterestEarned = s.TotalSavingsInterestEarned,
                    DepositoInterestEarned = s.TotalDepositoInterestEarned + s.Depositos.Sum(d => d.CurrentValue - d.Principal),
                    BondCouponEarned = s.TotalBondCouponEarned,
                    DividendEarned = s.TotalDividendEarned,
                    PortfolioGainLoss = s.TotalRealizedPortfolioGainLoss + s.Portfolio.Values.Sum(p => p.ProfitLoss),
                    CrowdfundingGainLoss = s.TotalRealizedCrowdfundingGainLoss + s.CrowdfundingInvestments.Where(c => !c.HasFailed).Sum(c => c.CurrentValue - c.InvestedAmount),
                    TotalEventCostPaid = s.PlayerTotalEventCostPaid
                };
            }).ToList();
        }
    }

    public void PauseGame(string connectionId)
    {
        lock (_lock)
        {
            var session = GetSession(connectionId);
            if (session != null && !session.IsEventPending)
            {
                session.IsPaused = true;
            }
        }
    }

    public void ResumeGame(string connectionId)
    {
        lock (_lock)
        {
            var session = GetSession(connectionId);
            if (session != null && !session.IsEventPending)
            {
                session.IsPaused = false;
            }
        }
    }

    public void RestartGame(string connectionId)
    {
        lock (_lock)
        {
            var session = GetSession(connectionId);
            if (session != null)
            {
                var ageMode = session.AgeMode;
                var playerId = session.PlayerId;

                session.CurrentYear = 1;
                session.CurrentMonth = 1;
                session.MonthProgress = 0;
                session.CashBalance = 5_000_000; // Reduced starting amount for tighter early game
                session.SavingsAccount = null;
                session.Portfolio.Clear();
                session.Depositos.Clear();
                session.Bonds.Clear();
                session.IsGameOver = false;
                session.GameOverReason = null;
                session.IsPaused = false;
                session.ActiveEvent = null;
                session.EventCost = null;
                session.IsEventPending = false;
                session.NewUnlockMessage = null;
                session.ShowIntro = true;
                session.IntroAssetType = "tabungan";
                session.GameLog.Clear();

                session.UnlockedAssets.Clear();
                session.UnlockedAssets.Add("tabungan");

                // Reset event tracking for new game
                session.EventMonthForYear = _random.Next(7, 11); // Random month 7-10 for year 1
                session.EventOccurredThisYear = false;

                // Reinitialize stocks: randomly select 3 shariah + 1 non-shariah
                session.InitializeStocks(SelectRandomStocks(_random));

                // Reinitialize indices
                session.AvailableIndices = SelectRandomIndices(_random);

                // Reinitialize cryptos
                var cryptos = _allCryptos.Select(c => new CryptoInfo
                {
                    Symbol = c.Symbol,
                    Name = c.Name,
                    CurrentPrice = c.CurrentPrice,
                    PreviousPrice = c.PreviousPrice
                }).ToList();
                session.InitializeCryptos(cryptos);

                // Reinitialize crowdfunding projects (select 3 random)
                var crowdfunding = _allCrowdfundingProjects.OrderBy(_ => _random.Next()).Take(3).Select(p => new CrowdfundingProject
                {
                    ProjectId = p.ProjectId,
                    ProjectName = p.ProjectName,
                    ProjectType = p.ProjectType,
                    MinimumInvestment = p.MinimumInvestment,
                    ExpectedReturn = p.ExpectedReturn,
                    RiskLevel = p.RiskLevel,
                    LockUpMonths = p.LockUpMonths,
                    IsActive = true
                }).ToList();
                session.InitializeCrowdfunding(crowdfunding);

                session.InitializePrices(_assets);

                // Reset bot state
                session.BotCashBalance = 5_000_000;
                session.BotSavingsBalance = 0;
                session.BotDepositos.Clear();
                session.BotBonds.Clear();
                session.BotIndexFundUnits = 0;
                session.BotIndexFundCost = 0;
                session.BotGoldUnits = 0;
                session.BotGoldCost = 0;
                session.BotStockCost = 0;
                session.BotStockValue = 0;
                session.BotStockTicker = string.Empty;
                session.BotEventsPaidFromCash = 0;
                session.BotEventsPaidFromSavings = 0;
                session.BotEventsPaidFromPortfolio = 0;
                session.BotTotalEventCostPaid = 0;

                // Initialize bot with balanced strategy - 5% to savings as emergency fund
                var initialSavings = session.BotCashBalance * 0.05m;
                session.BotSavingsBalance = initialSavings;
                session.BotCashBalance -= initialSavings;

                session.AddLogEntry(ageMode == AgeMode.Kids
                    ? "Game dimulai ulang! Ayo belajar investasi lagi!"
                    : "Game restarted! Let's learn to invest wisely.");
            }
        }
    }
}
