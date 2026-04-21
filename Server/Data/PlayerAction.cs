namespace InvestmentGame.Server.Data;

public class PlayerAction
{
    public int Id { get; set; }
    public int SessionId { get; set; }
    public string InstrumentKey { get; set; } = string.Empty;
    public string ActionType { get; set; } = string.Empty; // "BUY" or "SELL"
    public decimal Amount { get; set; }
    public bool IsShariah { get; set; }
    public DateTime Timestamp { get; set; }
    public decimal PortfolioAllocationAfter { get; set; }
}
