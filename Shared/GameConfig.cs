namespace InvestmentGame.Shared;

/// <summary>
/// Central game configuration constants.
/// Game Year 1 corresponds to calendar year 2006 in the historical data (Data/Stocks/).
/// </summary>
public static class GameConfig
{
    /// <summary>
    /// The calendar year that Game Year 1 corresponds to.
    /// CSV data in Data/Stocks/ uses real calendar years starting from this year.
    /// </summary>
    public const int BaseCalendarYear = 2006;

    /// <summary>
    /// Convert a game year to the corresponding calendar year.
    /// Example: gameYear 1 → 2006, gameYear 4 → 2009.
    /// </summary>
    public static int ToCalendarYear(int gameYear) => BaseCalendarYear + gameYear - 1;

    /// <summary>
    /// Cash the player (and the bot) starts Game Year 1 with.
    /// </summary>
    public const decimal StartingCapital = 20_000_000m;

    /// <summary>
    /// Annual salary earned in Game Year 1, before any raise.
    /// </summary>
    public const decimal BaseYearlyIncome = 12_000_000m;

    /// <summary>
    /// Compounding raise applied to the salary every game year (10% per year).
    /// </summary>
    public const decimal AnnualRaiseRate = 0.10m;

    /// <summary>
    /// Salary earned in a given game year: BaseYearlyIncome * (1 + AnnualRaiseRate)^(gameYear - 1).
    /// Example: year 1 -> 12,000,000; year 2 -> 13,200,000; year 3 -> 14,520,000.
    /// </summary>
    public static decimal GetYearlySalary(int gameYear)
    {
        if (gameYear < 1) return 0m;

        var salary = BaseYearlyIncome;
        for (var i = 1; i < gameYear; i++)
        {
            salary *= 1m + AnnualRaiseRate;
        }
        return Math.Round(salary, 0);
    }

    /// <summary>
    /// Total salary earned across game years 1..throughYear (inclusive).
    /// Used as the "money put in" baseline for profit calculations.
    /// </summary>
    public static decimal GetCumulativeSalary(int throughYear)
    {
        var total = 0m;
        for (var year = 1; year <= throughYear; year++)
        {
            total += GetYearlySalary(year);
        }
        return total;
    }
}
