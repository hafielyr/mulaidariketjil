using Microsoft.AspNetCore.SignalR.Client;
using InvestmentGame.Shared.Models;

namespace InvestmentGame.Client.Services;

public class GameClient : IAsyncDisposable
{
    private HubConnection? _hubConnection;
    private System.Timers.Timer? _tickTimer;
    private bool _isRunning;

    public event Action<GameState>? OnGameStateUpdated;
    public event Action? OnGamePaused;
    public event Action? OnGameResumed;
    public event Action<string>? OnError;

    // Multiplayer events
    public event Action<RoomInfo>? OnRoomUpdated;
    public event Action<GameState>? OnRoomGameStarted;
    public event Action<string>? OnPlayerJoined;
    public event Action<string>? OnPlayerLeft;
    public event Action<List<LeaderboardEntry>>? OnLeaderboardUpdated;
    public event Action<List<LeaderboardEntry>>? OnAllPlayersFinished;
    public event Action<string>? OnHostChanged;
    public event Action? OnRoomClosed;
    public event Action<string>? OnRoomError;
    public event Action? OnLeftRoom;
    public event Action<string>? OnPlayerDisconnected;
    public event Action<string>? OnPlayerReconnected;

    public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;
    public bool IsRunning => _isRunning;

    public GameClient(IConfiguration configuration)
    {
    }

    public async Task ConnectAsync(string baseUri)
    {
        if (_hubConnection != null)
        {
            await _hubConnection.DisposeAsync();
        }

        _hubConnection = new HubConnectionBuilder()
            .WithUrl($"{baseUri}gamehub")
            .WithAutomaticReconnect()
            .Build();

        // Solo game callbacks
        _hubConnection.On<GameState>("GameStateUpdated", state =>
        {
            OnGameStateUpdated?.Invoke(state);
        });

        _hubConnection.On("GamePaused", () =>
        {
            _isRunning = false;
            StopTickTimer();
            OnGamePaused?.Invoke();
        });

        _hubConnection.On("GameResumed", () =>
        {
            _isRunning = true;
            StartTickTimer();
            OnGameResumed?.Invoke();
        });

        // Multiplayer callbacks
        _hubConnection.On<RoomInfo>("RoomUpdated", room =>
        {
            OnRoomUpdated?.Invoke(room);
        });

        _hubConnection.On<GameState>("RoomGameStarted", state =>
        {
            _isRunning = true;
            StartTickTimer();
            OnRoomGameStarted?.Invoke(state);
        });

        _hubConnection.On<string>("PlayerJoined", name =>
        {
            OnPlayerJoined?.Invoke(name);
        });

        _hubConnection.On<string>("PlayerLeft", name =>
        {
            OnPlayerLeft?.Invoke(name);
        });

        _hubConnection.On<List<LeaderboardEntry>>("LeaderboardUpdated", entries =>
        {
            OnLeaderboardUpdated?.Invoke(entries);
        });

        _hubConnection.On<List<LeaderboardEntry>>("AllPlayersFinished", entries =>
        {
            OnAllPlayersFinished?.Invoke(entries);
        });

        _hubConnection.On<string>("HostChanged", newHost =>
        {
            OnHostChanged?.Invoke(newHost);
        });

        _hubConnection.On("RoomClosed", () =>
        {
            _isRunning = false;
            StopTickTimer();
            OnRoomClosed?.Invoke();
        });

        _hubConnection.On<string>("RoomError", error =>
        {
            OnRoomError?.Invoke(error);
        });

        _hubConnection.On("LeftRoom", () =>
        {
            _isRunning = false;
            StopTickTimer();
            OnLeftRoom?.Invoke();
        });

        _hubConnection.On<string>("PlayerDisconnected", name =>
        {
            OnPlayerDisconnected?.Invoke(name);
        });

        _hubConnection.On<string>("PlayerReconnected", name =>
        {
            OnPlayerReconnected?.Invoke(name);
        });

        _hubConnection.Closed += async (error) =>
        {
            _isRunning = false;
            StopTickTimer();
            OnError?.Invoke("Connection lost. Attempting to reconnect...");
            await Task.Delay(5000);
            try
            {
                await _hubConnection.StartAsync();
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"Failed to reconnect: {ex.Message}");
            }
        };

        await _hubConnection.StartAsync();
    }

    // === SOLO GAME ===
    public async Task<GameState?> StartGameAsync(string playerName, AgeMode ageMode, Language language = Language.Indonesian)
    {
        if (_hubConnection == null) return null;

        var state = await _hubConnection.InvokeAsync<GameState>("StartGame", playerName, ageMode, language);
        _isRunning = true;
        StartTickTimer();
        return state;
    }

    public async Task<GameState?> GetGameStateAsync()
    {
        if (_hubConnection == null) return null;
        return await _hubConnection.InvokeAsync<GameState?>("GetGameState");
    }

    public async Task DismissIntroAsync()
    {
        if (_hubConnection == null) return;
        await _hubConnection.SendAsync("DismissIntro");
    }

    // === ROOM MANAGEMENT (Multiplayer) ===
    public async Task<RoomInfo?> CreateRoomAsync(string playerName, AgeMode ageMode, Language language, int maxPlayers = 4)
    {
        if (_hubConnection == null) return null;
        return await _hubConnection.InvokeAsync<RoomInfo>("CreateRoom", playerName, ageMode, language, maxPlayers);
    }

    public async Task<RoomInfo?> JoinRoomAsync(string roomCode, string playerName)
    {
        if (_hubConnection == null) return null;
        return await _hubConnection.InvokeAsync<RoomInfo?>("JoinRoom", roomCode, playerName);
    }

    public async Task LeaveRoomAsync()
    {
        if (_hubConnection == null) return;
        _isRunning = false;
        StopTickTimer();
        await _hubConnection.SendAsync("LeaveRoom");
    }

    public async Task SetReadyAsync(bool isReady)
    {
        if (_hubConnection == null) return;
        await _hubConnection.SendAsync("SetReady", isReady);
    }

    public async Task<bool> StartMultiplayerGameAsync()
    {
        if (_hubConnection == null) return false;
        return await _hubConnection.InvokeAsync<bool>("StartMultiplayerGame");
    }

    public async Task<List<LeaderboardEntry>?> GetLeaderboardAsync()
    {
        if (_hubConnection == null) return null;
        return await _hubConnection.InvokeAsync<List<LeaderboardEntry>>("GetLeaderboard");
    }

    // === SAVINGS ACCOUNT ===
    public async Task<bool> DepositToSavingsAsync(decimal amount)
    {
        if (_hubConnection == null) return false;
        return await _hubConnection.InvokeAsync<bool>("DepositToSavings", amount);
    }

    public async Task<bool> WithdrawFromSavingsAsync(decimal amount)
    {
        if (_hubConnection == null) return false;
        return await _hubConnection.InvokeAsync<bool>("WithdrawFromSavings", amount);
    }

    // === CERTIFICATE OF DEPOSIT ===
    public async Task<bool> BuyDepositoAsync(int periodMonths, decimal amount, bool autoRollOver = false)
    {
        if (_hubConnection == null) return false;
        return await _hubConnection.InvokeAsync<bool>("BuyDeposito", periodMonths, amount, autoRollOver);
    }

    public async Task<bool> WithdrawDepositoAsync(string depositoId, bool earlyWithdraw)
    {
        if (_hubConnection == null) return false;
        return await _hubConnection.InvokeAsync<bool>("WithdrawDeposito", depositoId, earlyWithdraw);
    }

    public async Task<bool> ToggleDepositoAutoRollOverAsync(string depositoId)
    {
        if (_hubConnection == null) return false;
        return await _hubConnection.InvokeAsync<bool>("ToggleDepositoAutoRollOver", depositoId);
    }

    // === GOVERNMENT BONDS ===
    public async Task<bool> BuyBondAsync(string bondType, decimal amount)
    {
        if (_hubConnection == null) return false;
        return await _hubConnection.InvokeAsync<bool>("BuyBond", bondType, amount);
    }

    public async Task<bool> WithdrawBondAsync(string bondId)
    {
        if (_hubConnection == null) return false;
        return await _hubConnection.InvokeAsync<bool>("WithdrawBond", bondId);
    }

    // === INDIVIDUAL STOCKS ===
    public async Task<bool> BuyStockAsync(string ticker, int lots)
    {
        if (_hubConnection == null) return false;
        return await _hubConnection.InvokeAsync<bool>("BuyStock", ticker, lots);
    }

    public async Task<bool> SellStockAsync(string ticker, int lots)
    {
        if (_hubConnection == null) return false;
        return await _hubConnection.InvokeAsync<bool>("SellStock", ticker, lots);
    }

    // === CRYPTO ===
    public async Task<bool> BuyCryptoAsync(string symbol, decimal amount)
    {
        if (_hubConnection == null) return false;
        return await _hubConnection.InvokeAsync<bool>("BuyCrypto", symbol, amount);
    }

    public async Task<bool> SellCryptoAsync(string symbol, decimal amount)
    {
        if (_hubConnection == null) return false;
        return await _hubConnection.InvokeAsync<bool>("SellCrypto", symbol, amount);
    }

    // === GENERAL ASSETS (Index Fund, Gold, Crowdfunding) ===
    public async Task<bool> BuyAssetAsync(string assetType)
    {
        if (_hubConnection == null) return false;
        return await _hubConnection.InvokeAsync<bool>("BuyAsset", assetType);
    }

    public async Task<bool> BuyGoldByGramsAsync(decimal grams)
    {
        if (_hubConnection == null) return false;
        return await _hubConnection.InvokeAsync<bool>("BuyGoldByGrams", grams);
    }

    public async Task<bool> SellAssetAsync(string assetType)
    {
        if (_hubConnection == null) return false;
        return await _hubConnection.InvokeAsync<bool>("SellAsset", assetType);
    }

    public async Task<bool> SellAllAssetsAsync(string assetType)
    {
        if (_hubConnection == null) return false;
        return await _hubConnection.InvokeAsync<bool>("SellAllAssets", assetType);
    }

    // === CROWDFUNDING ===
    public async Task<bool> BuyCrowdfundingAsync(string projectId, decimal amount)
    {
        if (_hubConnection == null) return false;
        return await _hubConnection.InvokeAsync<bool>("BuyCrowdfunding", projectId, amount);
    }

    // === EVENT HANDLING ===
    public async Task<bool> PayEventFromCashAsync()
    {
        if (_hubConnection == null) return false;
        return await _hubConnection.InvokeAsync<bool>("PayEventFromCash");
    }

    public async Task<bool> PayEventFromSavingsAsync()
    {
        if (_hubConnection == null) return false;
        return await _hubConnection.InvokeAsync<bool>("PayEventFromSavings");
    }

    public async Task<bool> PayEventFromPortfolioAsync(string assetType)
    {
        if (_hubConnection == null) return false;
        return await _hubConnection.InvokeAsync<bool>("PayEventFromPortfolio", assetType);
    }

    // === GAME CONTROL ===
    public async Task ProcessTickAsync()
    {
        if (_hubConnection == null || !_isRunning) return;
        await _hubConnection.SendAsync("ProcessTick");
    }

    public async Task PauseGameAsync()
    {
        if (_hubConnection == null) return;
        _isRunning = false;
        StopTickTimer();
        await _hubConnection.SendAsync("PauseGame");
    }

    public async Task ResumeGameAsync()
    {
        if (_hubConnection == null) return;
        _isRunning = true;
        StartTickTimer();
        await _hubConnection.SendAsync("ResumeGame");
    }

    public async Task RestartGameAsync()
    {
        if (_hubConnection == null) return;
        _isRunning = true;
        StartTickTimer();
        await _hubConnection.SendAsync("RestartGame");
    }

    // === DATA RETRIEVAL ===
    public async Task<Dictionary<string, AssetDefinition>?> GetAssetDefinitionsAsync()
    {
        if (_hubConnection == null) return null;
        return await _hubConnection.InvokeAsync<Dictionary<string, AssetDefinition>>("GetAssetDefinitions");
    }

    public async Task<List<DepositoRate>?> GetDepositoRatesAsync()
    {
        if (_hubConnection == null) return null;
        return await _hubConnection.InvokeAsync<List<DepositoRate>>("GetDepositoRates");
    }

    public async Task<List<BondRate>?> GetBondRatesAsync()
    {
        if (_hubConnection == null) return null;
        return await _hubConnection.InvokeAsync<List<BondRate>>("GetBondRates");
    }

    private void StartTickTimer()
    {
        StopTickTimer();
        _tickTimer = new System.Timers.Timer(1000);
        _tickTimer.Elapsed += async (sender, e) =>
        {
            if (_isRunning)
            {
                await ProcessTickAsync();
            }
        };
        _tickTimer.AutoReset = true;
        _tickTimer.Start();
    }

    private void StopTickTimer()
    {
        _tickTimer?.Stop();
        _tickTimer?.Dispose();
        _tickTimer = null;
    }

    public async ValueTask DisposeAsync()
    {
        StopTickTimer();
        if (_hubConnection != null)
        {
            await _hubConnection.DisposeAsync();
        }
    }
}
