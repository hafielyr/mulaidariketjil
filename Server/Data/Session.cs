namespace InvestmentGame.Server.Data;

public class Session
{
    public int Id { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    public decimal TotalProfitLoss { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }

    public List<PlayerAction> Actions { get; set; } = new();
}
