namespace InvestmentGame.Server.Services;

/// <summary>
/// Feature flag for simulating taxes on investment returns.
/// Enabled via appsettings.json ("InvestmentGame:TaxEnabled") or the
/// INVESTMENTGAME_TAX_ENABLED environment variable.
/// </summary>
public class TaxSettings
{
    public bool Enabled { get; set; }
}
