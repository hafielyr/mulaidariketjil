using System.Text.Json;
using InvestmentGame.Shared;

namespace InvestmentGame.Server.Services;

/// <summary>
/// Singleton service that loads real historical crypto prices from JSON at startup.
/// Data from Data/Crypto/12_crypto_monthly_prices.json (BTC, XRP, XLM).
/// Prices in USD are converted to IDR using historical annual average exchange rates.
/// Uses GameConfig.ToCalendarYear() for year mapping.
/// </summary>
public class CryptoDataService
{
    // symbol → (calendarYear, month) → priceIDR
    private readonly Dictionary<string, Dictionary<(int year, int month), decimal>> _prices = new();

    // symbol → display name
    private readonly Dictionary<string, string> _coinNames = new();

    private static readonly Dictionary<string, int> MonthMap = new()
    {
        ["Jan"] = 1, ["Feb"] = 2, ["Mar"] = 3, ["Apr"] = 4,
        ["May"] = 5, ["Jun"] = 6, ["Jul"] = 7, ["Aug"] = 8,
        ["Sep"] = 9, ["Oct"] = 10, ["Nov"] = 11, ["Dec"] = 12
    };

    // Historical annual average USD/IDR exchange rates
    private static readonly Dictionary<int, decimal> UsdToIdr = new()
    {
        [2017] = 13381m,
        [2018] = 14237m,
        [2019] = 14148m,
        [2020] = 14583m,
        [2021] = 14269m,
    };

    public CryptoDataService(IWebHostEnvironment env)
    {
        var path = Path.Combine(env.ContentRootPath, "..", "Data", "Crypto", "12_crypto_monthly_prices.json");
        if (!File.Exists(path))
            path = Path.Combine(env.ContentRootPath, "Data", "Crypto", "12_crypto_monthly_prices.json");

        if (File.Exists(path))
            ParseJson(path);
    }

    private void ParseJson(string path)
    {
        var json = File.ReadAllText(path);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Parse coin names
        if (root.TryGetProperty("coins", out var coins))
        {
            foreach (var coinProp in coins.EnumerateObject())
            {
                if (coinProp.Value.TryGetProperty("name", out var name))
                    _coinNames[coinProp.Name] = name.GetString() ?? coinProp.Name;
            }
        }

        if (!root.TryGetProperty("monthly_prices", out var monthlyPrices))
            return;

        foreach (var symbolProp in monthlyPrices.EnumerateObject())
        {
            var symbol = symbolProp.Name; // "BTC", "XRP", "XLM"
            if (!_prices.ContainsKey(symbol))
                _prices[symbol] = new Dictionary<(int, int), decimal>();

            foreach (var yearProp in symbolProp.Value.EnumerateObject())
            {
                if (!int.TryParse(yearProp.Name, out var year)) continue;
                if (!UsdToIdr.TryGetValue(year, out var rate)) continue;

                foreach (var monthProp in yearProp.Value.EnumerateObject())
                {
                    if (!MonthMap.TryGetValue(monthProp.Name, out var month)) continue;
                    if (monthProp.Value.ValueKind == JsonValueKind.Null) continue;
                    if (monthProp.Value.TryGetDecimal(out var priceUsd))
                    {
                        _prices[symbol][(year, month)] = Math.Round(priceUsd * rate);
                    }
                }
            }
        }
    }

    public decimal? GetPrice(string symbol, int gameYear, int gameMonth)
    {
        var calendarYear = GameConfig.ToCalendarYear(gameYear);
        if (_prices.TryGetValue(symbol, out var symbolPrices))
        {
            if (symbolPrices.TryGetValue((calendarYear, gameMonth), out var price))
                return price;
        }
        return null;
    }

    public List<decimal> GetPriceHistory(string symbol, int gameYear, int gameMonth, int monthsBack = 6)
    {
        var calendarYear = GameConfig.ToCalendarYear(gameYear);
        var cy = calendarYear;
        var cm = gameMonth;

        var prices = new List<(int y, int m, decimal p)>();
        for (int i = 0; i <= monthsBack; i++)
        {
            if (_prices.TryGetValue(symbol, out var symbolPrices))
            {
                if (symbolPrices.TryGetValue((cy, cm), out var price))
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

    public string GetCoinName(string symbol)
    {
        return _coinNames.TryGetValue(symbol, out var name) ? name : symbol;
    }
}
