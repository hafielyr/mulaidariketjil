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

    /// <summary>
    /// Creates a new room. Host is tracked separately and NOT added to Players list.
    /// </summary>
    public RoomInfo CreateRoom(string connectionId, string hostName, AgeMode ageMode, Language language)
    {
        lock (_lock)
        {
            var roomCode = GenerateRoomCode();
            var room = new RoomInfo
            {
                RoomCode = roomCode,
                HostConnectionId = connectionId,
                HostPlayerName = hostName,
                AgeMode = ageMode,
                Language = language,
                Status = RoomStatus.Waiting,
                CreatedAt = DateTime.UtcNow,
                Players = new List<RoomPlayer>()  // host is NOT in this list
            };

            _rooms[roomCode] = room;
            _connectionToRoom[connectionId] = roomCode;

            _logger.LogInformation("Room {RoomCode} created by host {HostName}", roomCode, hostName);
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

            if (room.Players.Any(p => p.ConnectionId == connectionId))
                return (null, "Already in this room");

            room.Players.Add(new RoomPlayer
            {
                ConnectionId = connectionId,
                PlayerName = playerName,
                IsReady = false,
                IsConnected = true
            });

            _connectionToRoom[connectionId] = roomCode;

            _logger.LogInformation("Player {PlayerName} joined room {RoomCode}", playerName, roomCode);
            return (room, null);
        }
    }

    /// <summary>
    /// Returns (roomCode, wasHost, updatedRoom).
    /// updatedRoom is null when room is now empty.
    /// </summary>
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

            // Check if this is the host leaving
            bool wasHost = room.HostConnectionId == connectionId;

            if (wasHost)
            {
                // Host leaves: remove host tracking but keep room alive (players still playing/in lobby)
                _connectionToRoom.Remove(connectionId);
                room.HostConnectionId = string.Empty;
                room.HostPlayerName = string.Empty;

                // If no players remain, remove the room
                if (room.Players.Count == 0)
                {
                    _rooms.Remove(roomCode);
                    _logger.LogInformation("Room {RoomCode} removed (host left, no players)", roomCode);
                    return (roomCode, true, null);
                }

                _logger.LogInformation("Host left room {RoomCode}", roomCode);
                return (roomCode, true, room);
            }

            // A regular player is leaving
            var player = room.Players.FirstOrDefault(p => p.ConnectionId == connectionId);
            if (player != null)
                room.Players.Remove(player);
            _connectionToRoom.Remove(connectionId);

            // If room is empty (no host, no players) remove it
            if (room.Players.Count == 0 && string.IsNullOrEmpty(room.HostConnectionId))
            {
                _rooms.Remove(roomCode);
                _logger.LogInformation("Room {RoomCode} removed (empty)", roomCode);
                return (roomCode, false, null);
            }

            return (roomCode, false, room);
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

    /// <summary>
    /// Mark a player as unlock-ready. Returns true when ALL players in the room are unlock-ready.
    /// </summary>
    public bool SetUnlockReady(string connectionId)
    {
        lock (_lock)
        {
            if (!_connectionToRoom.TryGetValue(connectionId, out var roomCode))
                return false;

            if (!_rooms.TryGetValue(roomCode, out var room))
                return false;

            var player = room.Players.FirstOrDefault(p => p.ConnectionId == connectionId);
            if (player != null)
                player.IsUnlockReady = true;

            return room.Players.Count > 0 && room.Players.All(p => p.IsUnlockReady);
        }
    }

    /// <summary>
    /// Reset all players' IsUnlockReady flags for the next unlock event.
    /// </summary>
    public void ResetUnlockReady(string roomCode)
    {
        lock (_lock)
        {
            if (!_rooms.TryGetValue(roomCode, out var room)) return;
            foreach (var p in room.Players)
                p.IsUnlockReady = false;
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

            if (room.Players.Count < 1)
                return (false, "Need at least 1 player");

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

    public bool IsHost(string connectionId)
    {
        lock (_lock)
        {
            if (!_connectionToRoom.TryGetValue(connectionId, out var roomCode))
                return false;
            if (!_rooms.TryGetValue(roomCode, out var room))
                return false;
            return room.HostConnectionId == connectionId;
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

    public (string? RoomCode, string? PlayerName, bool WasHost) MarkReconnected(string oldConnectionId, string newConnectionId)
    {
        lock (_lock)
        {
            if (!_connectionToRoom.TryGetValue(oldConnectionId, out var roomCode))
                return (null, null, false);

            if (!_rooms.TryGetValue(roomCode, out var room))
                return (null, null, false);

            // Check if it was the host reconnecting
            if (room.HostConnectionId == oldConnectionId)
            {
                room.HostConnectionId = newConnectionId;
                _connectionToRoom.Remove(oldConnectionId);
                _connectionToRoom[newConnectionId] = roomCode;
                return (roomCode, room.HostPlayerName, true);
            }

            var player = room.Players.FirstOrDefault(p => p.ConnectionId == oldConnectionId);
            if (player == null) return (null, null, false);

            player.ConnectionId = newConnectionId;
            player.IsConnected = true;

            _connectionToRoom.Remove(oldConnectionId);
            _connectionToRoom[newConnectionId] = roomCode;

            return (roomCode, player.PlayerName, false);
        }
    }

    public void RemoveRoom(string roomCode)
    {
        lock (_lock)
        {
            if (_rooms.TryGetValue(roomCode, out var room))
            {
                // Remove host connection
                if (!string.IsNullOrEmpty(room.HostConnectionId))
                    _connectionToRoom.Remove(room.HostConnectionId);
                // Remove player connections
                foreach (var player in room.Players)
                    _connectionToRoom.Remove(player.ConnectionId);
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
