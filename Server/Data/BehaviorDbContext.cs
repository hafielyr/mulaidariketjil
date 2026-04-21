using Microsoft.EntityFrameworkCore;

namespace InvestmentGame.Server.Data;

public class BehaviorDbContext : DbContext
{
    public BehaviorDbContext(DbContextOptions<BehaviorDbContext> options) : base(options) { }

    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<PlayerAction> Actions => Set<PlayerAction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Session>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.PlayerName).HasMaxLength(100);
            e.Property(s => s.TotalProfitLoss).HasColumnType("TEXT"); // SQLite stores decimals as text for precision
        });

        modelBuilder.Entity<PlayerAction>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.InstrumentKey).HasMaxLength(50);
            e.Property(a => a.ActionType).HasMaxLength(10);
            e.Property(a => a.Amount).HasColumnType("TEXT");
            e.Property(a => a.PortfolioAllocationAfter).HasColumnType("TEXT");
            e.HasIndex(a => a.SessionId);
            e.HasIndex(a => a.Timestamp);
        });
    }
}
