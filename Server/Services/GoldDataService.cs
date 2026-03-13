using InvestmentGame.Shared;

namespace InvestmentGame.Server.Services;

/// <summary>
/// Singleton service that loads real historical Antam gold prices from CSV at startup.
/// Data format: Year,Jan,Feb,...,Dec with prices in Rp/gram.
/// Uses GameConfig.ToCalendarYear() for year mapping.
/// </summary>
public class GoldDataService
{
    // (calendarYear, month) → price per gram
    private readonly Dictionary<(int year, int month), decimal> _prices = new();

    public GoldDataService(IWebHostEnvironment env)
    {
        var csvPath = Path.Combine(env.ContentRootPath, "..", "Data", "Gold", "04_gold_antam_monthly.csv");
        if (!File.Exists(csvPath))
        {
            csvPath = Path.Combine(env.ContentRootPath, "Data", "Gold", "04_gold_antam_monthly.csv");
        }

        if (File.Exists(csvPath))
        {
            ParseCsv(csvPath);
        }
    }

    private void ParseCsv(string path)
    {
        var lines = File.ReadAllLines(path);
        if (lines.Length < 2) return;

        // Header: Year,Jan,Feb,Mar,Apr,May,Jun,Jul,Aug,Sep,Oct,Nov,Dec
        for (int row = 1; row < lines.Length; row++)
        {
            var cols = lines[row].Split(',');
            if (cols.Length < 13) continue;

            if (!int.TryParse(cols[0].Trim(), out var year)) continue;

            for (int month = 1; month <= 12; month++)
            {
                var val = cols[month].Trim();
                if (string.IsNullOrEmpty(val)) continue;
                if (!decimal.TryParse(val, out var price)) continue;

                _prices[(year, month)] = price;
            }
        }
    }

    /// <summary>
    /// Get the gold price per gram for a given game year and month.
    /// </summary>
    public decimal? GetPrice(int gameYear, int gameMonth)
    {
        var calendarYear = GameConfig.ToCalendarYear(gameYear);
        if (_prices.TryGetValue((calendarYear, gameMonth), out var price))
            return price;
        return null;
    }

    /// <summary>
    /// Get price history for mini chart. Returns last (monthsBack+1) prices ending at current month.
    /// </summary>
    public List<decimal> GetPriceHistory(int gameYear, int gameMonth, int monthsBack = 6)
    {
        var calendarYear = GameConfig.ToCalendarYear(gameYear);
        var cy = calendarYear;
        var cm = gameMonth;

        var prices = new List<(int y, int m, decimal p)>();
        for (int i = 0; i <= monthsBack; i++)
        {
            if (_prices.TryGetValue((cy, cm), out var price))
                prices.Add((cy, cm, price));

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

    public bool HasData() => _prices.Count > 0;
}
