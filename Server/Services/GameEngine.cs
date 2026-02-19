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
    private readonly List<DepositoRate> _depositoRates;
    private readonly List<BondRate> _bondRates;
    private readonly List<StockInfo> _allStocks;
    private readonly List<CryptoInfo> _allCryptos;
    private readonly List<CrowdfundingProject> _allCrowdfundingProjects;
    private readonly Random _random = new();
    private readonly Dictionary<string, GameSession> _sessions = new();
    private readonly Dictionary<string, RoomMarketState> _roomMarketStates = new(); // roomCode → market state
    private readonly object _lock = new();
    private readonly ILogger<GameEngine> _logger;

    public GameEngine(ILogger<GameEngine> logger)
    {
        _logger = logger;
        _assets = InitializeAssets();
        _events = InitializeEvents();
        _depositoRates = InitializeDepositoRates();
        _bondRates = InitializeBondRates();
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
                WhatIsIt = "Deposito itu kayak menabung tapi dikunci! Kamu taruh uang untuk waktu tertentu (1-24 bulan), dan dapat bunga lebih besar dari tabungan biasa! Makin lama dikunci, makin besar hadiahnya!",
                WhatIsItEN = "A fixed deposit is like saving in a locked box! You put money in for a specific time (1-24 months), and you get bigger bonuses than regular savings! The longer you lock it, the bigger the reward!",
                RiskExplanation = "AMAN! Tapi uangmu dikunci. Kalau diambil sebelum waktunya, bonus bunganya berkurang!",
                RiskExplanationEN = "Safe! But your money is locked. If you take it early, you lose some of your bonus!",
                BestFor = "Uang yang nggak akan dipakai dalam beberapa bulan!",
                BestForEN = "Money you won't need for several months!",
                ExpectedReturn = "Bunga 2.5% - 6% per tahun, lebih besar dari tabungan biasa!",
                ExpectedReturnEN = "Interest 2.5% - 6% per year, more than regular savings!",
                // Normal Mode (Adult) - Full details with regulations
                DescriptionAdult = "Deposito berjangka dengan bunga tetap yang dijamin",
                DescriptionAdultEN = "Fixed-term deposit offering guaranteed interest returns",
                WhatIsItAdult = "Deposito Berjangka adalah simpanan berjangka dengan bunga tetap untuk tenor tertentu (1-24 bulan). Dijamin LPS hingga Rp 2 miliar. Bunga lebih tinggi dari tabungan, namun dana terkunci hingga jatuh tempo.",
                WhatIsItAdultEN = "A Certificate of Deposit (CD) is a time deposit offering fixed interest for specified terms (1-24 months). Guaranteed by LPS up to Rp 2 billion. Higher rates than savings but funds are locked until maturity.",
                RiskExplanationAdult = "Risiko sangat rendah. Pokok dan bunga dijamin LPS. Pencairan dini dikenakan penalti (kehilangan bunga + 1% dari pokok). Bunga dipotong pajak 20%.",
                RiskExplanationAdultEN = "Very low risk. Principal and interest guaranteed by LPS. Early withdrawal incurs penalty (forfeited interest + 1% of principal). Interest taxed at 20%.",
                BestForAdult = "Tabungan jangka menengah dengan return yang pasti. Ideal untuk dana yang tidak akan digunakan dalam periode tertentu.",
                BestForAdultEN = "Medium-term savings with predictable returns. Ideal for planned expenses.",
                ExpectedReturnAdult = "Bunga: 2.5% - 6% per tahun tergantung tenor (setelah pajak 20%)",
                ExpectedReturnAdultEN = "Interest: 2.5% - 6% p.a. depending on term (after 20% tax)",
                RealRules = "Diawasi OJK. Dijamin LPS. Tenor: 1, 3, 6, 12, 24 bulan. Minimum Rp 1 juta. Pajak bunga 20%. Opsi ARO (perpanjangan otomatis) tersedia.",
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
                WhatIsIt = "Bayangin kamu minjemin uang ke Pak Presiden buat bangun jalan & sekolah! Tiap bulan, negara kasih kamu uang terima kasih (namanya kupon)! Seperti jadi pahlawan yang membantu membangun Indonesia!",
                WhatIsItEN = "Imagine lending money to the President to build roads and schools! Every month, the government gives you thank-you money (called coupons)! You become a hero helping to build Indonesia!",
                RiskExplanation = "AMAN BANGET! Pemerintah Indonesia yang bayar, dan pemerintah SELALU membayar kembali!",
                RiskExplanationEN = "Super safe! The Indonesian government pays it, and the government ALWAYS pays back!",
                BestFor = "Kamu yang mau dapat uang jajan bulanan dari investasi!",
                BestForEN = "You who want to get monthly pocket money from investing!",
                ExpectedReturn = "Dapat bonus 5-7% per tahun, dibayar tiap bulan dari pemerintah!",
                ExpectedReturnEN = "Get 5-7% bonus per year, paid monthly from the government!",
                // Normal Mode (Adult) - Full details with regulations
                DescriptionAdult = "Surat utang negara dengan kupon tetap yang dijamin 100% oleh pemerintah",
                DescriptionAdultEN = "Government debt securities with fixed coupons, 100% government guaranteed",
                WhatIsItAdult = "Surat Berharga Negara (SBN) Ritel adalah surat utang yang diterbitkan Kementerian Keuangan RI. Jenis: ORI (tradeable), SBR (non-tradeable, early redemption 50%), SR (Sukuk, sharia), ST (Sukuk Tabungan). Dijamin 100% APBN.",
                WhatIsItAdultEN = "Retail Government Bonds (SBN) are debt securities issued by the Ministry of Finance. Types: ORI (tradeable), SBR (non-tradeable, 50% early redemption), SR (Sukuk, sharia), ST (Savings Sukuk). 100% backed by state budget (APBN).",
                RiskExplanationAdult = "Risiko sangat rendah. Dijamin 100% oleh Pemerintah RI (APBN). Kupon tetap (ORI/SR) atau mengambang dengan floor (SBR/ST). Kupon dipotong pajak 10%.",
                RiskExplanationAdultEN = "Very low risk. 100% guaranteed by Indonesian Government (APBN). Fixed coupon (ORI/SR) or floating with floor (SBR/ST). Coupon taxed at 10%.",
                BestForAdult = "Investor konservatif yang mencari pendapatan rutin bulanan dengan jaminan pemerintah. Harus WNI dengan e-KTP dan SID.",
                BestForAdultEN = "Conservative investors seeking regular monthly income with government guarantee. Must be Indonesian citizen with e-KTP and SID.",
                ExpectedReturnAdult = "Kupon: 5.5% - 7% per tahun, dibayar bulanan (setelah pajak 10%). Tenor: 2-6 tahun.",
                ExpectedReturnAdultEN = "Coupon: 5.5% - 7% p.a., paid monthly (after 10% tax). Tenor: 2-6 years.",
                RealRules = "Diawasi OJK/DJPPR. Dijamin 100% Pemerintah RI. Minimum Rp 1 juta, maksimum Rp 5 miliar. Kupon bulanan. Pajak kupon 10%, capital gain 10%.",
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
                MinReturn = -0.02m,
                MaxReturn = 0.03m,
                AlwaysPositive = false,
                RiskLevel = "Sedang",
                UnlockYear = 5,
                UnlockMonth = 1,
                MinimumInvestment = 50_000,
                IsFixedIncome = false
            },

            // === CRYPTO - Unlocked at year 6 ===
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
                UnlockYear = 6,
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

    private List<DepositoRate> InitializeDepositoRates()
    {
        // Balanced rates: longer tenors offer better compound returns than rolling over shorter tenors
        // 1 month: 3.0% annual baseline
        // 3 months: 4.2% annual (better than 3x 1-month rollover)
        // 6 months: 5.0% annual (better than 6x 1-month or 2x 3-month)
        // 12 months: 6.0% annual (significantly better for 1-year commitment)
        // 24 months: 7.0% annual (premium for 2-year lock-in)
        return new List<DepositoRate>
        {
            new DepositoRate { PeriodMonths = 1, PeriodName = "1 Bulan", AnnualRate = 0.030m, PenaltyRate = 0.50m, MinimumDeposit = 1_000_000 },
            new DepositoRate { PeriodMonths = 3, PeriodName = "3 Bulan", AnnualRate = 0.042m, PenaltyRate = 0.50m, MinimumDeposit = 1_000_000 },
            new DepositoRate { PeriodMonths = 6, PeriodName = "6 Bulan", AnnualRate = 0.050m, PenaltyRate = 0.50m, MinimumDeposit = 1_000_000 },
            new DepositoRate { PeriodMonths = 12, PeriodName = "12 Bulan", AnnualRate = 0.060m, PenaltyRate = 0.50m, MinimumDeposit = 1_000_000 },
            new DepositoRate { PeriodMonths = 24, PeriodName = "24 Bulan", AnnualRate = 0.070m, PenaltyRate = 0.50m, MinimumDeposit = 1_000_000 },
        };
    }

    private List<BondRate> InitializeBondRates()
    {
        // Based on mechanics: ORI/SR tradeable fixed coupon, SBR/ST non-tradeable floating with floor, 50% early redemption
        return new List<BondRate>
        {
            new BondRate { PeriodMonths = 36, PeriodName = "ORI (3 Tahun)", BondType = "ORI", CouponRate = 0.060m, MinimumInvestment = 1_000_000 },
            new BondRate { PeriodMonths = 36, PeriodName = "SR (3 Tahun Syariah)", BondType = "SR", CouponRate = 0.062m, MinimumInvestment = 1_000_000 },
            new BondRate { PeriodMonths = 24, PeriodName = "SBR (2 Tahun)", BondType = "SBR", CouponRate = 0.065m, MinimumInvestment = 1_000_000 },
            new BondRate { PeriodMonths = 24, PeriodName = "ST (2 Tahun Syariah)", BondType = "ST", CouponRate = 0.067m, MinimumInvestment = 1_000_000 },
        };
    }

    private List<StockInfo> InitializeStocks()
    {
        // Pool of Indonesian stocks with dividend yields (annual percentage)
        // Dividend is paid once per year (at year end) if player holds the stock
        // Prices designed for game balance: cheap stocks accessible early, expensive stocks for late game
        // 1 lot = 100 shares, so per-lot cost = price × 100
        return new List<StockInfo>
        {
            // === CHEAP STOCKS (under 2000/share = under 200K/lot) - accessible early game ===
            new StockInfo { Ticker = "HMSP", CompanyName = "HM Sampoerna", Sector = "Consumer", CurrentPrice = 850, PreviousPrice = 850, DividendYield = 0.08m }, // 85K/lot, 8% div
            new StockInfo { Ticker = "PGAS", CompanyName = "Perusahaan Gas Negara", Sector = "Energy", CurrentPrice = 1_200, PreviousPrice = 1_200, DividendYield = 0.09m }, // 120K/lot, 9% div
            new StockInfo { Ticker = "KLBF", CompanyName = "Kalbe Farma", Sector = "Pharma", CurrentPrice = 1_450, PreviousPrice = 1_450, DividendYield = 0.025m }, // 145K/lot, 2.5% div
            new StockInfo { Ticker = "ANTM", CompanyName = "Aneka Tambang", Sector = "Mining", CurrentPrice = 1_650, PreviousPrice = 1_650, DividendYield = 0.05m }, // 165K/lot, 5% div
            new StockInfo { Ticker = "PTBA", CompanyName = "Bukit Asam", Sector = "Mining", CurrentPrice = 1_950, PreviousPrice = 1_950, DividendYield = 0.12m }, // 195K/lot, 12% div (high)

            // === MID-RANGE STOCKS (2000-5000/share = 200K-500K/lot) - mid game ===
            new StockInfo { Ticker = "ADRO", CompanyName = "Adaro Energy", Sector = "Mining", CurrentPrice = 2_400, PreviousPrice = 2_400, DividendYield = 0.10m }, // 240K/lot, 10% div
            new StockInfo { Ticker = "TLKM", CompanyName = "Telkom Indonesia", Sector = "Telecom", CurrentPrice = 3_200, PreviousPrice = 3_200, DividendYield = 0.05m }, // 320K/lot, 5% div
            new StockInfo { Ticker = "UNVR", CompanyName = "Unilever Indonesia", Sector = "Consumer", CurrentPrice = 3_800, PreviousPrice = 3_800, DividendYield = 0.04m }, // 380K/lot, 4% div
            new StockInfo { Ticker = "ASII", CompanyName = "Astra International", Sector = "Automotive", CurrentPrice = 4_500, PreviousPrice = 4_500, DividendYield = 0.06m }, // 450K/lot, 6% div
            new StockInfo { Ticker = "BBRI", CompanyName = "Bank Rakyat Indonesia", Sector = "Banking", CurrentPrice = 4_800, PreviousPrice = 4_800, DividendYield = 0.045m }, // 480K/lot, 4.5% div

            // === EXPENSIVE STOCKS (5000+/share = 500K+/lot) - late game, blue chips ===
            new StockInfo { Ticker = "BMRI", CompanyName = "Bank Mandiri", Sector = "Banking", CurrentPrice = 5_500, PreviousPrice = 5_500, DividendYield = 0.05m }, // 550K/lot, 5% div
            new StockInfo { Ticker = "INDF", CompanyName = "Indofood Sukses", Sector = "Consumer", CurrentPrice = 6_200, PreviousPrice = 6_200, DividendYield = 0.04m }, // 620K/lot, 4% div
            new StockInfo { Ticker = "SMGR", CompanyName = "Semen Indonesia", Sector = "Materials", CurrentPrice = 7_500, PreviousPrice = 7_500, DividendYield = 0.035m }, // 750K/lot, 3.5% div
            new StockInfo { Ticker = "BBCA", CompanyName = "Bank Central Asia", Sector = "Banking", CurrentPrice = 9_200, PreviousPrice = 9_200, DividendYield = 0.03m }, // 920K/lot, 3% div
            new StockInfo { Ticker = "ICBP", CompanyName = "Indofood CBP", Sector = "Consumer", CurrentPrice = 10_500, PreviousPrice = 10_500, DividendYield = 0.03m }, // 1.05M/lot, 3% div
        };
    }

    private List<CryptoInfo> InitializeCryptos()
    {
        // Crypto starting prices for game balance
        // BTC: Rp 1,106,989,703
        // ETH: Rp 32,594,446
        // DOGE: Rp 10,536
        return new List<CryptoInfo>
        {
            new CryptoInfo { Symbol = "BTC", Name = "Bitcoin", CurrentPrice = 1_106_989_703, PreviousPrice = 1_106_989_703 },
            new CryptoInfo { Symbol = "ETH", Name = "Ethereum", CurrentPrice = 32_594_446, PreviousPrice = 32_594_446 },
            new CryptoInfo { Symbol = "DOGE", Name = "Dogecoin", CurrentPrice = 10_536, PreviousPrice = 10_536 },
        };
    }

    private List<CrowdfundingProject> InitializeCrowdfundingProjects()
    {
        // Diverse crowdfunding projects representing different business sectors
        // Each has a lock-up period (12-24 months) where funds cannot be withdrawn
        return new List<CrowdfundingProject>
        {
            new CrowdfundingProject
            {
                ProjectId = "CF001",
                ProjectName = "Kopi Nusantara Export",
                ProjectType = "Commodities",
                Description = "Ekspor kopi specialty dari petani lokal ke pasar internasional",
                FundingGoal = 500_000_000,
                CurrentFunding = 0,
                MinimumInvestment = 100_000,
                DaysRemaining = 90,
                ExpectedReturn = 0.15m, // 15% annual if successful
                RiskLevel = 3,
                LockUpMonths = 12 // 1 year lock-up
            },
            new CrowdfundingProject
            {
                ProjectId = "CF002",
                ProjectName = "Urban Vertical Farm",
                ProjectType = "Agriculture",
                Description = "Pertanian vertikal sayuran organik di perkotaan menggunakan teknologi hidroponik",
                FundingGoal = 800_000_000,
                CurrentFunding = 0,
                MinimumInvestment = 100_000,
                DaysRemaining = 90,
                ExpectedReturn = 0.25m, // 25% annual if successful (18 months)
                RiskLevel = 4,
                LockUpMonths = 18 // 1.5 year lock-up
            },
            new CrowdfundingProject
            {
                ProjectId = "CF003",
                ProjectName = "EdTech Learning Platform",
                ProjectType = "Tech Startup",
                Description = "Platform pembelajaran online untuk siswa SMA dengan AI tutor",
                FundingGoal = 1_200_000_000,
                CurrentFunding = 0,
                MinimumInvestment = 100_000,
                DaysRemaining = 90,
                ExpectedReturn = 0.35m, // 35% potential but higher risk (24 months)
                RiskLevel = 5,
                LockUpMonths = 24 // 2 year lock-up (tech startups take longer)
            },
            new CrowdfundingProject
            {
                ProjectId = "CF004",
                ProjectName = "Warung Makan Franchise",
                ProjectType = "F&B",
                Description = "Franchise waralaba makanan cepat saji dengan menu lokal",
                FundingGoal = 600_000_000,
                CurrentFunding = 0,
                MinimumInvestment = 100_000,
                DaysRemaining = 90,
                ExpectedReturn = 0.15m, // 15% stable return (12 months)
                RiskLevel = 2,
                LockUpMonths = 12 // 1 year lock-up
            },
            new CrowdfundingProject
            {
                ProjectId = "CF005",
                ProjectName = "Fashion Retail Store",
                ProjectType = "Retail",
                Description = "Toko fashion lokal dengan desain unik untuk pasar millennial",
                FundingGoal = 400_000_000,
                CurrentFunding = 0,
                MinimumInvestment = 100_000,
                DaysRemaining = 90,
                ExpectedReturn = 0.15m, // 15% modest return (12 months)
                RiskLevel = 3,
                LockUpMonths = 12 // 1 year lock-up
            }
        };
    }

    private List<RandomEvent> InitializeEvents()
    {
        return new List<RandomEvent>
        {
            new RandomEvent
            {
                Title = "Ada yang Sakit!",
                TitleAdult = "Medical Emergency",
                Description = "Wah, ada keluarga yang sakit dan harus ke rumah sakit! Harus bayar biaya dokter dan obat!",
                DescriptionAdult = "A family member requires hospitalization. Medical expenses not fully covered by insurance.",
                Cost = 5_000_000,
                Impact = "Kesehatan"
            },
            new RandomEvent
            {
                Title = "Motor/Mobil Rewel!",
                TitleAdult = "Vehicle Repair",
                Description = "Waduh, motor atau mobil tiba-tiba mogok! Harus ke bengkel dan ganti suku cadang!",
                DescriptionAdult = "Major vehicle breakdown requiring significant repair costs.",
                Cost = 3_000_000,
                Impact = "Transportasi"
            },
            new RandomEvent
            {
                Title = "Atap Bocor!",
                TitleAdult = "Home Repair",
                Description = "Hujan deres dan atap rumah bocor! Harus cepat diperbaiki!",
                DescriptionAdult = "Roof damage requiring immediate repair during rainy season.",
                Cost = 7_000_000,
                Impact = "Rumah"
            },
            new RandomEvent
            {
                Title = "Waktunya Sekolah!",
                TitleAdult = "Education Expenses",
                Description = "Naik kelas! Tapi harus bayar uang pangkal dan beli seragam baru!",
                DescriptionAdult = "School fees and new uniform costs for new academic year.",
                Cost = 4_000_000,
                Impact = "Pendidikan"
            },
            new RandomEvent
            {
                Title = "Bayar Pajak!",
                TitleAdult = "Tax Payment",
                Description = "Waktunya bayar pajak tahunan! Sebagai warga negara yang baik!",
                DescriptionAdult = "Annual tax underpayment discovered during filing.",
                Cost = 2_500_000,
                Impact = "Pajak"
            },
            new RandomEvent
            {
                Title = "Banyak Kondangan!",
                TitleAdult = "Social Obligations",
                Description = "Wow ada 3 teman nikahan bulan ini! Harus beli kado dan kasih amplop!",
                DescriptionAdult = "Multiple wedding invitations requiring gifts and cash contributions.",
                Cost = 2_000_000,
                Impact = "Sosial"
            },
            new RandomEvent
            {
                Title = "HP Rusak!",
                TitleAdult = "Device Replacement",
                Description = "Yah HP jatuh dan rusak parah! Harus beli baru!",
                DescriptionAdult = "Essential smartphone damaged beyond repair, needs replacement.",
                Cost = 3_500_000,
                Impact = "Elektronik"
            },
            new RandomEvent
            {
                Title = "Iuran Komplek!",
                TitleAdult = "Community Fees",
                Description = "Waktunya bayar iuran RT, keamanan, dan kebersihan!",
                DescriptionAdult = "Annual neighborhood security and maintenance fees due.",
                Cost = 1_500_000,
                Impact = "Lingkungan"
            }
        };
    }

    public Dictionary<string, AssetDefinition> GetAssets() => _assets;
    public List<DepositoRate> GetDepositoRates() => _depositoRates;
    public List<BondRate> GetBondRates() => _bondRates;

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

            session.EventMonthForYear = _random.Next(7, 11);
            session.EventOccurredThisYear = false;

            // Select 5 random stocks for this session (include dividend yield)
            var selectedStocks = _allStocks.OrderBy(_ => _random.Next()).Take(5).Select(s => new StockInfo
            {
                Ticker = s.Ticker,
                CompanyName = s.CompanyName,
                Sector = s.Sector,
                CurrentPrice = s.CurrentPrice,
                PreviousPrice = s.PreviousPrice,
                LastPriceUpdateMonth = 0,
                DividendYield = s.DividendYield
            }).ToList();
            session.InitializeStocks(selectedStocks);

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
                Description = p.Description,
                FundingGoal = p.FundingGoal,
                CurrentFunding = 0,
                MinimumInvestment = p.MinimumInvestment,
                DaysRemaining = 90 + _random.Next(-30, 30), // 60-120 days
                ExpectedReturn = p.ExpectedReturn,
                RiskLevel = p.RiskLevel,
                IsActive = true
            }).ToList();
            session.InitializeCrowdfunding(crowdfunding);

            // Savings account is unlocked from the start
            session.UnlockedAssets.Add("tabungan");
            session.AddLogEntry(ageMode == AgeMode.Kids
                ? "Selamat datang di Tjoean! Mari belajar investasi!"
                : "Welcome to Tjoean Investment Simulator. Let's learn to invest wisely.");

            // Initialize bot with emerging market balanced strategy
            // Target allocation: 5% Savings, 25% Deposito, 20% Bonds, 30% Index Fund, 20% Gold
            // Bot starts with 5% in savings as emergency fund (always available)
            var initialSavings = session.BotCashBalance * 0.05m; // 250K
            session.BotSavingsBalance = initialSavings;
            session.BotCashBalance -= initialSavings;

            _sessions[connectionId] = session;
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

            // Select 5 random stocks (shared for all room players)
            state.AvailableStocks = _allStocks.OrderBy(_ => rng.Next()).Take(5).Select(s => new StockInfo
            {
                Ticker = s.Ticker,
                CompanyName = s.CompanyName,
                Sector = s.Sector,
                CurrentPrice = s.CurrentPrice,
                PreviousPrice = s.PreviousPrice,
                LastPriceUpdateMonth = 0,
                DividendYield = s.DividendYield
            }).ToList();

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
                Description = p.Description,
                FundingGoal = p.FundingGoal,
                CurrentFunding = 0,
                MinimumInvestment = p.MinimumInvestment,
                DaysRemaining = 90 + rng.Next(-30, 30),
                ExpectedReturn = p.ExpectedReturn,
                RiskLevel = p.RiskLevel,
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
                ShowIntro = true,
                IntroAssetType = "tabungan"
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
                LastPriceUpdateMonth = 0, DividendYield = s.DividendYield
            }).ToList());

            // Clone shared cryptos
            session.InitializeCryptos(marketState.AvailableCryptos.Select(c => new CryptoInfo
            {
                Symbol = c.Symbol, Name = c.Name,
                CurrentPrice = c.CurrentPrice, PreviousPrice = c.PreviousPrice
            }).ToList());

            // Clone shared crowdfunding
            session.InitializeCrowdfunding(marketState.AvailableCrowdfunding.Select(p => new CrowdfundingProject
            {
                ProjectId = p.ProjectId, ProjectName = p.ProjectName, ProjectType = p.ProjectType,
                Description = p.Description, FundingGoal = p.FundingGoal, CurrentFunding = 0,
                MinimumInvestment = p.MinimumInvestment, DaysRemaining = p.DaysRemaining,
                ExpectedReturn = p.ExpectedReturn, RiskLevel = p.RiskLevel, IsActive = true
            }).ToList());

            session.UnlockedAssets.Add("tabungan");
            session.AddLogEntry(ageMode == AgeMode.Kids
                ? "Selamat datang di Tjoean! Mari belajar investasi!"
                : "Welcome to Tjoean Investment Simulator. Let's learn to invest wisely.");

            // Initialize bot
            var initialSavings = session.BotCashBalance * 0.05m;
            session.BotSavingsBalance = initialSavings;
            session.BotCashBalance -= initialSavings;

            _sessions[connectionId] = session;
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
                entries.Add(new LeaderboardEntry
                {
                    PlayerName = "Financial Advisor Bot",
                    NetWorth = firstSession.BotNetWorth,
                    IsBot = true,
                    TotalProfit = firstSession.BotNetWorth - 5_000_000 - (firstSession.CurrentYear - 1) * GameSession.YEARLY_INCOME
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

            session.AddLogEntry(session.AgeMode == AgeMode.Kids
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

            session.AddLogEntry(session.AgeMode == AgeMode.Kids
                ? $"Ambil uang dari tabungan Rp {amount:N0}"
                : $"Withdrew Rp {amount:N0} from savings account");
            return true;
        }
    }

    // === DEPOSITO OPERATIONS ===
    public bool BuyDeposito(string connectionId, int periodMonths, decimal amount, bool autoRollOver = false)
    {
        lock (_lock)
        {
            var session = GetSession(connectionId);
            if (session == null || session.IsGameOver || session.IsPaused || session.IsEventPending)
                return false;

            var rate = _depositoRates.FirstOrDefault(r => r.PeriodMonths == periodMonths);
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
                AutoRollOver = autoRollOver
            };

            session.Depositos.Add(deposito);
            session.AddLogEntry(session.AgeMode == AgeMode.Kids
                ? $"Buka Deposito {rate.PeriodName} Rp {amount:N0} (bunga {rate.AnnualRate * 100}%/tahun)"
                : $"Opened {rate.PeriodName} CD of Rp {amount:N0} at {rate.AnnualRate * 100}% p.a.");
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
                session.AddLogEntry(session.AgeMode == AgeMode.Kids
                    ? $"Deposito jatuh tempo! Terima Rp {withdrawAmount:N0}"
                    : $"CD matured! Received Rp {withdrawAmount:N0}");
            }
            else
            {
                var rate = _depositoRates.FirstOrDefault(r => r.PeriodMonths == deposito.PeriodMonths);
                var penaltyRate = rate?.PenaltyRate ?? 0.5m;
                var earnedInterest = deposito.CurrentValue - deposito.Principal;
                var penalty = earnedInterest * penaltyRate;
                withdrawAmount = deposito.Principal + earnedInterest - penalty;
                session.AddLogEntry(session.AgeMode == AgeMode.Kids
                    ? $"Cairkan deposito lebih awal, kena denda. Terima Rp {withdrawAmount:N0}"
                    : $"Early CD withdrawal with {penaltyRate * 100}% penalty. Received Rp {withdrawAmount:N0}");
            }

            session.CashBalance += withdrawAmount;
            session.Depositos.Remove(deposito);
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

            session.AddLogEntry(session.AgeMode == AgeMode.Kids
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

            var rate = _bondRates.FirstOrDefault(r => r.BondType == bondType);
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
                MonthsRemaining = rate.PeriodMonths
            };

            session.Bonds.Add(bond);
            session.AddLogEntry(session.AgeMode == AgeMode.Kids
                ? $"Beli {rate.PeriodName} Rp {amount:N0} (kupon {rate.CouponRate * 100}%/tahun)"
                : $"Purchased {rate.PeriodName} bond of Rp {amount:N0} at {rate.CouponRate * 100}% coupon");
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

            session.AddLogEntry(session.AgeMode == AgeMode.Kids
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

            session.AddLogEntry(session.AgeMode == AgeMode.Kids
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
            var profitText = profit >= 0 ? $"untung Rp {profit:N0}" : $"rugi Rp {Math.Abs(profit):N0}";
            session.AddLogEntry(session.AgeMode == AgeMode.Kids
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
                    DisplayName = session.AgeMode == AgeMode.Kids ? asset.DisplayName : asset.DisplayNameAdult,
                    Units = 0,
                    PricePerUnit = currentPrice,
                    TotalCost = 0
                };
            }

            session.Portfolio[assetType].Units += unitsToBuy;
            session.Portfolio[assetType].TotalCost += GameSession.UNIT_COST;
            session.Portfolio[assetType].PricePerUnit = currentPrice;

            session.AddLogEntry(session.AgeMode == AgeMode.Kids
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
                    DisplayName = session.AgeMode == AgeMode.Kids ? asset.DisplayName : asset.DisplayNameAdult,
                    Units = 0,
                    PricePerUnit = currentPrice,
                    TotalCost = 0
                };
            }

            session.Portfolio[assetType].Units += (int)Math.Floor(grams); // Store grams as units
            session.Portfolio[assetType].TotalCost += totalCost;
            session.Portfolio[assetType].PricePerUnit = currentPrice;

            session.AddLogEntry(session.AgeMode == AgeMode.Kids
                ? $"Beli {grams}g Emas (Rp {totalCost:N0})"
                : $"Purchased {grams}g Gold for Rp {totalCost:N0}");
            return true;
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
            var asset = _assets[assetType];
            var profitText = profit >= 0 ? $"untung Rp {profit:N0}" : $"rugi Rp {Math.Abs(profit):N0}";
            session.AddLogEntry(session.AgeMode == AgeMode.Kids
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
            var asset = _assets[assetType];
            var profitText = profit >= 0 ? $"untung Rp {profit:N0}" : $"rugi Rp {Math.Abs(profit):N0}";
            session.AddLogEntry(session.AgeMode == AgeMode.Kids
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

            session.AddLogEntry(session.AgeMode == AgeMode.Kids
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

            session.AddLogEntry(session.AgeMode == AgeMode.Kids
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
            session.AddLogEntry(session.AgeMode == AgeMode.Kids
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
            session.AddLogEntry(session.AgeMode == AgeMode.Kids
                ? $"Bayar {session.ActiveEvent.Title} dari kas: Rp {cost:N0}"
                : $"Paid {session.ActiveEvent.TitleAdult} from cash: Rp {cost:N0}");
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

            session.AddLogEntry(session.AgeMode == AgeMode.Kids
                ? $"Bayar {session.ActiveEvent.Title} dari tabungan: Rp {cost:N0}"
                : $"Paid {session.ActiveEvent.TitleAdult} from savings: Rp {cost:N0}");
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
                var currentPrice = session.AssetPrices.GetValueOrDefault(assetType, _assets.GetValueOrDefault(assetType)?.BasePrice ?? 1_000_000);
                var portfolioValue = portfolio.Units * currentPrice;

                if (portfolioValue >= cost)
                {
                    var unitsNeeded = (int)Math.Ceiling(cost / currentPrice);
                    if (unitsNeeded > (int)portfolio.Units) unitsNeeded = (int)portfolio.Units;

                    var saleValue = unitsNeeded * currentPrice;
                    portfolio.Units -= unitsNeeded;
                    portfolio.TotalCost -= (portfolio.TotalCost / (portfolio.Units + unitsNeeded)) * unitsNeeded;

                    if (portfolio.Units <= 0)
                        session.Portfolio.Remove(assetType);

                    if (saleValue > cost)
                        session.CashBalance += (saleValue - cost);

                    var displayName = _assets.GetValueOrDefault(assetType)?.DisplayName ?? assetType;
                    session.AddLogEntry(session.AgeMode == AgeMode.Kids
                        ? $"Bayar {session.ActiveEvent.Title} dari {displayName}: Rp {cost:N0}"
                        : $"Paid {session.ActiveEvent.TitleAdult} from portfolio: Rp {cost:N0}");
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
    }

    public void ProcessTick(string connectionId)
    {
        lock (_lock)
        {
            var session = GetSession(connectionId);
            if (session == null || session.IsGameOver || session.IsPaused || session.IsEventPending)
                return;

            if (session.ShowIntro)
                return;

            session.MonthProgress += 20;
            session.NewUnlockMessage = null;

            if (session.MonthProgress >= 100)
            {
                session.MonthProgress = 0;
                ProcessMonthEnd(session);
            }
        }
    }

    private void ProcessMonthEnd(GameSession session)
    {
        // Update savings interest (monthly)
        if (session.SavingsAccount != null)
        {
            var monthlyInterest = session.SavingsAccount.Balance * (session.SavingsAccount.InterestRate / 12);
            session.SavingsAccount.Balance += monthlyInterest;
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
                    var rate = _depositoRates.FirstOrDefault(r => r.PeriodMonths == deposito.PeriodMonths);
                    if (rate != null)
                    {
                        // Reset the deposito with new principal (maturity value)
                        deposito.Principal = maturityValue;
                        deposito.InterestRate = rate.AnnualRate; // Use current rate in case it changed
                        deposito.StartYear = session.CurrentYear;
                        deposito.StartMonth = session.CurrentMonth;
                        deposito.MonthsRemaining = deposito.PeriodMonths;

                        session.AddLogEntry(session.AgeMode == AgeMode.Kids
                            ? $"🔄 Deposito di-roll over otomatis! Principal baru: Rp {maturityValue:N0}"
                            : $"🔄 CD automatically rolled over! New principal: Rp {maturityValue:N0}");
                    }
                    else
                    {
                        // If rate not found, treat as normal maturity
                        session.CashBalance += maturityValue;
                        session.AddLogEntry(session.AgeMode == AgeMode.Kids
                            ? $"Deposito jatuh tempo! +Rp {maturityValue:N0}"
                            : $"CD matured! +Rp {maturityValue:N0}");
                        session.Depositos.Remove(deposito);
                    }
                }
                else
                {
                    // Normal maturity - return to cash
                    session.CashBalance += deposito.MaturityValue;
                    session.AddLogEntry(session.AgeMode == AgeMode.Kids
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

            bond.MonthsRemaining--;
            if (bond.IsMatured)
            {
                session.CashBalance += bond.Principal;
                session.AddLogEntry(session.AgeMode == AgeMode.Kids
                    ? $"Obligasi jatuh tempo! Pokok kembali Rp {bond.Principal:N0}"
                    : $"Bond matured! Principal returned: Rp {bond.Principal:N0}");
                session.Bonds.Remove(bond);
            }
        }

        // Update market prices for fluctuating assets
        UpdateMarketPrices(session);

        // Update stock prices every 2 months
        if (session.CurrentMonth % 2 == 0)
        {
            UpdateStockPrices(session);
        }

        // Update crypto prices monthly
        UpdateCryptoPrices(session);

        // Update portfolio values
        UpdatePortfolioValues(session);

        // Process crowdfunding investments (check maturity and failures)
        ProcessCrowdfundingMonthEnd(session);

        session.CurrentMonth++;

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

        // Check for random event (month 7-10, starting from year 2)
        if (session.CurrentYear >= 2 && session.CurrentMonth == session.EventMonthForYear && !session.EventOccurredThisYear)
        {
            TriggerMonthlyEvent(session);
            session.EventOccurredThisYear = true;
            return;
        }

        if (session.CurrentMonth > GameSession.MONTHS_PER_YEAR)
        {
            session.CurrentMonth = 1;
            session.CurrentYear++;

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
            session.AddLogEntry(session.AgeMode == AgeMode.Kids
                ? $"Terima gaji tahunan: Rp {GameSession.YEARLY_INCOME:N0}"
                : $"Annual income received: Rp {GameSession.YEARLY_INCOME:N0}");

            // Pay dividends for stocks held
            PayStockDividends(session);

            CheckAssetUnlocks(session);
        }

        // Check for mid-year unlock (deposito at month 6)
        if (session.CurrentYear == 1 && session.CurrentMonth == 6)
        {
            CheckAssetUnlocks(session);
        }

        if (session.CurrentYear > GameSession.MAX_YEARS)
        {
            session.IsGameOver = true;
            session.AddLogEntry(session.AgeMode == AgeMode.Kids
                ? $"Permainan selesai! Kekayaan akhir: Rp {session.NetWorth:N0}"
                : $"Game complete! Final net worth: Rp {session.NetWorth:N0}");
        }

        // Process bot month end (same month as player)
        ProcessBotMonthEnd(session);
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

            // 20% annual failure rate = ~1.7% monthly chance
            // We'll use 2% monthly chance for simplicity (roughly 22% annual)
            if (_random.NextDouble() < 0.02)
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

                failedProjects.Add(investment.ProjectName);

                session.AddLogEntry(session.AgeMode == AgeMode.Kids
                    ? $"😢 GAGAL! Proyek {investment.ProjectName} bangkrut! Investasi Rp {investment.InvestedAmount:N0} hilang!"
                    : $"❌ FAILURE! {investment.ProjectName} failed due to {investment.FailureReason}. Investment of Rp {investment.InvestedAmount:N0} lost!");
            }
            else if (investment.IsMatured)
            {
                // Investment matured successfully - return with profit
                var totalReturn = investment.InvestedAmount * (1 + investment.ExpectedReturn * investment.LockUpMonths / 12);
                session.CashBalance += totalReturn;

                session.AddLogEntry(session.AgeMode == AgeMode.Kids
                    ? $"🎉 Sukses! Investasi {investment.ProjectName} jatuh tempo! +Rp {totalReturn:N0}"
                    : $"✅ SUCCESS! {investment.ProjectName} matured! +Rp {totalReturn:N0}");

                session.CrowdfundingInvestments.Remove(investment);
            }
        }

        // Set failure message for UI notification
        if (failedProjects.Any())
        {
            session.CrowdfundingFailureMessage = session.AgeMode == AgeMode.Kids
                ? $"😢 Oh tidak! Proyek {string.Join(", ", failedProjects)} GAGAL! Uangmu hilang!"
                : $"❌ Project failure: {string.Join(", ", failedProjects)}. Your investment is lost.";
        }
    }

    /// <summary>
    /// Pay annual dividends for all stocks held by player
    /// Dividend = Number of shares * Current price * Dividend yield
    /// </summary>
    private void PayStockDividends(GameSession session)
    {
        decimal totalDividends = 0;
        var dividendDetails = new List<string>();

        foreach (var portfolio in session.Portfolio.Values.Where(p => p.AssetType == "saham"))
        {
            var stock = session.AvailableStocks.FirstOrDefault(s => s.Ticker == portfolio.Ticker);
            if (stock != null && stock.DividendYield > 0)
            {
                // Dividend = shares * current price * yield
                var dividend = Math.Round(portfolio.Units * stock.CurrentPrice * stock.DividendYield, 0);
                if (dividend > 0)
                {
                    totalDividends += dividend;
                    dividendDetails.Add($"{stock.Ticker}: Rp {dividend:N0}");
                }
            }
        }

        if (totalDividends > 0)
        {
            session.CashBalance += totalDividends;
            session.AddLogEntry(session.AgeMode == AgeMode.Kids
                ? $"Terima dividen saham! +Rp {totalDividends:N0}"
                : $"Stock dividends received: +Rp {totalDividends:N0}");
        }
    }

    /// <summary>
    /// Process bot's monthly updates including:
    /// - Savings interest
    /// - Deposito maturity
    /// - Bond maturity and coupon payments
    /// - Investment rebalancing when assets unlock
    /// - Yearly income allocation
    /// Bot uses emerging market balanced strategy recommended by financial advisors:
    /// 5% Savings, 25% Deposito, 20% Bonds, 30% Index Fund, 20% Gold
    /// </summary>
    private void ProcessBotMonthEnd(GameSession session)
    {
        // Apply savings interest (1% annually = 0.0833% monthly)
        if (session.BotSavingsBalance > 0)
        {
            var monthlyInterest = session.BotSavingsBalance * (0.01m / 12);
            session.BotSavingsBalance += monthlyInterest;
        }

        // Process bot depositos - check maturity
        foreach (var deposito in session.BotDepositos.ToList())
        {
            deposito.MonthsRemaining--;
            if (deposito.IsMatured)
            {
                session.BotCashBalance += deposito.MaturityValue;
                session.BotDepositos.Remove(deposito);
            }
        }

        // Process bot bonds - check maturity
        foreach (var bond in session.BotBonds.ToList())
        {
            bond.MonthsRemaining--;
            if (bond.IsMatured)
            {
                // Return principal + final coupon payment
                session.BotCashBalance += bond.CurrentValue;
                session.BotBonds.Remove(bond);
            }
        }

        // Bot investment logic based on game progression
        var totalMonths = session.TotalGameMonths;

        // Month 6: Deposito unlocks - invest 25% allocation in 12-month CD
        if (totalMonths == 6 && session.BotDepositos.Count == 0)
        {
            var depositoAmount = Math.Min(session.BotCashBalance, 2_500_000m);
            if (depositoAmount >= 1_000_000m)
            {
                var rate = _depositoRates.FirstOrDefault(r => r.PeriodMonths == 12);
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

        // Year 2 (Month 13): Index Fund unlocks - invest 30% of available funds
        if (totalMonths == 13 && session.BotIndexFundUnits == 0)
        {
            var indexAmount = Math.Min(session.BotCashBalance, 3_000_000m);
            if (indexAmount >= 100_000m)
            {
                var price = session.AssetPrices.GetValueOrDefault("reksadana", 1_000_000m);
                var units = indexAmount / price;
                session.BotIndexFundUnits = units;
                session.BotIndexFundCost = indexAmount;
                session.BotCashBalance -= indexAmount;
            }
        }

        // Year 3 (Month 25): Bonds unlock - invest 20% allocation in SBR (2 year)
        if (totalMonths == 25 && session.BotBonds.Count == 0)
        {
            var bondAmount = Math.Min(session.BotCashBalance, 2_000_000m);
            if (bondAmount >= 1_000_000m)
            {
                var rate = _bondRates.FirstOrDefault(r => r.BondType == "SBR");
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
                        MonthsRemaining = rate.PeriodMonths
                    });
                    session.BotCashBalance -= bondAmount;
                }
            }
        }

        // Year 5 (Month 49): Gold unlocks - invest 20% allocation
        if (totalMonths == 49 && session.BotGoldUnits == 0)
        {
            var goldAmount = Math.Min(session.BotCashBalance, 2_000_000m);
            if (goldAmount >= 50_000m)
            {
                var price = session.AssetPrices.GetValueOrDefault("emas", 1_000_000m);
                var units = goldAmount / price;
                session.BotGoldUnits = units;
                session.BotGoldCost = goldAmount;
                session.BotCashBalance -= goldAmount;
            }
        }

        // On year end (when player receives income), bot also receives and reinvests
        if (session.CurrentMonth == 1 && session.CurrentYear > 1)
        {
            session.BotCashBalance += GameSession.YEARLY_INCOME;
            RebalanceBotPortfolio(session);
        }
    }

    /// <summary>
    /// Rebalance bot portfolio to maintain target allocation
    /// Emerging market balanced strategy: 5% Savings, 25% Deposito, 20% Bonds, 30% Index, 20% Gold
    /// </summary>
    private void RebalanceBotPortfolio(GameSession session)
    {
        var availableCash = session.BotCashBalance;
        if (availableCash < 1_000_000m) return;

        var totalMonths = session.TotalGameMonths;

        // Maintain reserve for potential events (5M covers most event costs)
        var reserveAmount = 5_000_000m;
        var investableAmount = Math.Max(0, availableCash - reserveAmount);

        if (investableAmount < 1_000_000m) return;

        // Allocate based on what's unlocked
        // Target: 30% Index Fund, 25% Deposito, 20% Bonds, 20% Gold, 5% Savings

        // If Gold is unlocked (Month 49+) - 20% allocation
        if (totalMonths >= 49 && investableAmount >= 100_000m)
        {
            var goldInvestment = Math.Min(investableAmount * 0.20m, investableAmount);
            if (goldInvestment >= 100_000m)
            {
                var price = session.AssetPrices.GetValueOrDefault("emas", 1_000_000m);
                var units = goldInvestment / price;
                session.BotGoldUnits += units;
                session.BotGoldCost += goldInvestment;
                session.BotCashBalance -= goldInvestment;
                investableAmount -= goldInvestment;
            }
        }

        // If Index Fund is unlocked (Month 13+) - 30% allocation (largest for growth)
        if (totalMonths >= 13 && investableAmount >= 100_000m)
        {
            var indexInvestment = Math.Min(investableAmount * 0.30m, investableAmount);
            if (indexInvestment >= 100_000m)
            {
                var price = session.AssetPrices.GetValueOrDefault("reksadana", 1_000_000m);
                var units = indexInvestment / price;
                session.BotIndexFundUnits += units;
                session.BotIndexFundCost += indexInvestment;
                session.BotCashBalance -= indexInvestment;
                investableAmount -= indexInvestment;
            }
        }

        // If Bonds are unlocked (Month 25+) - 20% allocation
        if (totalMonths >= 25 && investableAmount >= 1_000_000m)
        {
            var bondAmount = Math.Min(investableAmount * 0.20m, investableAmount);
            if (bondAmount >= 1_000_000m)
            {
                var rate = _bondRates.FirstOrDefault(r => r.BondType == "SBR");
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
                        MonthsRemaining = rate.PeriodMonths
                    });
                    session.BotCashBalance -= bondAmount;
                    investableAmount -= bondAmount;
                }
            }
        }

        // If Deposito is unlocked (Month 6+) - 25% allocation
        if (totalMonths >= 6 && investableAmount >= 1_000_000m)
        {
            var depositoAmount = Math.Min(investableAmount * 0.25m, investableAmount);
            if (depositoAmount >= 1_000_000m)
            {
                var rate = _depositoRates.FirstOrDefault(r => r.PeriodMonths == 12);
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
                    investableAmount -= depositoAmount;
                }
            }
        }

        // Put remaining 5% into savings for emergency fund
        if (investableAmount >= 100_000m)
        {
            var savingsAmount = investableAmount * 0.05m;
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
            var price = session.AssetPrices.GetValueOrDefault("reksadana", 1_000_000m);
            var unitsToSell = remaining / price;
            session.BotIndexFundUnits -= unitsToSell;
            session.BotEventsPaidFromPortfolio++;
            return;
        }
        else if (indexFundValue > 0)
        {
            remaining -= indexFundValue;
            session.BotIndexFundUnits = 0;
        }

        // 4. Try gold (liquid commodity)
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

        // 5. Try bonds (early redemption with partial penalty)
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

        // 6. Last resort - liquidate depositos (early withdrawal with penalty)
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

    private void CheckAssetUnlocks(GameSession session)
    {
        var newUnlocks = new List<string>();
        var totalMonths = session.TotalGameMonths;

        foreach (var asset in _assets)
        {
            var unlockMonth = (asset.Value.UnlockYear - 1) * 12 + asset.Value.UnlockMonth;

            if (totalMonths >= unlockMonth && !session.UnlockedAssets.Contains(asset.Key))
            {
                session.UnlockedAssets.Add(asset.Key);
                newUnlocks.Add(session.AgeMode == AgeMode.Kids ? asset.Value.DisplayName : asset.Value.DisplayNameAdult);

                // Show intro for newly unlocked asset
                session.ShowIntro = true;
                session.IntroAssetType = asset.Key;
            }
        }

        if (newUnlocks.Any())
        {
            var message = session.AgeMode == AgeMode.Kids
                ? $"BARU! Investasi terbuka: {string.Join(", ", newUnlocks)}"
                : $"NEW! Unlocked: {string.Join(", ", newUnlocks)}";
            session.NewUnlockMessage = message;
            session.AddLogEntry(message);
        }
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

        foreach (var asset in _assets.Where(a => !a.Value.IsFixedIncome && a.Value.Category != "stock" && a.Value.Category != "crypto"))
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
    }

    private void UpdateStockPrices(GameSession session)
    {
        var rng = GetRngForSession(session);

        foreach (var stock in session.AvailableStocks)
        {
            stock.PreviousPrice = stock.CurrentPrice;

            // Random change between -15% to +20%
            var changePercent = (decimal)(rng.NextDouble() * 0.35 - 0.15);

            // 10% chance of big move
            if (rng.NextDouble() < 0.10)
            {
                changePercent *= 2;
            }

            var newPrice = stock.CurrentPrice * (1 + changePercent);
            newPrice = Math.Max(100, Math.Min(stock.CurrentPrice * 3, newPrice)); // Price bounds
            stock.CurrentPrice = Math.Round(newPrice, 0);
        }
    }

    private void UpdateCryptoPrices(GameSession session)
    {
        var rng = GetRngForSession(session);

        foreach (var crypto in session.AvailableCryptos)
        {
            crypto.PreviousPrice = crypto.CurrentPrice;

            // Crypto volatility range: -40% to +40% monthly for all coins
            decimal minChange = -0.40m; // -40%
            decimal maxChange = 0.40m;  // +40%

            // Calculate random change within the volatility range
            var range = maxChange - minChange;
            var baseChange = minChange + (decimal)(rng.NextDouble() * (double)range);

            var newPrice = crypto.CurrentPrice * (1 + baseChange);
            // Minimum price: 1 IDR
            newPrice = Math.Max(1, newPrice);
            crypto.CurrentPrice = Math.Round(newPrice, 0);
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
        session.IsPaused = true;

        session.AddLogEntry(session.AgeMode == AgeMode.Kids
            ? $"EVENT BULAN {session.CurrentMonth}: {evt.Title}"
            : $"MONTH {session.CurrentMonth} EVENT: {evt.TitleAdult}");

        // Bot experiences the same event and pays automatically
        ProcessBotEventPayment(session, randomizedCost);

        var totalAssets = session.CashBalance + session.TotalSavingsValue + session.TotalPortfolioValue;
        if (totalAssets < randomizedCost)
        {
            session.IsGameOver = true;
            session.GameOverReason = session.AgeMode == AgeMode.Kids
                ? $"Tidak mampu membayar {evt.Title}. Total aset Rp {totalAssets:N0} tidak cukup untuk Rp {randomizedCost:N0}."
                : $"Unable to pay {evt.TitleAdult}. Total assets Rp {totalAssets:N0} insufficient for Rp {randomizedCost:N0}.";
            session.AddLogEntry($"GAME OVER: {session.GameOverReason}");
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

                // Reinitialize stocks with dividend yields
                var selectedStocks = _allStocks.OrderBy(_ => _random.Next()).Take(5).Select(s => new StockInfo
                {
                    Ticker = s.Ticker,
                    CompanyName = s.CompanyName,
                    Sector = s.Sector,
                    CurrentPrice = s.CurrentPrice,
                    PreviousPrice = s.PreviousPrice,
                    LastPriceUpdateMonth = 0,
                    DividendYield = s.DividendYield
                }).ToList();
                session.InitializeStocks(selectedStocks);

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
                    Description = p.Description,
                    FundingGoal = p.FundingGoal,
                    CurrentFunding = 0,
                    MinimumInvestment = p.MinimumInvestment,
                    DaysRemaining = 90 + _random.Next(-30, 30),
                    ExpectedReturn = p.ExpectedReturn,
                    RiskLevel = p.RiskLevel,
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
