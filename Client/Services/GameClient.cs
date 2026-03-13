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
    public event Action<string?>? OnReconnecting;
    public event Action<string?>? OnReconnected;

    // Multiplayer events
    public event Action<RoomInfo>? OnRoomUpdated;
    public event Action<GameState>? OnRoomGameStarted;
    public event Action<RoomInfo>? OnHostDashboardStarted;
    public event Action<string>? OnPlayerJoined;
    public event Action<string>? OnPlayerLeft;
    public event Action<List<LeaderboardEntry>>? OnLeaderboardUpdated;
    public event Action<List<LeaderboardEntry>>? OnAllPlayersFinished;
    public event Action? OnHostLeft;
    public event Action? OnRoomClosed;
    public event Action<string>? OnRoomError;
    public event Action? OnLeftRoom;
    public event Action<string>? OnPlayerDisconnected;
    public event Action<string>? OnPlayerReconnected;

    // Multiplayer control events
    public event Action? OnAllPaused;
    public event Action? OnAllResumed;
    public event Action<string>? OnAssetUnlocked;   // payload: asset type string
    public event Action? OnUnlockComplete;

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
            .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(30) })
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
            // Only players get this — start their tick timer
            _isRunning = true;
            StartTickTimer();
            OnRoomGameStarted?.Invoke(state);
        });

        _hubConnection.On<RoomInfo>("HostDashboardStarted", room =>
        {
            // Host gets this instead of RoomGameStarted — host does NOT get a tick timer
            OnHostDashboardStarted?.Invoke(room);
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

        _hubConnection.On("HostLeft", () =>
        {
            OnHostLeft?.Invoke();
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

        // Multiplayer control callbacks
        _hubConnection.On("AllPaused", () =>
        {
            _isRunning = false;
            StopTickTimer();
            OnAllPaused?.Invoke();
        });

        _hubConnection.On("AllResumed", () =>
        {
            _isRunning = true;
            StartTickTimer();
            OnAllResumed?.Invoke();
        });

        _hubConnection.On<string>("AssetUnlocked", assetType =>
        {
            // Pause the tick timer — all sessions are paused server-side until all players dismiss popup
            _isRunning = false;
            StopTickTimer();
            OnAssetUnlocked?.Invoke(assetType);
        });

        _hubConnection.On("UnlockComplete", () =>
        {
            _isRunning = true;
            StartTickTimer();
            OnUnlockComplete?.Invoke();
        });

        _hubConnection.Reconnecting += (error) =>
        {
            OnReconnecting?.Invoke(error?.Message);
            return Task.CompletedTask;
        };

        _hubConnection.Reconnected += (connectionId) =>
        {
            OnReconnected?.Invoke(connectionId);
            return Task.CompletedTask;
        };

        _hubConnection.Closed += async (error) =>
        {
            _isRunning = false;
            StopTickTimer();

            // Retry up to 3 times with increasing delays before giving up
            var retryDelays = new[] { 5000, 10000, 20000 };
            foreach (var delay in retryDelays)
            {
                OnReconnecting?.Invoke("Attempting to reconnect...");
                await Task.Delay(delay);
                try
                {
                    await _hubConnection.StartAsync();
                    OnReconnected?.Invoke(null);
                    return;
                }
                catch
                {
                    // Continue to next retry
                }
            }

            OnError?.Invoke("Connection lost. Please refresh the page.");
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
    public async Task<RoomInfo?> CreateRoomAsync(string hostName, AgeMode ageMode, Language language)
    {
        if (_hubConnection == null) return null;
        return await _hubConnection.InvokeAsync<RoomInfo>("CreateRoom", hostName, ageMode, language);
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

    // === HOST DASHBOARD ===
    public async Task PauseAllPlayersAsync()
    {
        if (_hubConnection == null) return;
        await _hubConnection.SendAsync("PauseAllPlayers");
    }

    public async Task ResumeAllPlayersAsync()
    {
        if (_hubConnection == null) return;
        await _hubConnection.SendAsync("ResumeAllPlayers");
    }

    public async Task SetUnlockReadyAsync()
    {
        if (_hubConnection == null) return;
        await _hubConnection.SendAsync("SetUnlockReady");
    }

    public async Task HostForceResumeAsync()
    {
        if (_hubConnection == null) return;
        await _hubConnection.SendAsync("HostForceResume");
    }

    public async Task<List<PlayerSummary>?> GetAllPlayerPortfoliosAsync()
    {
        if (_hubConnection == null) return null;
        return await _hubConnection.InvokeAsync<List<PlayerSummary>>("GetAllPlayerPortfolios");
    }

    public async Task<RoomInfo?> GetRoomStatusAsync()
    {
        if (_hubConnection == null) return null;
        return await _hubConnection.InvokeAsync<RoomInfo?>("GetRoomStatus");
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
    public async Task<bool> BuyDepositoAsync(int periodMonths, decimal amount, bool autoRollOver = false, bool isShariah = false)
    {
        if (_hubConnection == null) return false;
        return await _hubConnection.InvokeAsync<bool>("BuyDeposito", periodMonths, amount, autoRollOver, isShariah);
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

    // === INDEX FUNDS ===
    public async Task<bool> BuyIndexAsync(string indexId, decimal amount)
    {
        if (_hubConnection == null) return false;
        return await _hubConnection.InvokeAsync<bool>("BuyIndex", indexId, amount);
    }

    public async Task<bool> SellIndexAsync(string indexId, decimal units)
    {
        if (_hubConnection == null) return false;
        return await _hubConnection.InvokeAsync<bool>("SellIndex", indexId, units);
    }

    public async Task<bool> SellAllIndexAsync(string indexId)
    {
        if (_hubConnection == null) return false;
        return await _hubConnection.InvokeAsync<bool>("SellAllIndex", indexId);
    }

    // === GENERAL ASSETS (Gold, Crowdfunding) ===
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
