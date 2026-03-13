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
}
