using System.Text.Json;
using InvestmentGame.Shared;

namespace InvestmentGame.Server.Services;

/// <summary>
/// Singleton service that loads real historical bond data from JSON at startup.
/// ORI (conventional): Obligasi Ritel Indonesia, 2006-2021
/// SR (shariah): Sukuk Ritel, 2009-2021 (akad Ijarah)
/// </summary>
public class BondDataService
{
    // calendarYear → (series, couponRate as decimal, tenorYears)
    private readonly Dictionary<int, (string series, decimal couponRate, int tenorYears)> _oriByYear = new();
    private readonly Dictionary<int, (string series, decimal couponRate, int tenorYears, string akad)> _srByYear = new();

    public BondDataService(IWebHostEnvironment env)
    {
        var basePath = Path.Combine(env.ContentRootPath, "..", "Data", "Bonds");
        if (!Directory.Exists(basePath))
            basePath = Path.Combine(env.ContentRootPath, "Data", "Bonds");

        var oriPath = Path.Combine(basePath, "06_ori_bonds.json");
        var sbsnPath = Path.Combine(basePath, "11_sbsn_shariah_bonds.json");

        if (File.Exists(oriPath)) ParseORI(oriPath);
        if (File.Exists(sbsnPath)) ParseSBSN(sbsnPath);
    }

    private void ParseORI(string path)
    {
        var json = File.ReadAllText(path);
        using var doc = JsonDocument.Parse(json);
        var data = doc.RootElement.GetProperty("data");

        // Group by year, pick the latest series issued that year
        var byYear = new Dictionary<int, (string series, decimal couponRate, int tenorYears)>();
        foreach (var entry in data.EnumerateArray())
        {
            var year = entry.GetProperty("year_issued").GetInt32();
            var series = entry.GetProperty("series").GetString() ?? "";
            var coupon = entry.GetProperty("coupon_rate").GetDecimal() / 100m;
            var tenor = entry.GetProperty("tenor_years").GetInt32();

            // Overwrite so we keep the latest series for each year
            byYear[year] = (series, coupon, tenor);
        }

        foreach (var kv in byYear)
            _oriByYear[kv.Key] = kv.Value;
    }

    private void ParseSBSN(string path)
    {
        var json = File.ReadAllText(path);
        using var doc = JsonDocument.Parse(json);
        var allSeries = doc.RootElement.GetProperty("all_series");

        // Pick latest SR (Sukuk Ritel) series per year
        var byYear = new Dictionary<int, (string series, decimal couponRate, int tenorYears, string akad)>();
        foreach (var entry in allSeries.EnumerateArray())
        {
            var type = entry.GetProperty("type").GetString() ?? "";
            if (type != "Sukuk Ritel") continue; // Only SR, not ST

            var year = entry.GetProperty("year").GetInt32();
            var series = entry.GetProperty("series").GetString() ?? "";
            var coupon = entry.GetProperty("coupon_pct").GetDecimal() / 100m;
            var tenor = entry.GetProperty("tenor_years").GetInt32();
            var akad = entry.GetProperty("akad").GetString() ?? "Ijarah";

            // Overwrite so we keep the latest SR for each year
            byYear[year] = (series, coupon, tenor, akad);
        }

        foreach (var kv in byYear)
            _srByYear[kv.Key] = kv.Value;
    }

    /// <summary>Get ORI (conventional) bond data for a given game year.</summary>
    public (string series, decimal couponRate, int tenorYears)? GetORI(int gameYear)
    {
        var cy = GameConfig.ToCalendarYear(gameYear);
        if (_oriByYear.TryGetValue(cy, out var data))
            return data;

        // Fallback: find most recent year before this one
        for (int fallback = cy - 1; fallback >= 2006; fallback--)
        {
            if (_oriByYear.TryGetValue(fallback, out var prev))
                return prev;
        }
        return null;
    }

    /// <summary>Get SR (shariah Sukuk Ritel) bond data for a given game year. Returns null before 2009.</summary>
    public (string series, decimal couponRate, int tenorYears, string akad)? GetSR(int gameYear)
    {
        var cy = GameConfig.ToCalendarYear(gameYear);
        // SR001 was first issued in 2009 (game year 4)
        if (cy < 2009) return null;

        if (_srByYear.TryGetValue(cy, out var data))
            return data;

        // Fallback: find most recent year before this one
        for (int fallback = cy - 1; fallback >= 2009; fallback--)
        {
            if (_srByYear.TryGetValue(fallback, out var prev))
                return prev;
        }
        return null;
    }

    public bool HasData() => _oriByYear.Count > 0;
}
