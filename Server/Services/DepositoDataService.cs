using System.Text.Json;
using InvestmentGame.Shared;

namespace InvestmentGame.Server.Services;

/// <summary>
/// Singleton service that loads real historical deposito rates from JSON at startup.
/// Conventional: BRI (2006-2021, 5 tenors: 1m/3m/6m/12m/24m)
/// Shariah: Bank Muamalat Mudharabah (2006-2021, 4 tenors: 1m/3m/6m/12m + nisbah)
/// </summary>
public class DepositoDataService
{
    // conventional[calendarYear][tenorMonths] → annual rate (decimal, e.g. 0.0825)
    private readonly Dictionary<int, Dictionary<int, decimal>> _conventional = new();
    // shariah[calendarYear][tenorMonths] → (rate, nisbah)
    private readonly Dictionary<int, Dictionary<int, (decimal rate, string nisbah)>> _shariah = new();
    // BI Rate by calendar year
    private readonly Dictionary<int, decimal> _biRates = new();

    public DepositoDataService(IWebHostEnvironment env)
    {
        var basePath = Path.Combine(env.ContentRootPath, "..", "Data", "Deposit");
        if (!Directory.Exists(basePath))
            basePath = Path.Combine(env.ContentRootPath, "Data", "Deposit");

        var convPath = Path.Combine(basePath, "05_deposito_bri.json");
        var shariahPath = Path.Combine(basePath, "10_deposito_shariah_muamalat.json");

        if (File.Exists(convPath)) ParseConventional(convPath);
        if (File.Exists(shariahPath)) ParseShariah(shariahPath);
    }

    private void ParseConventional(string path)
    {
        var json = File.ReadAllText(path);
        using var doc = JsonDocument.Parse(json);
        var data = doc.RootElement.GetProperty("data");

        foreach (var entry in data.EnumerateArray())
        {
            var year = entry.GetProperty("year").GetInt32();
            var rates = new Dictionary<int, decimal>();

            rates[1] = entry.GetProperty("tenor_1m").GetDecimal() / 100m;
            rates[3] = entry.GetProperty("tenor_3m").GetDecimal() / 100m;
            rates[6] = entry.GetProperty("tenor_6m").GetDecimal() / 100m;
            rates[12] = entry.GetProperty("tenor_12m").GetDecimal() / 100m;
            rates[24] = entry.GetProperty("tenor_24m").GetDecimal() / 100m;

            _conventional[year] = rates;
            _biRates[year] = entry.GetProperty("bi_rate").GetDecimal() / 100m;
        }
    }

    private void ParseShariah(string path)
    {
        var json = File.ReadAllText(path);
        using var doc = JsonDocument.Parse(json);
        var data = doc.RootElement.GetProperty("data");

        foreach (var prop in data.EnumerateObject())
        {
            if (!int.TryParse(prop.Name, out var year)) continue;
            var entry = prop.Value;
            var rates = new Dictionary<int, (decimal rate, string nisbah)>();

            rates[1] = (entry.GetProperty("1m").GetDecimal() / 100m, entry.GetProperty("nisbah_1m").GetString() ?? "50:50");
            rates[3] = (entry.GetProperty("3m").GetDecimal() / 100m, entry.GetProperty("nisbah_3m").GetString() ?? "50:50");
            rates[6] = (entry.GetProperty("6m").GetDecimal() / 100m, entry.GetProperty("nisbah_6m").GetString() ?? "50:50");
            rates[12] = (entry.GetProperty("12m").GetDecimal() / 100m, entry.GetProperty("nisbah_12m").GetString() ?? "55:45");

            _shariah[year] = rates;
        }
    }

    /// <summary>Get conventional (BRI) deposito rate for a given game year and tenor.</summary>
    public decimal? GetConventionalRate(int gameYear, int tenorMonths)
    {
        var cy = GameConfig.ToCalendarYear(gameYear);
        if (_conventional.TryGetValue(cy, out var rates) && rates.TryGetValue(tenorMonths, out var rate))
            return rate;
        return null;
    }

    /// <summary>Get shariah (Muamalat) equivalent rate for a given game year and tenor.</summary>
    public decimal? GetShariahRate(int gameYear, int tenorMonths)
    {
        var cy = GameConfig.ToCalendarYear(gameYear);
        if (_shariah.TryGetValue(cy, out var rates) && rates.TryGetValue(tenorMonths, out var info))
            return info.rate;
        return null;
    }

    /// <summary>Get shariah nisbah ratio string (e.g. "55:45") for display.</summary>
    public string? GetShariahNisbah(int gameYear, int tenorMonths)
    {
        var cy = GameConfig.ToCalendarYear(gameYear);
        if (_shariah.TryGetValue(cy, out var rates) && rates.TryGetValue(tenorMonths, out var info))
            return info.nisbah;
        return null;
    }

    /// <summary>Get the BI Rate reference for a given game year.</summary>
    public decimal? GetBIRate(int gameYear)
    {
        var cy = GameConfig.ToCalendarYear(gameYear);
        if (_biRates.TryGetValue(cy, out var rate))
            return rate;
        return null;
    }

    public bool HasData() => _conventional.Count > 0;
}
