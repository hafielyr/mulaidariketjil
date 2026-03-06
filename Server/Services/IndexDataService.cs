using System.Text.Json;
using InvestmentGame.Shared;

namespace InvestmentGame.Server.Services;

/// <summary>
/// Singleton service that loads real historical index prices from JSON at startup.
/// Conventional indices from 03_index_monthly_prices.json (IHSG, LQ45, BISNIS27).
/// Shariah indices from 08_shariah_index_monthly.json (JII).
/// Uses GameConfig.ToCalendarYear() for year mapping.
/// </summary>
public class IndexDataService
{
    // indexId → (calendarYear, month) → price
    private readonly Dictionary<string, Dictionary<(int year, int month), decimal>> _prices = new();

    private static readonly Dictionary<string, int> MonthMap = new()
    {
        ["Jan"] = 1, ["Feb"] = 2, ["Mar"] = 3, ["Apr"] = 4,
        ["May"] = 5, ["Jun"] = 6, ["Jul"] = 7, ["Aug"] = 8,
        ["Sep"] = 9, ["Oct"] = 10, ["Nov"] = 11, ["Dec"] = 12
    };

    public IndexDataService(IWebHostEnvironment env)
    {
        // Load conventional indices
        var convPath = Path.Combine(env.ContentRootPath, "..", "Data", "Index", "03_index_monthly_prices.json");
        if (!File.Exists(convPath))
            convPath = Path.Combine(env.ContentRootPath, "Data", "Index", "03_index_monthly_prices.json");

        if (File.Exists(convPath))
            ParseConventionalJson(convPath);

        // Load shariah indices
        var shariahPath = Path.Combine(env.ContentRootPath, "..", "Data", "Index", "08_shariah_index_monthly.json");
        if (!File.Exists(shariahPath))
            shariahPath = Path.Combine(env.ContentRootPath, "Data", "Index", "08_shariah_index_monthly.json");

        if (File.Exists(shariahPath))
            ParseShariahJson(shariahPath);
    }

    private void ParseConventionalJson(string path)
    {
        var json = File.ReadAllText(path);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("monthly_prices", out var monthlyPrices))
            return;

        foreach (var indexProp in monthlyPrices.EnumerateObject())
        {
            var indexId = indexProp.Name; // "IHSG", "LQ45", "BISNIS27"
            if (!_prices.ContainsKey(indexId))
                _prices[indexId] = new Dictionary<(int, int), decimal>();

            foreach (var yearProp in indexProp.Value.EnumerateObject())
            {
                if (!int.TryParse(yearProp.Name, out var year)) continue;

                foreach (var monthProp in yearProp.Value.EnumerateObject())
                {
                    if (!MonthMap.TryGetValue(monthProp.Name, out var month)) continue;
                    if (monthProp.Value.ValueKind == JsonValueKind.Null) continue;
                    if (monthProp.Value.TryGetDecimal(out var price))
                    {
                        _prices[indexId][(year, month)] = price;
                    }
                }
            }
        }
    }

    private void ParseShariahJson(string path)
    {
        var json = File.ReadAllText(path);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("indices", out var indices))
            return;

        foreach (var indexProp in indices.EnumerateObject())
        {
            var indexId = indexProp.Name; // "JII", "ISSI"
            if (!indexProp.Value.TryGetProperty("monthly", out var monthly))
                continue;

            if (!_prices.ContainsKey(indexId))
                _prices[indexId] = new Dictionary<(int, int), decimal>();

            foreach (var yearProp in monthly.EnumerateObject())
            {
                if (!int.TryParse(yearProp.Name, out var year)) continue;

                foreach (var monthProp in yearProp.Value.EnumerateObject())
                {
                    if (!MonthMap.TryGetValue(monthProp.Name, out var month)) continue;
                    if (monthProp.Value.ValueKind == JsonValueKind.Null) continue;
                    if (monthProp.Value.TryGetDecimal(out var price))
                    {
                        _prices[indexId][(year, month)] = price;
                    }
                }
            }
        }
    }

    public decimal? GetPrice(string indexId, int gameYear, int gameMonth)
    {
        var calendarYear = GameConfig.ToCalendarYear(gameYear);
        if (_prices.TryGetValue(indexId, out var indexPrices))
        {
            if (indexPrices.TryGetValue((calendarYear, gameMonth), out var price))
                return price;
        }
        return null;
    }

    public List<decimal> GetPriceHistory(string indexId, int gameYear, int gameMonth, int monthsBack = 6)
    {
        var calendarYear = GameConfig.ToCalendarYear(gameYear);
        var cy = calendarYear;
        var cm = gameMonth;

        var prices = new List<(int y, int m, decimal p)>();
        for (int i = 0; i <= monthsBack; i++)
        {
            if (_prices.TryGetValue(indexId, out var indexPrices))
            {
                if (indexPrices.TryGetValue((cy, cm), out var price))
                    prices.Add((cy, cm, price));
            }

            cm--;
            if (cm < 1)
            {
                cm = 12;
                cy--;
            }
        }

        prices.Reverse();
        return prices.Select(p => p.p).ToList();
    }

    /// <summary>
    /// Get available conventional indices (those with data from 2007+, game year 2+).
    /// </summary>
    public List<string> GetConventionalIndices() => new() { "IHSG", "LQ45" };

    /// <summary>
    /// Get available shariah indices (JII has data from 2006).
    /// ISSI excluded: data only from 2011, game needs index data from Y2 = 2007.
    /// </summary>
    public List<string> GetShariahIndices() => new() { "JII" };

    public bool HasData(string indexId, int gameYear, int gameMonth)
    {
        var calendarYear = GameConfig.ToCalendarYear(gameYear);
        return _prices.TryGetValue(indexId, out var indexPrices)
            && indexPrices.ContainsKey((calendarYear, gameMonth));
    }
}
