using System.Text.Json;
using InvestmentGame.Shared;

namespace InvestmentGame.Server.Services;

/// <summary>
/// Singleton service that loads real historical stock prices from JSON at startup.
/// Also loads real dividend data from JSON.
/// Year mapping configured in GameConfig: Game Year 1 = Calendar Year 2006.
/// Stocks unlock at Y4M1 → first price shown = January 2009.
///
/// Besides the 20 surviving blue chips it also loads the historically-failed IDX stocks
/// (Data/Stocks/13_failed_stock_monthly_prices.json): their prices live in the same lookup,
/// their suspension/delisting dates in <see cref="FailedStocks"/>.
/// </summary>
public class StockDataService
{
    private static readonly string[] MonthAbbr = { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

    // ticker → (year, month) → price
    private readonly Dictionary<string, Dictionary<(int year, int month), decimal>> _prices = new();

    // ticker → calendarYear → (totalAmount, type)
    private readonly Dictionary<string, Dictionary<int, (decimal totalAmount, string type)>> _dividends = new();

    // ticker → suspension/delisting metadata for stocks that really failed on IDX
    private readonly Dictionary<string, FailedStock> _failedStocks = new();

    public StockDataService(IWebHostEnvironment env)
    {
        var jsonPath = Path.Combine(env.ContentRootPath, "..", "Data", "Stocks", "01_stock_monthly_prices.json");
        if (!File.Exists(jsonPath))
        {
            jsonPath = Path.Combine(env.ContentRootPath, "Data", "Stocks", "01_stock_monthly_prices.json");
        }

        if (File.Exists(jsonPath))
        {
            ParseJson(jsonPath);
        }

        var divPath = Path.Combine(env.ContentRootPath, "..", "Data", "Stocks", "02_stock_dividends.json");
        if (!File.Exists(divPath))
        {
            divPath = Path.Combine(env.ContentRootPath, "Data", "Stocks", "02_stock_dividends.json");
        }

        if (File.Exists(divPath))
        {
            ParseDividends(divPath);
        }

        var failedPath = Path.Combine(env.ContentRootPath, "..", "Data", "Stocks", "13_failed_stock_monthly_prices.json");
        if (!File.Exists(failedPath))
        {
            failedPath = Path.Combine(env.ContentRootPath, "Data", "Stocks", "13_failed_stock_monthly_prices.json");
        }

        if (File.Exists(failedPath))
        {
            // Same "stocks" shape as the blue-chip file, so prices merge into the same lookup
            ParseJson(failedPath);
            ParseFailures(failedPath);
        }
    }

    private void ParseJson(string path)
    {
        var json = File.ReadAllText(path);
        using var doc = JsonDocument.Parse(json);

        if (!doc.RootElement.TryGetProperty("stocks", out var stocks)) return;

        foreach (var ticker in stocks.EnumerateObject())
        {
            var tickerPrices = new Dictionary<(int, int), decimal>();

            foreach (var yearProp in ticker.Value.EnumerateObject())
            {
                if (!int.TryParse(yearProp.Name, out var year)) continue;

                foreach (var monthProp in yearProp.Value.EnumerateObject())
                {
                    var monthIdx = Array.IndexOf(MonthAbbr, monthProp.Name);
                    if (monthIdx < 0) continue;
                    var monthNum = monthIdx + 1;

                    if (monthProp.Value.TryGetDecimal(out var price))
                        tickerPrices[(year, monthNum)] = price;
                }
            }

            _prices[ticker.Name] = tickerPrices;
        }
    }

    /// <summary>
    /// Get the stock price for a given game year and month.
    /// Uses GameConfig.ToCalendarYear() for year mapping.
    /// </summary>
    public decimal? GetPrice(string ticker, int gameYear, int gameMonth)
    {
        var calendarYear = GameConfig.ToCalendarYear(gameYear);
        if (_prices.TryGetValue(ticker, out var tickerPrices))
        {
            if (tickerPrices.TryGetValue((calendarYear, gameMonth), out var price))
                return price;
        }
        return null;
    }

    /// <summary>
    /// Get price history for mini chart. Returns last (monthsBack+1) prices ending at current month.
    /// </summary>
    public List<decimal> GetPriceHistory(string ticker, int gameYear, int gameMonth, int monthsBack = 6)
    {
        var result = new List<decimal>();
        var calendarYear = GameConfig.ToCalendarYear(gameYear);

        // Walk backwards from current month
        var cy = calendarYear;
        var cm = gameMonth;

        // Collect prices going back
        var prices = new List<(int y, int m, decimal p)>();
        for (int i = 0; i <= monthsBack; i++)
        {
            if (_prices.TryGetValue(ticker, out var tickerPrices))
            {
                if (tickerPrices.TryGetValue((cy, cm), out var price))
                    prices.Add((cy, cm, price));
            }

            // Move back one month
            cm--;
            if (cm < 1)
            {
                cm = 12;
                cy--;
            }
        }

        // Reverse so oldest is first
        prices.Reverse();
        return prices.Select(p => p.p).ToList();
    }

    /// <summary>
    /// Check if we have price data for a given ticker.
    /// </summary>
    public bool HasData(string ticker) => _prices.ContainsKey(ticker);

    private void ParseDividends(string path)
    {
        var json = File.ReadAllText(path);
        var entries = JsonSerializer.Deserialize<List<DividendEntry>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (entries == null) return;

        foreach (var entry in entries)
        {
            if (!_dividends.ContainsKey(entry.Ticker))
                _dividends[entry.Ticker] = new Dictionary<int, (decimal, string)>();

            var dict = _dividends[entry.Ticker];
            if (dict.TryGetValue(entry.Year, out var existing))
            {
                // Accumulate multiple dividends in same year (Final + Interim)
                var newTotal = existing.totalAmount + entry.AmountPerShare;
                var newType = existing.type == entry.Type ? existing.type : "Final+Interim";
                dict[entry.Year] = (newTotal, newType);
            }
            else
            {
                dict[entry.Year] = (entry.AmountPerShare, entry.Type);
            }
        }
    }

    /// <summary>
    /// Get dividend data for a ticker in a given game year.
    /// Returns (totalAmountPerShare, type) or null if no dividend that year.
    /// </summary>
    public (decimal amount, string type)? GetDividend(string ticker, int gameYear)
    {
        var calendarYear = GameConfig.ToCalendarYear(gameYear);
        if (_dividends.TryGetValue(ticker, out var yearData))
        {
            if (yearData.TryGetValue(calendarYear, out var div))
                return (div.totalAmount, div.type);
        }
        return null;
    }

    /// <summary>
    /// The historically-failed stocks, in file order. One of them is added to every game session.
    /// </summary>
    public IReadOnlyList<FailedStock> FailedStocks => _failedStocks.Values.ToList();

    /// <summary>
    /// Failure metadata for a ticker, or null when the ticker is one of the surviving blue chips.
    /// </summary>
    public FailedStock? GetFailedStock(string ticker) => _failedStocks.GetValueOrDefault(ticker);

    private void ParseFailures(string path)
    {
        var json = File.ReadAllText(path);
        using var doc = JsonDocument.Parse(json);

        if (!doc.RootElement.TryGetProperty("failures", out var failures)) return;

        foreach (var entry in failures.EnumerateObject())
        {
            var v = entry.Value;
            var (listedYear, listedMonth) = ParseYearMonth(v, "listed_from");
            var (suspendedYear, suspendedMonth) = ParseYearMonth(v, "suspended_from");
            var (delistedYear, delistedMonth) = ParseYearMonth(v, "delisted_on");

            _failedStocks[entry.Name] = new FailedStock
            {
                Ticker = entry.Name,
                CompanyName = v.TryGetProperty("company_name", out var n) ? n.GetString() ?? entry.Name : entry.Name,
                Sector = v.TryGetProperty("sector", out var sc) ? sc.GetString() ?? string.Empty : string.Empty,
                IsShariahCompliant = v.TryGetProperty("shariah_compliant", out var sh) && sh.GetBoolean(),
                ListedYear = listedYear,
                ListedMonth = listedMonth,
                SuspendedYear = suspendedYear,
                SuspendedMonth = suspendedMonth,
                DelistedYear = delistedYear,
                DelistedMonth = delistedMonth,
                ResidualValuePerShare = v.TryGetProperty("residual_value_per_share", out var rv) && rv.TryGetDecimal(out var rvd) ? rvd : 0m,
                FallbackPrice = v.TryGetProperty("fallback_price", out var fp) && fp.TryGetDecimal(out var fpd) ? fpd : 0m
            };
        }
    }

    /// <summary>Reads a "YYYY-MM" property into a (year, month) pair; both null when absent.</summary>
    private static (int? year, int? month) ParseYearMonth(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var prop)) return (null, null);

        var raw = prop.ValueKind == JsonValueKind.String ? prop.GetString() : null;
        if (string.IsNullOrWhiteSpace(raw)) return (null, null);

        var parts = raw.Split('-');
        if (parts.Length != 2) return (null, null);
        if (!int.TryParse(parts[0], out var year) || !int.TryParse(parts[1], out var month)) return (null, null);

        return (year, month);
    }

    private class DividendEntry
    {
        public string Ticker { get; set; } = string.Empty;
        public int Year { get; set; }
        public decimal AmountPerShare { get; set; }
        public string Type { get; set; } = string.Empty;
    }
}

/// <summary>
/// An IDX stock that really failed between 2009 and 2020: trading was suspended by the exchange
/// and, in most cases, the listing was removed afterwards. Dates are calendar dates.
/// </summary>
public class FailedStock
{
    public string Ticker { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string Sector { get; set; } = string.Empty;
    public bool IsShariahCompliant { get; set; }

    /// <summary>Calendar year/month the stock started trading, or null if it was already listed in 2009.</summary>
    public int? ListedYear { get; set; }
    public int? ListedMonth { get; set; }

    /// <summary>Calendar year/month the exchange halted trading, or null if it never was suspended.</summary>
    public int? SuspendedYear { get; set; }
    public int? SuspendedMonth { get; set; }

    /// <summary>Calendar year/month the listing was removed, or null if the stock is still listed.</summary>
    public int? DelistedYear { get; set; }
    public int? DelistedMonth { get; set; }

    /// <summary>What a share is still worth to the holder once the listing is gone (0 for a wipe-out).</summary>
    public decimal ResidualValuePerShare { get; set; }

    /// <summary>Price used when the monthly series has no entry for the requested month.</summary>
    public decimal FallbackPrice { get; set; }

    /// <summary>True once the stock has had its IPO (always true for stocks listed before 2009).</summary>
    public bool IsListedAt(int calendarYear, int month) =>
        !ListedYear.HasValue || !ListedMonth.HasValue || HasReached(calendarYear, month, ListedYear, ListedMonth);

    /// <summary>The first month the stock can be traded, as (year, month); (0, 0) when it was always listed.</summary>
    public (int year, int month) FirstListedMonth => (ListedYear ?? 0, ListedMonth ?? 0);

    /// <summary>True once the exchange has halted trading in the stock.</summary>
    public bool IsSuspendedAt(int calendarYear, int month) =>
        HasReached(calendarYear, month, SuspendedYear, SuspendedMonth);

    /// <summary>True once the given calendar month is at or past the delisting date.</summary>
    public bool IsDelistedAt(int calendarYear, int month) =>
        HasReached(calendarYear, month, DelistedYear, DelistedMonth);

    /// <summary>True when (year, month) is at or past the given event date, which may be unset.</summary>
    private static bool HasReached(int calendarYear, int month, int? eventYear, int? eventMonth)
    {
        if (!eventYear.HasValue || !eventMonth.HasValue) return false;
        return calendarYear > eventYear.Value
               || (calendarYear == eventYear.Value && month >= eventMonth.Value);
    }
}
