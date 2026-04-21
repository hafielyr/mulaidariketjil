using System.Collections.Concurrent;
using System.Threading.Channels;
using InvestmentGame.Server.Data;
using Microsoft.EntityFrameworkCore;

namespace InvestmentGame.Server.Services;

public class BehaviorLogService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BehaviorLogService> _logger;
    private readonly Channel<BehaviorJob> _channel;
    private readonly ConcurrentDictionary<string, int> _connectionToSessionId = new();
    private Task? _consumer;
    private CancellationTokenSource? _cts;

    public BehaviorLogService(IServiceScopeFactory scopeFactory, ILogger<BehaviorLogService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _channel = Channel.CreateUnbounded<BehaviorJob>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        using (var scope = _scopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BehaviorDbContext>();
            db.Database.EnsureCreated();
        }

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _consumer = Task.Run(() => ConsumeAsync(_cts.Token));
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _channel.Writer.TryComplete();
        _cts?.Cancel();
        if (_consumer != null)
        {
            try { await _consumer; }
            catch (OperationCanceledException) { }
        }
    }

    public void StartSession(string connectionId, string playerName)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<BehaviorDbContext>();
            var session = new Session { PlayerName = playerName };
            db.Sessions.Add(session);
            db.SaveChanges();
            _connectionToSessionId[connectionId] = session.Id;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create behavior session for {Connection}", connectionId);
        }
    }

    public void LogAction(string connectionId, string instrumentKey, string actionType,
        decimal amount, bool isShariah, decimal portfolioAllocationAfter)
    {
        if (!_connectionToSessionId.TryGetValue(connectionId, out var sessionId)) return;
        var action = new PlayerAction
        {
            SessionId = sessionId,
            InstrumentKey = instrumentKey,
            ActionType = actionType,
            Amount = amount,
            IsShariah = isShariah,
            Timestamp = DateTime.UtcNow,
            PortfolioAllocationAfter = portfolioAllocationAfter
        };
        _channel.Writer.TryWrite(BehaviorJob.ForAction(action));
    }

    public void EndSession(string connectionId, decimal totalProfitLoss)
    {
        if (!_connectionToSessionId.TryRemove(connectionId, out var sessionId)) return;
        _channel.Writer.TryWrite(BehaviorJob.ForEndSession(sessionId, totalProfitLoss));
    }

    private async Task ConsumeAsync(CancellationToken ct)
    {
        try
        {
            await foreach (var head in _channel.Reader.ReadAllAsync(ct))
            {
                var batch = new List<BehaviorJob> { head };
                while (batch.Count < 500 && _channel.Reader.TryRead(out var more))
                {
                    batch.Add(more);
                }

                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<BehaviorDbContext>();

                    foreach (var job in batch)
                    {
                        if (job.Action != null)
                        {
                            db.Actions.Add(job.Action);
                        }
                        else if (job.EndSessionId.HasValue)
                        {
                            var s = await db.Sessions.FindAsync(new object[] { job.EndSessionId.Value }, ct);
                            if (s != null)
                            {
                                s.TotalProfitLoss = job.TotalProfitLoss;
                                s.EndedAt = DateTime.UtcNow;
                            }
                        }
                    }

                    await db.SaveChangesAsync(ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to persist behavior batch of {Count}", batch.Count);
                }
            }
        }
        catch (OperationCanceledException) { }
    }
}

internal sealed class BehaviorJob
{
    public PlayerAction? Action { get; init; }
    public int? EndSessionId { get; init; }
    public decimal TotalProfitLoss { get; init; }

    public static BehaviorJob ForAction(PlayerAction action) => new() { Action = action };
    public static BehaviorJob ForEndSession(int sessionId, decimal totalProfitLoss) =>
        new() { EndSessionId = sessionId, TotalProfitLoss = totalProfitLoss };
}
