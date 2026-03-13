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
            bool isHost = room.HostConnectionId == Context.ConnectionId;

            if (room.Status == RoomStatus.InProgress)
            {
                if (isHost)
                {
                    // Host disconnected during game: auto-resume if paused, notify players
                    _gameEngine.ResumeAllInRoom(room.RoomCode);
                    _roomManager.LeaveRoom(Context.ConnectionId);
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, room.RoomCode);
                    await Clients.Group(room.RoomCode).SendAsync("HostLeft");
                }
                else
                {
                    // Player disconnected: mark as disconnected but keep session alive
                    _roomManager.MarkDisconnected(Context.ConnectionId);
                    var session = _gameEngine.GetSession(Context.ConnectionId);
                    if (session != null)
                        session.IsPaused = true;
                    await Clients.Group(room.RoomCode).SendAsync("PlayerDisconnected",
                        room.Players.FirstOrDefault(p => p.ConnectionId == Context.ConnectionId)?.PlayerName ?? "Unknown");
                }
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
                            await Clients.Group(roomCode).SendAsync("HostLeft");
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

    public async Task<RoomInfo> CreateRoom(string hostName, AgeMode ageMode, Language language)
    {
        var room = _roomManager.CreateRoom(Context.ConnectionId, hostName, ageMode, language);
        await Groups.AddToGroupAsync(Context.ConnectionId, room.RoomCode);
        _logger.LogInformation("Room {RoomCode} created by host {HostName}", room.RoomCode, hostName);
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
        _gameEngine.RemoveSession(Context.ConnectionId);

        if (updatedRoom != null)
        {
            if (wasHost)
                await Clients.Group(roomCode).SendAsync("HostLeft");
            else
                await Clients.Group(roomCode).SendAsync("PlayerLeft",
                    updatedRoom.Players.FirstOrDefault(p => p.ConnectionId == Context.ConnectionId)?.PlayerName ?? "Unknown");
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
        _gameEngine.CreateRoomMarketState(room.RoomCode);

        // Create game sessions for all PLAYERS (not host)
        foreach (var player in room.Players)
        {
            var session = _gameEngine.CreateMultiplayerSession(
                player.PlayerName, player.ConnectionId, room.RoomCode, room.AgeMode, room.Language);

            var gameState = session.ToGameState();
            gameState.PlayerSummaries = GetPlayerSummaries(room.RoomCode);
            await Clients.Client(player.ConnectionId).SendAsync("RoomGameStarted", gameState);
        }

        // Send host to dashboard (no GameSession for host)
        await Clients.Client(room.HostConnectionId).SendAsync("HostDashboardStarted", room);

        // Broadcast initial Tabungan unlock via MP sync popup (sessions start paused)
        _roomManager.ResetUnlockReady(room.RoomCode);
        await Clients.Group(room.RoomCode).SendAsync("AssetUnlocked", "tabungan");
        await Clients.Client(room.HostConnectionId).SendAsync("RoomUpdated", room);

        await Clients.Group(room.RoomCode).SendAsync("RoomUpdated", room);
        _logger.LogInformation("Multiplayer game started in room {RoomCode}", room.RoomCode);
        return true;
    }

    // === HOST DASHBOARD METHODS (host only) ===

    public RoomInfo? GetRoomStatus()
    {
        return _roomManager.GetRoomByConnection(Context.ConnectionId);
    }

    public List<PlayerSummary> GetAllPlayerPortfolios()
    {
        var room = _roomManager.GetRoomByConnection(Context.ConnectionId);
        if (room == null) return new List<PlayerSummary>();
        return _gameEngine.GetAllPlayerPortfolios(room.RoomCode);
    }

    public async Task PauseAllPlayers()
    {
        var room = _roomManager.GetRoomByConnection(Context.ConnectionId);
        if (room == null || room.HostConnectionId != Context.ConnectionId) return;

        _gameEngine.PauseAllInRoom(room.RoomCode);
        await Clients.Group(room.RoomCode).SendAsync("AllPaused");
    }

    public async Task ResumeAllPlayers()
    {
        var room = _roomManager.GetRoomByConnection(Context.ConnectionId);
        if (room == null || room.HostConnectionId != Context.ConnectionId) return;

        _gameEngine.ResumeAllInRoom(room.RoomCode);
        await Clients.Group(room.RoomCode).SendAsync("AllResumed");
    }

    // === UNLOCK SYNC ===

    public async Task SetUnlockReady()
    {
        var room = _roomManager.GetRoomByConnection(Context.ConnectionId);
        if (room == null) return;

        var allReady = _roomManager.SetUnlockReady(Context.ConnectionId);

        // Notify host of updated ready state
        await Clients.Client(room.HostConnectionId).SendAsync("RoomUpdated", room);

        if (allReady)
        {
            // All players dismissed their intro popups — resume all sessions
            _gameEngine.ResumeAllInRoom(room.RoomCode);
            _roomManager.ResetUnlockReady(room.RoomCode);
            // Send updated room (all IsUnlockReady=false) to host so dashboard resets
            await Clients.Client(room.HostConnectionId).SendAsync("RoomUpdated", room);
            await Clients.Group(room.RoomCode).SendAsync("UnlockComplete");
        }
    }

    public async Task HostForceResume()
    {
        var room = _roomManager.GetRoomByConnection(Context.ConnectionId);
        if (room == null || room.HostConnectionId != Context.ConnectionId) return;

        _gameEngine.ResumeAllInRoom(room.RoomCode);
        _roomManager.ResetUnlockReady(room.RoomCode);
        // Send updated room (all IsUnlockReady=false) to host so dashboard resets
        await Clients.Client(room.HostConnectionId).SendAsync("RoomUpdated", room);
        await Clients.Group(room.RoomCode).SendAsync("UnlockComplete");
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
            ConnectionId = s.ConnectionId,
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
                await Clients.Caller.SendAsync("GameStateUpdated", session.ToGameState());
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
                await Clients.Caller.SendAsync("GameStateUpdated", session.ToGameState());
        }
        return result;
    }

    // === CERTIFICATE OF DEPOSIT ===
    public async Task<bool> BuyDeposito(int periodMonths, decimal amount, bool autoRollOver = false, bool isShariah = false)
    {
        var result = _gameEngine.BuyDeposito(Context.ConnectionId, periodMonths, amount, autoRollOver, isShariah);
        if (result)
        {
            var session = _gameEngine.GetSession(Context.ConnectionId);
            if (session != null)
                await Clients.Caller.SendAsync("GameStateUpdated", session.ToGameState());
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
                await Clients.Caller.SendAsync("GameStateUpdated", session.ToGameState());
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
                await Clients.Caller.SendAsync("GameStateUpdated", session.ToGameState());
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
                await Clients.Caller.SendAsync("GameStateUpdated", session.ToGameState());
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
                await Clients.Caller.SendAsync("GameStateUpdated", session.ToGameState());
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
                await Clients.Caller.SendAsync("GameStateUpdated", session.ToGameState());
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
                await Clients.Caller.SendAsync("GameStateUpdated", session.ToGameState());
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
                await Clients.Caller.SendAsync("GameStateUpdated", session.ToGameState());
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
                await Clients.Caller.SendAsync("GameStateUpdated", session.ToGameState());
        }
        return result;
    }

    // === INDEX FUNDS ===
    public async Task<bool> BuyIndex(string indexId, decimal amount)
    {
        var result = _gameEngine.BuyIndex(Context.ConnectionId, indexId, amount);
        if (result)
        {
            var session = _gameEngine.GetSession(Context.ConnectionId);
            if (session != null)
                await Clients.Caller.SendAsync("GameStateUpdated", session.ToGameState());
        }
        return result;
    }

    public async Task<bool> SellIndex(string indexId, decimal units)
    {
        var result = _gameEngine.SellIndex(Context.ConnectionId, indexId, units);
        if (result)
        {
            var session = _gameEngine.GetSession(Context.ConnectionId);
            if (session != null)
                await Clients.Caller.SendAsync("GameStateUpdated", session.ToGameState());
        }
        return result;
    }

    public async Task<bool> SellAllIndex(string indexId)
    {
        var result = _gameEngine.SellAllIndex(Context.ConnectionId, indexId);
        if (result)
        {
            var session = _gameEngine.GetSession(Context.ConnectionId);
            if (session != null)
                await Clients.Caller.SendAsync("GameStateUpdated", session.ToGameState());
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
                await Clients.Caller.SendAsync("GameStateUpdated", session.ToGameState());
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
                await Clients.Caller.SendAsync("GameStateUpdated", session.ToGameState());
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
                await Clients.Caller.SendAsync("GameStateUpdated", session.ToGameState());
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
                await Clients.Caller.SendAsync("GameStateUpdated", session.ToGameState());
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
                await Clients.Caller.SendAsync("GameStateUpdated", session.ToGameState());
        }
        return result;
    }

    // === EVENT HANDLING ===
    public async Task<bool> PayEventFromCash()
    {
        var result = _gameEngine.PayEventFromCash(Context.ConnectionId);
        var session = _gameEngine.GetSession(Context.ConnectionId);
        if (session != null)
            await Clients.Caller.SendAsync("GameStateUpdated", session.ToGameState());
        return result;
    }

    public async Task<bool> PayEventFromSavings()
    {
        var result = _gameEngine.PayEventFromSavings(Context.ConnectionId);
        var session = _gameEngine.GetSession(Context.ConnectionId);
        if (session != null)
            await Clients.Caller.SendAsync("GameStateUpdated", session.ToGameState());
        return result;
    }

    public async Task<bool> PayEventFromPortfolio(string assetType)
    {
        var result = _gameEngine.PayEventFromPortfolio(Context.ConnectionId, assetType);
        var session = _gameEngine.GetSession(Context.ConnectionId);
        if (session != null)
            await Clients.Caller.SendAsync("GameStateUpdated", session.ToGameState());
        return result;
    }

    // === GAME CONTROL ===
    public async Task ProcessTick()
    {
        var (unlockOccurred, unlockAssetType, _) = _gameEngine.ProcessTick(Context.ConnectionId);
        var session = _gameEngine.GetSession(Context.ConnectionId);
        if (session != null)
        {
            // Check auto-pay for multiplayer events
            if (session.IsMultiplayer && session.IsEventPending)
            {
                var autoPaid = _gameEngine.CheckAndAutoPayEvent(Context.ConnectionId);
                // Re-fetch session state after potential auto-pay
                if (autoPaid)
                {
                    // Broadcast updated state since event was resolved
                }
            }

            var gameState = session.ToGameState();

            // For multiplayer, include player summaries and check for all-finished
            if (session.IsMultiplayer && session.RoomCode != null)
            {
                gameState.PlayerSummaries = GetPlayerSummaries(session.RoomCode);
                var room = _roomManager.GetRoomByConnection(Context.ConnectionId);

                // Broadcast leaderboard update at month-end (when MonthProgress resets to 0)
                if (session.MonthProgress == 0)
                {
                    var leaderboard = _gameEngine.GetLeaderboard(session.RoomCode);
                    await Clients.Group(session.RoomCode).SendAsync("LeaderboardUpdated", leaderboard);
                }

                // Broadcast asset unlock to all players in room
                if (unlockOccurred && unlockAssetType != null)
                {
                    _roomManager.ResetUnlockReady(session.RoomCode);
                    await Clients.Group(session.RoomCode).SendAsync("AssetUnlocked", unlockAssetType);
                    // Refresh host dashboard with current room state (all IsUnlockReady=false)
                    if (room != null && !string.IsNullOrEmpty(room.HostConnectionId))
                        await Clients.Client(room.HostConnectionId).SendAsync("RoomUpdated", room);
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
            await Clients.Caller.SendAsync("GamePaused");
    }

    public async Task ResumeGame()
    {
        _gameEngine.ResumeGame(Context.ConnectionId);
        var session = _gameEngine.GetSession(Context.ConnectionId);
        if (session != null)
            await Clients.Caller.SendAsync("GameResumed");
    }

    public async Task RestartGame()
    {
        _gameEngine.RestartGame(Context.ConnectionId);
        var session = _gameEngine.GetSession(Context.ConnectionId);
        if (session != null)
            await Clients.Caller.SendAsync("GameStateUpdated", session.ToGameState());
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
