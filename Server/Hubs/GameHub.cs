using Microsoft.AspNetCore.SignalR;
using InvestmentGame.Server.Services;
using InvestmentGame.Shared.Models;

namespace InvestmentGame.Server.Hubs;

public class GameHub : Hub
{
    private readonly GameEngine _gameEngine;
    private readonly RoomManager _roomManager;
    private readonly ILogger<GameHub> _logger;

    public GameHub(GameEngine gameEngine, RoomManager roomManager, ILogger<GameHub> logger)
    {
        _gameEngine = gameEngine;
        _roomManager = roomManager;
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

        var room = _roomManager.GetRoomByConnection(Context.ConnectionId);
        if (room != null)
        {
            if (room.Status == RoomStatus.InProgress)
            {
                // Mark as disconnected but keep session alive
                _roomManager.MarkDisconnected(Context.ConnectionId);
                var session = _gameEngine.GetSession(Context.ConnectionId);
                if (session != null)
                {
                    session.IsPaused = true;
                }
                await Clients.Group(room.RoomCode).SendAsync("PlayerDisconnected",
                    room.Players.FirstOrDefault(p => p.ConnectionId == Context.ConnectionId)?.PlayerName ?? "Unknown");
            }
            else
            {
                // In lobby - leave the room
                var (roomCode, wasHost, updatedRoom) = _roomManager.LeaveRoom(Context.ConnectionId);
                if (roomCode != null)
                {
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomCode);
                    if (updatedRoom != null)
                    {
                        if (wasHost)
                        {
                            await Clients.Group(roomCode).SendAsync("HostChanged", updatedRoom.HostPlayerName);
                        }
                        await Clients.Group(roomCode).SendAsync("RoomUpdated", updatedRoom);
                    }
                    else
                    {
                        await Clients.Group(roomCode).SendAsync("RoomClosed");
                    }
                }
                _gameEngine.RemoveSession(Context.ConnectionId);
            }
        }
        else
        {
            _gameEngine.RemoveSession(Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    // === SOLO GAME (backward compatible) ===

    public GameState StartGame(string playerName, AgeMode ageMode, Language language = Language.Indonesian)
    {
        _logger.LogInformation("Starting solo game for player: {PlayerName}", playerName);
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

    // === ROOM MANAGEMENT (Multiplayer) ===

    public async Task<RoomInfo> CreateRoom(string playerName, AgeMode ageMode, Language language, int maxPlayers = 4)
    {
        var room = _roomManager.CreateRoom(Context.ConnectionId, playerName, ageMode, language, maxPlayers);
        await Groups.AddToGroupAsync(Context.ConnectionId, room.RoomCode);
        _logger.LogInformation("Room {RoomCode} created by {PlayerName}", room.RoomCode, playerName);
        return room;
    }

    public async Task<RoomInfo?> JoinRoom(string roomCode, string playerName)
    {
        var (room, error) = _roomManager.JoinRoom(roomCode, Context.ConnectionId, playerName);
        if (room == null)
        {
            await Clients.Caller.SendAsync("RoomError", error);
            return null;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, room.RoomCode);
        await Clients.Group(room.RoomCode).SendAsync("RoomUpdated", room);
        await Clients.GroupExcept(room.RoomCode, Context.ConnectionId).SendAsync("PlayerJoined", playerName);
        return room;
    }

    public async Task LeaveRoom()
    {
        var (roomCode, wasHost, updatedRoom) = _roomManager.LeaveRoom(Context.ConnectionId);
        if (roomCode == null) return;

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomCode);

        // Also remove the game session
        _gameEngine.RemoveSession(Context.ConnectionId);

        if (updatedRoom != null)
        {
            var player = updatedRoom.Players.FirstOrDefault(p => p.ConnectionId == Context.ConnectionId);
            await Clients.Group(roomCode).SendAsync("PlayerLeft", player?.PlayerName ?? "Unknown");
            if (wasHost)
            {
                await Clients.Group(roomCode).SendAsync("HostChanged", updatedRoom.HostPlayerName);
            }
            await Clients.Group(roomCode).SendAsync("RoomUpdated", updatedRoom);
        }
        else
        {
            await Clients.Group(roomCode).SendAsync("RoomClosed");
        }

        await Clients.Caller.SendAsync("LeftRoom");
    }

    public async Task SetReady(bool isReady)
    {
        var room = _roomManager.SetReady(Context.ConnectionId, isReady);
        if (room != null)
        {
            await Clients.Group(room.RoomCode).SendAsync("RoomUpdated", room);
        }
    }

    public async Task<bool> StartMultiplayerGame()
    {
        var (success, error) = _roomManager.StartGame(Context.ConnectionId);
        if (!success)
        {
            await Clients.Caller.SendAsync("RoomError", error);
            return false;
        }

        var room = _roomManager.GetRoomByConnection(Context.ConnectionId);
        if (room == null) return false;

        // Create shared market state for the room
        var marketState = _gameEngine.CreateRoomMarketState(room.RoomCode);

        // Create game sessions for all players
        foreach (var player in room.Players)
        {
            var session = _gameEngine.CreateMultiplayerSession(
                player.PlayerName, player.ConnectionId, room.RoomCode, room.AgeMode, room.Language);

            // Send initial game state to each player
            var gameState = session.ToGameState();
            gameState.IsHost = player.IsHost;
            gameState.PlayerSummaries = GetPlayerSummaries(room.RoomCode);
            await Clients.Client(player.ConnectionId).SendAsync("RoomGameStarted", gameState);
        }

        await Clients.Group(room.RoomCode).SendAsync("RoomUpdated", room);
        _logger.LogInformation("Multiplayer game started in room {RoomCode}", room.RoomCode);
        return true;
    }

    public List<LeaderboardEntry> GetLeaderboard()
    {
        var room = _roomManager.GetRoomByConnection(Context.ConnectionId);
        if (room == null) return new List<LeaderboardEntry>();
        return _gameEngine.GetLeaderboard(room.RoomCode);
    }

    private List<PlayerSummary> GetPlayerSummaries(string roomCode)
    {
        var sessions = _gameEngine.GetRoomSessions(roomCode);
        var room = _roomManager.GetRoom(roomCode);

        return sessions.Select(s => new PlayerSummary
        {
            PlayerName = s.PlayerId,
            NetWorth = s.NetWorth,
            IsConnected = room?.Players.FirstOrDefault(p => p.ConnectionId == s.ConnectionId)?.IsConnected ?? true,
            CurrentYear = s.CurrentYear,
            CurrentMonth = s.CurrentMonth
        }).ToList();
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
            var gameState = session.ToGameState();

            // For multiplayer, include player summaries and check for all-finished
            if (session.IsMultiplayer && session.RoomCode != null)
            {
                gameState.PlayerSummaries = GetPlayerSummaries(session.RoomCode);
                var room = _roomManager.GetRoomByConnection(Context.ConnectionId);
                gameState.IsHost = room?.HostConnectionId == Context.ConnectionId;

                // Broadcast leaderboard update at month-end (when MonthProgress resets to 0)
                if (session.MonthProgress == 0)
                {
                    var leaderboard = _gameEngine.GetLeaderboard(session.RoomCode);
                    await Clients.Group(session.RoomCode).SendAsync("LeaderboardUpdated", leaderboard);
                }

                // Check if all players finished
                if (session.IsGameOver && _gameEngine.AreAllRoomPlayersFinished(session.RoomCode))
                {
                    var finalLeaderboard = _gameEngine.GetLeaderboard(session.RoomCode);
                    gameState.AllPlayersFinished = true;
                    gameState.FinalLeaderboard = finalLeaderboard;
                    await Clients.Group(session.RoomCode).SendAsync("AllPlayersFinished", finalLeaderboard);

                    _roomManager.SetRoomFinished(session.RoomCode);
                }
            }

            await Clients.Caller.SendAsync("GameStateUpdated", gameState);
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
