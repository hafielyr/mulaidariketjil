# Multiplayer Implementation Plan

## Overview

Transform the current single-player investment simulation into a multiplayer experience where players can create/join rooms using a room code, compete in real-time, and compare results at the end of a 10-year game.

Each room host sets the game configuration (light/adult mode, language). All players in the room share the same market conditions (stock prices, crypto prices, events) but make independent investment decisions. At game end, all players are ranked by net worth alongside the bot.

---

## Phase 1: Shared Models (Shared Project)

### 1.1 New Models in `PortfolioItem.cs`

```
RoomInfo
├── RoomCode          (string, 6-char alphanumeric)
├── HostConnectionId  (string)
├── HostPlayerName    (string)
├── AgeMode           (AgeMode - Light/Adult)
├── Language          (Language - ID/EN)
├── MaxPlayers        (int, default 4)
├── Players           (List<RoomPlayer>)
├── Status            (RoomStatus: Waiting / InProgress / Finished)
├── CreatedAt         (DateTime)

RoomPlayer
├── ConnectionId      (string)
├── PlayerName        (string)
├── IsHost            (bool)
├── IsReady           (bool)
├── IsConnected       (bool)

RoomStatus (enum)
├── Waiting
├── InProgress
├── Finished

MultiplayerGameState (extends/wraps GameState)
├── RoomCode          (string)
├── Players           (List<PlayerSummary>)  // name, net worth, connected
├── IsHost            (bool)
├── Leaderboard       (List<LeaderboardEntry>)  // sorted by net worth

PlayerSummary
├── PlayerName        (string)
├── NetWorth          (decimal)
├── IsConnected       (bool)
├── CurrentYear       (int)
├── CurrentMonth      (int)

LeaderboardEntry
├── Rank              (int)
├── PlayerName        (string)
├── NetWorth          (decimal)
├── IsBot             (bool)
├── TotalProfit       (decimal)
```

### 1.2 Modify Existing `GameState`

Add fields:
- `RoomCode` (string?, null for solo play)
- `IsMultiplayer` (bool)
- `PlayerSummaries` (List<PlayerSummary>?, other players' public info)
- `IsHost` (bool)
- `AllPlayersFinished` (bool, for end-game ranking)
- `FinalLeaderboard` (List<LeaderboardEntry>?, populated at game over)

---

## Phase 2: Room Management (Server)

### 2.1 New Service: `RoomManager.cs`

Registered as singleton alongside `GameEngine`.

```
RoomManager
├── Fields:
│   ├── _rooms: Dictionary<string, RoomInfo>
│   ├── _connectionToRoom: Dictionary<string, string>  // connectionId → roomCode
│   └── _lock: object
│
├── Methods:
│   ├── CreateRoom(connectionId, playerName, ageMode, language) → RoomInfo
│   │   └── Generate unique 6-char room code (uppercase letters + digits)
│   │
│   ├── JoinRoom(roomCode, connectionId, playerName) → RoomInfo?
│   │   ├── Validate: room exists, status == Waiting, not full
│   │   ├── Add player to room
│   │   └── Return null with error reason if invalid
│   │
│   ├── LeaveRoom(connectionId) → (roomCode, wasHost)
│   │   ├── Remove player from room
│   │   ├── If host left: transfer host to next player, or close room if empty
│   │   └── Return room code for broadcast cleanup
│   │
│   ├── SetReady(connectionId, isReady) → RoomInfo?
│   │
│   ├── StartGame(connectionId) → bool
│   │   ├── Validate: caller is host, all players ready
│   │   └── Set status = InProgress
│   │
│   ├── GetRoom(roomCode) → RoomInfo?
│   ├── GetRoomByConnection(connectionId) → RoomInfo?
│   ├── IsInRoom(connectionId) → bool
│   ├── RemoveRoom(roomCode) → void  // cleanup finished/empty rooms
│   └── GenerateRoomCode() → string  // collision-resistant 6-char code
```

### 2.2 Modify `GameEngine.cs`

**Shared Market State:** When a room starts, all players in the room must share:
- Same `AvailableStocks` (same 5 random stocks)
- Same `AvailableCryptos` (same selection)
- Same initial asset prices
- Same price fluctuations each month (use shared seed)
- Same random events (same event month, same event type)

**Implementation approach:**
- Add a `RoomMarketState` class that holds the shared random seed and derived market data
- `CreateSession()` gains optional `roomCode` parameter
- When creating sessions for a room, all sessions share the same `RoomMarketState`
- Price updates in `ProcessMonthEnd()` use the shared state instead of per-session randomness

```
RoomMarketState
├── SharedRandom          (Random with fixed seed per room)
├── AvailableStocks       (List<StockDefinition>)
├── AvailableCryptos      (List<CryptoDefinition>)
├── InitialPrices         (Dictionary<string, decimal>)
├── EventMonthPerYear     (Dictionary<int, int>)  // pre-generated
├── EventTypePerYear      (Dictionary<int, RandomEvent>)
```

**New methods on GameEngine:**
- `CreateMultiplayerSessions(roomCode, players, ageMode, language)` - creates all sessions with shared market state
- `GetRoomSessions(roomCode)` - returns all sessions in a room
- `GetLeaderboard(roomCode)` - returns sorted player rankings + bot

**Modify existing methods:**
- `ProcessMonthEnd()` - use `RoomMarketState` for price updates if multiplayer
- `CreateSession()` - accept optional `RoomMarketState` to share market data

### 2.3 Modify `GameSession.cs`

Add fields:
- `RoomCode` (string?, null for solo)
- `RoomMarketState` (reference to shared market data)
- `IsMultiplayer` (bool)

Modify `ToGameState()`:
- Include `RoomCode`, `IsMultiplayer`, `IsHost` fields
- Include `PlayerSummaries` (fetched from GameEngine for same room)

---

## Phase 3: SignalR Hub Changes (Server)

### 3.1 Modify `GameHub.cs`

**New Hub Methods (Room Management):**

```csharp
// Room lifecycle
CreateRoom(playerName, ageMode, language) → RoomInfo
JoinRoom(roomCode, playerName) → RoomInfo?
LeaveRoom() → void
SetReady(isReady) → void
StartMultiplayerGame() → bool

// In-game multiplayer
GetLeaderboard() → List<LeaderboardEntry>
```

**SignalR Groups:**
- When a player creates/joins a room, add them to a SignalR group named by room code:
  `await Groups.AddToGroupAsync(Context.ConnectionId, roomCode)`
- Broadcast room updates to the group:
  `await Clients.Group(roomCode).SendAsync("RoomUpdated", roomInfo)`

**New Client Callbacks:**

| Callback | When | Payload |
|----------|------|---------|
| `RoomUpdated` | Player joins/leaves/readies | `RoomInfo` |
| `RoomGameStarted` | Host starts game | `GameState` (initial) |
| `PlayerJoined` | New player joins room | `RoomPlayer` |
| `PlayerLeft` | Player leaves room | `string playerName` |
| `LeaderboardUpdated` | Any player's month ends | `List<LeaderboardEntry>` |
| `AllPlayersFinished` | All players reached Year 10 | `List<LeaderboardEntry>` |
| `HostChanged` | Host disconnected, new host | `string newHostName` |
| `RoomClosed` | Room closed (host left, no players) | - |
| `PlayerDisconnected` | Player lost connection | `string playerName` |
| `PlayerReconnected` | Player reconnected | `string playerName` |

**Modify Existing Methods:**
- `ProcessTick()` - after processing, broadcast leaderboard update to room group (throttled, e.g., every month-end only)
- `OnDisconnectedAsync()` - handle room cleanup, notify other players, keep session alive for reconnection (grace period)
- `StartGame()` - keep working for solo play (backward compatible)

### 3.2 Reconnection Handling

When a player disconnects mid-game:
1. Mark player as `IsConnected = false` in room
2. Keep their `GameSession` alive for 2 minutes
3. Broadcast `PlayerDisconnected` to room
4. Their game timer pauses (no ticks processed)
5. On reconnect: match by `PlayerId`, restore session, resume timer
6. After 2 min timeout: remove session, notify room

---

## Phase 4: Client Changes

### 4.1 Modify `GameClient.cs`

**New Methods:**

```csharp
// Room management
CreateRoomAsync(playerName, ageMode, language) → RoomInfo
JoinRoomAsync(roomCode, playerName) → RoomInfo?
LeaveRoomAsync() → void
SetReadyAsync(isReady) → void
StartMultiplayerGameAsync() → bool
GetLeaderboardAsync() → List<LeaderboardEntry>
```

**New Events:**

```csharp
event Action<RoomInfo>? OnRoomUpdated;
event Action<GameState>? OnRoomGameStarted;
event Action<string>? OnPlayerJoined;
event Action<string>? OnPlayerLeft;
event Action<List<LeaderboardEntry>>? OnLeaderboardUpdated;
event Action<List<LeaderboardEntry>>? OnAllPlayersFinished;
event Action<string>? OnHostChanged;
event Action? OnRoomClosed;
```

**Register callbacks in `ConnectAsync()`.**

### 4.2 Modify `Game.razor` - New UI Screens

**Screen Flow (Updated):**

```
┌─────────────────────┐
│   Mode Selection     │  NEW
│  Solo / Multiplayer  │
└────────┬────────────┘
         │
    ┌────┴────┐
    │         │
    ▼         ▼
┌────────┐  ┌──────────────┐
│ Solo   │  │ Multiplayer  │  NEW
│ (same  │  │  ┌─────────┐ │
│  as    │  │  │ Create  │ │
│ today) │  │  │  Room   │ │
│        │  │  ├─────────┤ │
│        │  │  │  Join   │ │
│        │  │  │  Room   │ │
│        │  │  └─────────┘ │
└────────┘  └──────┬───────┘
                   │
              ┌────┴────┐
              │         │
              ▼         ▼
        ┌──────────┐  ┌──────────┐
        │ Create   │  │  Join    │  NEW
        │ Room     │  │  Room    │
        │ Screen   │  │  Screen  │
        │ (config) │  │ (code)   │
        └────┬─────┘  └────┬─────┘
             │              │
             ▼              ▼
        ┌───────────────────────┐
        │     Room Lobby        │  NEW
        │  Players, Ready, Start│
        └───────────┬───────────┘
                    │
                    ▼
        ┌───────────────────────┐
        │    Game Screen        │  MODIFIED
        │  (+ leaderboard bar)  │
        └───────────┬───────────┘
                    │
                    ▼
        ┌───────────────────────┐
        │   Game Over Screen    │  MODIFIED
        │  (multiplayer ranks)  │
        └───────────────────────┘
```

### 4.3 New UI Components Detail

#### Screen: Mode Selection

Simple choice screen after the title:
- **Solo** button - proceeds to current start screen (name, mode, language)
- **Multiplayer** button - proceeds to create/join screen

#### Screen: Create Room

Form fields:
- Player name (text input)
- Age mode toggle (Light / Adult)
- Language toggle (Bahasa Indonesia / English)
- Max players slider (2-4)
- **"Create Room"** button

On submit → calls `CreateRoomAsync()` → navigates to Room Lobby.

#### Screen: Join Room

Form fields:
- Player name (text input)
- Room code (6-character input, uppercase, auto-format)
- **"Join Room"** button

Validation:
- Show error if room not found, room full, or game already started
- Auto-display the room's mode/language so the joiner knows before confirming

On submit → calls `JoinRoomAsync()` → navigates to Room Lobby.

#### Screen: Room Lobby

Displays:
- Room code (large, copyable, shareable)
- Room settings (mode, language) - read-only for non-hosts
- Player list with ready status:
  ```
  [Crown] Player1 (Host)     [Ready ✓]
          Player2             [Ready ✓]
          Player3             [Not Ready]
          (Waiting for players...)
  ```
- **"Ready"** toggle button (for non-hosts)
- **"Start Game"** button (host only, enabled when all players are ready and at least 2 players)
- **"Leave Room"** button

Real-time updates via `OnRoomUpdated` callback.

#### Modified: Game Screen

Add a **leaderboard sidebar/bar** showing:
- All players' names and current net worth
- Sorted by net worth (descending)
- Updated every month-end
- Show connected/disconnected status
- Compact view so it doesn't dominate the screen

Position: either as a collapsible panel on the right side, or as a thin bar at the top below the header.

#### Modified: Game Over Screen

Replace single bot comparison with **full leaderboard**:
- Rank all players + bot by final net worth
- Show each player's:
  - Final net worth
  - Total profit (net worth - starting cash)
  - Investment allocation breakdown
  - Events paid (and from which source)
- Highlight the winner
- "Play Again" → returns to Room Lobby (if room still exists) or Mode Selection
- "Back to Menu" → returns to Mode Selection

---

## Phase 5: Game Synchronization

### 5.1 Independent Timers, Shared Markets

Each player runs their own tick timer independently (same as current). This means:
- Players may be at different months (e.g., Player1 at Year 3 Month 5, Player2 at Year 3 Month 2) if one pauses
- **Market prices are deterministic** per month - generated from shared seed so all players see the same prices for the same month regardless of when they reach it
- Events trigger at the same month for all players (pre-determined per room)

### 5.2 Price Determinism

Instead of using `Random.Next()` during `ProcessMonthEnd()`, pre-calculate or derive prices from:
```
price_change = DeriveFromSeed(sharedSeed, year, month, assetId)
```

This ensures Player A at Year 3 Month 6 sees the same stock price as Player B at Year 3 Month 6, even if they reach that month at different real-world times.

### 5.3 Game End

- Each player's game ends independently when they reach Year 10 Month 12
- When a player finishes: broadcast their final stats to room
- When ALL players finish: broadcast final leaderboard with `AllPlayersFinished`
- Players who finish early see a "Waiting for other players..." overlay with the live leaderboard

---

## Phase 6: Edge Cases & Error Handling

### 6.1 Host Disconnection
- Transfer host to the next connected player (by join order)
- If no players remain, close room and clean up all sessions
- Broadcast `HostChanged` to remaining players

### 6.2 Player Disconnection Mid-Game
- Pause their timer, keep session for 2 minutes
- Show "(Disconnected)" next to their name in leaderboard
- If they don't reconnect in 2 min: remove from room, their data stays for final ranking but marked as "Disconnected"

### 6.3 Room Cleanup
- Rooms in `Waiting` status auto-expire after 10 minutes of inactivity
- Rooms in `Finished` status auto-expire after 5 minutes
- Empty rooms are immediately removed

### 6.4 Backward Compatibility
- Solo mode remains exactly as it is today
- All existing `GameHub` methods continue to work for solo sessions
- Multiplayer adds NEW hub methods; existing methods check if session is multiplayer and include room-level broadcasts where needed

---

## Implementation Order

| Step | Description | Files Changed | Effort |
|------|-------------|---------------|--------|
| 1 | Add new models (RoomInfo, etc.) | `Shared/Models/PortfolioItem.cs` | Small |
| 2 | Create `RoomManager` service | `Server/Services/RoomManager.cs` (new) | Medium |
| 3 | Add `RoomMarketState` for shared markets | `Server/Services/GameEngine.cs` | Medium |
| 4 | Modify `GameSession` for room awareness | `Server/Services/GameSession.cs` | Small |
| 5 | Add room hub methods | `Server/Hubs/GameHub.cs` | Medium |
| 6 | Register services, groups | `Server/Program.cs` | Small |
| 7 | Add client methods & events | `Client/Services/GameClient.cs` | Medium |
| 8 | Add Mode Selection screen | `Client/Pages/Game.razor` | Small |
| 9 | Add Create/Join Room screens | `Client/Pages/Game.razor` | Medium |
| 10 | Add Room Lobby screen | `Client/Pages/Game.razor` | Medium |
| 11 | Add leaderboard to game screen | `Client/Pages/Game.razor` | Medium |
| 12 | Modify game over screen | `Client/Pages/Game.razor` | Medium |
| 13 | Deterministic price generation | `Server/Services/GameEngine.cs` | Medium |
| 14 | Reconnection handling | `GameHub.cs`, `GameClient.cs` | Medium |
| 15 | Room cleanup & edge cases | `RoomManager.cs`, `GameHub.cs` | Small |
| 16 | Testing & polish | All | Large |

---

## New Files Summary

| File | Purpose |
|------|---------|
| `Server/Services/RoomManager.cs` | Room creation, joining, lifecycle management |

## Modified Files Summary

| File | Changes |
|------|---------|
| `Shared/Models/PortfolioItem.cs` | New models: RoomInfo, RoomPlayer, RoomStatus, PlayerSummary, LeaderboardEntry. New fields on GameState. |
| `Server/Services/GameEngine.cs` | RoomMarketState class, shared market data, deterministic pricing, multiplayer session creation, leaderboard calculation |
| `Server/Services/GameSession.cs` | RoomCode, RoomMarketState reference, IsMultiplayer flag, updated ToGameState() |
| `Server/Hubs/GameHub.cs` | New room management methods, SignalR groups, room broadcasts, reconnection logic |
| `Server/Program.cs` | Register RoomManager singleton |
| `Client/Services/GameClient.cs` | New room methods, new event callbacks |
| `Client/Pages/Game.razor` | Mode selection, create/join room, lobby, leaderboard sidebar, multiplayer game over |
