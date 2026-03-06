using System.Text.Json;
using InvestmentGame.Shared;

namespace InvestmentGame.Server.Services;

/// <summary>
/// Singleton service that loads real historical stock prices from JSON at startup.
/// Also loads real dividend data from JSON.
/// Year mapping configured in GameConfig: Game Year 1 = Calendar Year 2006.
/// Stocks unlock at Y4M1 → first price shown = January 2009.
/// </summary>
public class StockDataService
{
    private static readonly string[] MonthAbbr = { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

    // ticker → (year, month) → price
    private readonly Dictionary<string, Dictionary<(int year, int month), decimal>> _prices = new();

    // ticker → calendarYear → (totalAmount, type)
    private readonly Dictionary<string, Dictionary<int, (decimal totalAmount, string type)>> _dividends = new();

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

    private class DividendEntry
    {
        public string Ticker { get; set; } = string.Empty;
        public int Year { get; set; }
        public decimal AmountPerShare { get; set; }
        public string Type { get; set; } = string.Empty;
    }
}
