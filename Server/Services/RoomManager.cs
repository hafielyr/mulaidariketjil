using InvestmentGame.Shared.Models;

namespace InvestmentGame.Server.Services;

public class RoomManager
{
    private readonly Dictionary<string, RoomInfo> _rooms = new();
    private readonly Dictionary<string, string> _connectionToRoom = new(); // connectionId → roomCode
    private readonly object _lock = new();
    private readonly Random _random = new();
    private readonly ILogger<RoomManager> _logger;

    public RoomManager(ILogger<RoomManager> logger)
    {
        _logger = logger;
    }

    public RoomInfo CreateRoom(string connectionId, string playerName, AgeMode ageMode, Language language, int maxPlayers = 4)
    {
        lock (_lock)
        {
            var roomCode = GenerateRoomCode();
            var room = new RoomInfo
            {
                RoomCode = roomCode,
                HostConnectionId = connectionId,
                HostPlayerName = playerName,
                AgeMode = ageMode,
                Language = language,
                MaxPlayers = Math.Clamp(maxPlayers, 2, 4),
                Status = RoomStatus.Waiting,
                CreatedAt = DateTime.UtcNow,
                Players = new List<RoomPlayer>
                {
                    new RoomPlayer
                    {
                        ConnectionId = connectionId,
                        PlayerName = playerName,
                        IsHost = true,
                        IsReady = true,
                        IsConnected = true
                    }
                }
            };

            _rooms[roomCode] = room;
            _connectionToRoom[connectionId] = roomCode;

            _logger.LogInformation("Room {RoomCode} created by {PlayerName}", roomCode, playerName);
            return room;
        }
    }

    public (RoomInfo? Room, string? Error) JoinRoom(string roomCode, string connectionId, string playerName)
    {
        lock (_lock)
        {
            roomCode = roomCode.ToUpper();

            if (!_rooms.TryGetValue(roomCode, out var room))
                return (null, "Room not found");

            if (room.Status != RoomStatus.Waiting)
                return (null, "Game already started");

            if (room.Players.Count >= room.MaxPlayers)
                return (null, "Room is full");

            if (room.Players.Any(p => p.ConnectionId == connectionId))
                return (null, "Already in this room");

            room.Players.Add(new RoomPlayer
            {
                ConnectionId = connectionId,
                PlayerName = playerName,
                IsHost = false,
                IsReady = false,
                IsConnected = true
            });

            _connectionToRoom[connectionId] = roomCode;

            _logger.LogInformation("Player {PlayerName} joined room {RoomCode}", playerName, roomCode);
            return (room, null);
        }
    }

    public (string? RoomCode, bool WasHost, RoomInfo? Room) LeaveRoom(string connectionId)
    {
        lock (_lock)
        {
            if (!_connectionToRoom.TryGetValue(connectionId, out var roomCode))
                return (null, false, null);

            if (!_rooms.TryGetValue(roomCode, out var room))
            {
                _connectionToRoom.Remove(connectionId);
                return (null, false, null);
            }

            var player = room.Players.FirstOrDefault(p => p.ConnectionId == connectionId);
            if (player == null)
            {
                _connectionToRoom.Remove(connectionId);
                return (roomCode, false, room);
            }

            var wasHost = player.IsHost;
            room.Players.Remove(player);
            _connectionToRoom.Remove(connectionId);

            if (room.Players.Count == 0)
            {
                _rooms.Remove(roomCode);
                _logger.LogInformation("Room {RoomCode} removed (empty)", roomCode);
                return (roomCode, wasHost, null);
            }

            // Transfer host if needed
            if (wasHost)
            {
                var newHost = room.Players.FirstOrDefault(p => p.IsConnected) ?? room.Players.First();
                newHost.IsHost = true;
                room.HostConnectionId = newHost.ConnectionId;
                room.HostPlayerName = newHost.PlayerName;
                _logger.LogInformation("Host transferred to {PlayerName} in room {RoomCode}", newHost.PlayerName, roomCode);
            }

            return (roomCode, wasHost, room);
        }
    }

    public RoomInfo? SetReady(string connectionId, bool isReady)
    {
        lock (_lock)
        {
            if (!_connectionToRoom.TryGetValue(connectionId, out var roomCode))
                return null;

            if (!_rooms.TryGetValue(roomCode, out var room))
                return null;

            var player = room.Players.FirstOrDefault(p => p.ConnectionId == connectionId);
            if (player == null) return null;

            player.IsReady = isReady;
            return room;
        }
    }

    public (bool Success, string? Error) StartGame(string connectionId)
    {
        lock (_lock)
        {
            if (!_connectionToRoom.TryGetValue(connectionId, out var roomCode))
                return (false, "Not in a room");

            if (!_rooms.TryGetValue(roomCode, out var room))
                return (false, "Room not found");

            if (room.HostConnectionId != connectionId)
                return (false, "Only host can start");

            if (room.Players.Count < 2)
                return (false, "Need at least 2 players");

            if (!room.Players.All(p => p.IsReady))
                return (false, "Not all players ready");

            room.Status = RoomStatus.InProgress;
            _logger.LogInformation("Game started in room {RoomCode}", roomCode);
            return (true, null);
        }
    }

    public RoomInfo? GetRoom(string roomCode)
    {
        lock (_lock)
        {
            return _rooms.GetValueOrDefault(roomCode.ToUpper());
        }
    }

    public RoomInfo? GetRoomByConnection(string connectionId)
    {
        lock (_lock)
        {
            if (!_connectionToRoom.TryGetValue(connectionId, out var roomCode))
                return null;
            return _rooms.GetValueOrDefault(roomCode);
        }
    }

    public bool IsInRoom(string connectionId)
    {
        lock (_lock)
        {
            return _connectionToRoom.ContainsKey(connectionId);
        }
    }

    public void SetRoomFinished(string roomCode)
    {
        lock (_lock)
        {
            if (_rooms.TryGetValue(roomCode, out var room))
            {
                room.Status = RoomStatus.Finished;
            }
        }
    }

    public void MarkDisconnected(string connectionId)
    {
        lock (_lock)
        {
            if (!_connectionToRoom.TryGetValue(connectionId, out var roomCode))
                return;

            if (!_rooms.TryGetValue(roomCode, out var room))
                return;

            var player = room.Players.FirstOrDefault(p => p.ConnectionId == connectionId);
            if (player != null)
            {
                player.IsConnected = false;
            }
        }
    }

    public (string? RoomCode, string? PlayerName) MarkReconnected(string oldConnectionId, string newConnectionId)
    {
        lock (_lock)
        {
            if (!_connectionToRoom.TryGetValue(oldConnectionId, out var roomCode))
                return (null, null);

            if (!_rooms.TryGetValue(roomCode, out var room))
                return (null, null);

            var player = room.Players.FirstOrDefault(p => p.ConnectionId == oldConnectionId);
            if (player == null) return (null, null);

            player.ConnectionId = newConnectionId;
            player.IsConnected = true;

            _connectionToRoom.Remove(oldConnectionId);
            _connectionToRoom[newConnectionId] = roomCode;

            if (room.HostConnectionId == oldConnectionId)
                room.HostConnectionId = newConnectionId;

            return (roomCode, player.PlayerName);
        }
    }

    public void RemoveRoom(string roomCode)
    {
        lock (_lock)
        {
            if (_rooms.TryGetValue(roomCode, out var room))
            {
                foreach (var player in room.Players)
                {
                    _connectionToRoom.Remove(player.ConnectionId);
                }
                _rooms.Remove(roomCode);
            }
        }
    }

    public void CleanupExpiredRooms()
    {
        lock (_lock)
        {
            var now = DateTime.UtcNow;
            var toRemove = _rooms.Where(kvp =>
                (kvp.Value.Status == RoomStatus.Waiting && (now - kvp.Value.CreatedAt).TotalMinutes > 10) ||
                (kvp.Value.Status == RoomStatus.Finished && (now - kvp.Value.CreatedAt).TotalMinutes > 5)
            ).Select(kvp => kvp.Key).ToList();

            foreach (var roomCode in toRemove)
            {
                RemoveRoom(roomCode);
                _logger.LogInformation("Room {RoomCode} expired and removed", roomCode);
            }
        }
    }

    private string GenerateRoomCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // Excluding confusing chars: I,O,0,1
        string code;
        do
        {
            code = new string(Enumerable.Range(0, 6).Select(_ => chars[_random.Next(chars.Length)]).ToArray());
        } while (_rooms.ContainsKey(code));

        return code;
    }
}
