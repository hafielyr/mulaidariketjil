using Microsoft.AspNetCore.SignalR;
using InvestmentGame.Server.Services;
using InvestmentGame.Shared.Models;

namespace InvestmentGame.Server.Hubs;

public class GameHub : Hub
{
    private readonly GameEngine _gameEngine;
    private readonly ILogger<GameHub> _logger;

    public GameHub(GameEngine gameEngine, ILogger<GameHub> logger)
    {
        _gameEngine = gameEngine;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
        _gameEngine.RemoveSession(Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    public GameState StartGame(string playerName, AgeMode ageMode, Language language = Language.Indonesian)
    {
        _logger.LogInformation("Starting game for player: {PlayerName}, AgeMode: {AgeMode}, Language: {Language}", playerName, ageMode, language);
        var session = _gameEngine.CreateSession(playerName, Context.ConnectionId, ageMode, language);
        return session.ToGameState();
    }

    public GameState? GetGameState()
    {
        var session = _gameEngine.GetSession(Context.ConnectionId);
        return session?.ToGameState();
    }

    public async Task DismissIntro()
    {
        _gameEngine.DismissIntro(Context.ConnectionId);
        var session = _gameEngine.GetSession(Context.ConnectionId);
        if (session != null)
        {
            await Clients.Caller.SendAsync("GameStateUpdated", session.ToGameState());
        }
    }

    // === SAVINGS ACCOUNT ===
    public async Task<bool> DepositToSavings(decimal amount)
    {
        var result = _gameEngine.DepositToSavings(Context.ConnectionId, amount);
        if (result)
        {
            var session = _gameEngine.GetSession(Context.ConnectionId);
            if (session != null)
            {
                await Clients.Caller.SendAsync("GameStateUpdated", session.ToGameState());
            }
        }
        return result;
    }

    public async Task<bool> WithdrawFromSavings(decimal amount)
    {
        var result = _gameEngine.WithdrawFromSavings(Context.ConnectionId, amount);
        if (result)
        {
            var session = _gameEngine.GetSession(Context.ConnectionId);
            if (session != null)
            {
                await Clients.Caller.SendAsync("GameStateUpdated", session.ToGameState());
            }
        }
        return result;
    }

    // === CERTIFICATE OF DEPOSIT ===
    public async Task<bool> BuyDeposito(int periodMonths, decimal amount, bool autoRollOver = false)
    {
        var result = _gameEngine.BuyDeposito(Context.ConnectionId, periodMonths, amount, autoRollOver);
        if (result)
        {
            var session = _gameEngine.GetSession(Context.ConnectionId);
            if (session != null)
            {
                await Clients.Caller.SendAsync("GameStateUpdated", session.ToGameState());
            }
        }
        return result;
    }

    public async Task<bool> WithdrawDeposito(string depositoId, bool earlyWithdraw)
    {
        var result = _gameEngine.WithdrawDeposito(Context.ConnectionId, depositoId, earlyWithdraw);
        if (result)
        {
            var session = _gameEngine.GetSession(Context.ConnectionId);
            if (session != null)
            {
                await Clients.Caller.SendAsync("GameStateUpdated", session.ToGameState());
            }
        }
        return result;
    }

    public async Task<bool> ToggleDepositoAutoRollOver(string depositoId)
    {
        var result = _gameEngine.ToggleDepositoAutoRollOver(Context.ConnectionId, depositoId);
        if (result)
        {
            var session = _gameEngine.GetSession(Context.ConnectionId);
            if (session != null)
            {
                await Clients.Caller.SendAsync("GameStateUpdated", session.ToGameState());
            }
        }
        return result;
    }

    // === GOVERNMENT BONDS ===
    public async Task<bool> BuyBond(string bondType, decimal amount)
    {
        var result = _gameEngine.BuyBond(Context.ConnectionId, bondType, amount);
        if (result)
        {
            var session = _gameEngine.GetSession(Context.ConnectionId);
            if (session != null)
            {
                await Clients.Caller.SendAsync("GameStateUpdated", session.ToGameState());
            }
        }
        return result;
    }

    public async Task<bool> WithdrawBond(string bondId)
    {
        var result = _gameEngine.WithdrawBond(Context.ConnectionId, bondId);
        if (result)
        {
            var session = _gameEngine.GetSession(Context.ConnectionId);
            if (session != null)
            {
                await Clients.Caller.SendAsync("GameStateUpdated", session.ToGameState());
            }
        }
        return result;
    }

    // === INDIVIDUAL STOCKS ===
    public async Task<bool> BuyStock(string ticker, int lots)
    {
        var result = _gameEngine.BuyStock(Context.ConnectionId, ticker, lots);
        if (result)
        {
            var session = _gameEngine.GetSession(Context.ConnectionId);
            if (session != null)
            {
                await Clients.Caller.SendAsync("GameStateUpdated", session.ToGameState());
            }
        }
        return result;
    }

    public async Task<bool> SellStock(string ticker, int lots)
    {
        var result = _gameEngine.SellStock(Context.ConnectionId, ticker, lots);
        if (result)
        {
            var session = _gameEngine.GetSession(Context.ConnectionId);
            if (session != null)
            {
                await Clients.Caller.SendAsync("GameStateUpdated", session.ToGameState());
            }
        }
        return result;
    }

    // === CRYPTO ===
    public async Task<bool> BuyCrypto(string symbol, decimal amount)
    {
        var result = _gameEngine.BuyCrypto(Context.ConnectionId, symbol, amount);
        if (result)
        {
            var session = _gameEngine.GetSession(Context.ConnectionId);
            if (session != null)
            {
                await Clients.Caller.SendAsync("GameStateUpdated", session.ToGameState());
            }
        }
        return result;
    }

    public async Task<bool> SellCrypto(string symbol, decimal amount)
    {
        var result = _gameEngine.SellCrypto(Context.ConnectionId, symbol, amount);
        if (result)
        {
            var session = _gameEngine.GetSession(Context.ConnectionId);
            if (session != null)
            {
                await Clients.Caller.SendAsync("GameStateUpdated", session.ToGameState());
            }
        }
        return result;
    }

    // === GENERAL ASSETS (Index Fund, Gold, Crowdfunding) ===
    public async Task<bool> BuyAsset(string assetType)
    {
        var result = _gameEngine.BuyAsset(Context.ConnectionId, assetType);
        if (result)
        {
            var session = _gameEngine.GetSession(Context.ConnectionId);
            if (session != null)
            {
                await Clients.Caller.SendAsync("GameStateUpdated", session.ToGameState());
            }
        }
        return result;
    }

    public async Task<bool> BuyGoldByGrams(decimal grams)
    {
        var result = _gameEngine.BuyGoldByGrams(Context.ConnectionId, grams);
        if (result)
        {
            var session = _gameEngine.GetSession(Context.ConnectionId);
            if (session != null)
            {
                await Clients.Caller.SendAsync("GameStateUpdated", session.ToGameState());
            }
        }
        return result;
    }

    public async Task<bool> SellAsset(string assetType)
    {
        var result = _gameEngine.SellAsset(Context.ConnectionId, assetType);
        if (result)
        {
            var session = _gameEngine.GetSession(Context.ConnectionId);
            if (session != null)
            {
                await Clients.Caller.SendAsync("GameStateUpdated", session.ToGameState());
            }
        }
        return result;
    }

    public async Task<bool> SellAllAssets(string assetType)
    {
        var result = _gameEngine.SellAllAssets(Context.ConnectionId, assetType);
        if (result)
        {
            var session = _gameEngine.GetSession(Context.ConnectionId);
            if (session != null)
            {
                await Clients.Caller.SendAsync("GameStateUpdated", session.ToGameState());
            }
        }
        return result;
    }

    // === CROWDFUNDING ===
    public async Task<bool> BuyCrowdfunding(string projectId, decimal amount)
    {
        var result = _gameEngine.BuyCrowdfunding(Context.ConnectionId, projectId, amount);
        if (result)
        {
            var session = _gameEngine.GetSession(Context.ConnectionId);
            if (session != null)
            {
                await Clients.Caller.SendAsync("GameStateUpdated", session.ToGameState());
            }
        }
        return result;
    }

    // === EVENT HANDLING ===
    public async Task<bool> PayEventFromCash()
    {
        var result = _gameEngine.PayEventFromCash(Context.ConnectionId);
        var session = _gameEngine.GetSession(Context.ConnectionId);
        if (session != null)
        {
            await Clients.Caller.SendAsync("GameStateUpdated", session.ToGameState());
        }
        return result;
    }

    public async Task<bool> PayEventFromSavings()
    {
        var result = _gameEngine.PayEventFromSavings(Context.ConnectionId);
        var session = _gameEngine.GetSession(Context.ConnectionId);
        if (session != null)
        {
            await Clients.Caller.SendAsync("GameStateUpdated", session.ToGameState());
        }
        return result;
    }

    public async Task<bool> PayEventFromPortfolio(string assetType)
    {
        var result = _gameEngine.PayEventFromPortfolio(Context.ConnectionId, assetType);
        var session = _gameEngine.GetSession(Context.ConnectionId);
        if (session != null)
        {
            await Clients.Caller.SendAsync("GameStateUpdated", session.ToGameState());
        }
        return result;
    }

    // === GAME CONTROL ===
    public async Task ProcessTick()
    {
        _gameEngine.ProcessTick(Context.ConnectionId);
        var session = _gameEngine.GetSession(Context.ConnectionId);
        if (session != null)
        {
            await Clients.Caller.SendAsync("GameStateUpdated", session.ToGameState());
        }
    }

    public async Task PauseGame()
    {
        _gameEngine.PauseGame(Context.ConnectionId);
        var session = _gameEngine.GetSession(Context.ConnectionId);
        if (session != null)
        {
            await Clients.Caller.SendAsync("GamePaused");
        }
    }

    public async Task ResumeGame()
    {
        _gameEngine.ResumeGame(Context.ConnectionId);
        var session = _gameEngine.GetSession(Context.ConnectionId);
        if (session != null)
        {
            await Clients.Caller.SendAsync("GameResumed");
        }
    }

    public async Task RestartGame()
    {
        _gameEngine.RestartGame(Context.ConnectionId);
        var session = _gameEngine.GetSession(Context.ConnectionId);
        if (session != null)
        {
            await Clients.Caller.SendAsync("GameStateUpdated", session.ToGameState());
        }
    }

    // === DATA RETRIEVAL ===
    public Dictionary<string, AssetDefinition> GetAssetDefinitions()
    {
        return _gameEngine.GetAssets();
    }

    public List<DepositoRate> GetDepositoRates()
    {
        return _gameEngine.GetDepositoRates();
    }

    public List<BondRate> GetBondRates()
    {
        return _gameEngine.GetBondRates();
    }
}
