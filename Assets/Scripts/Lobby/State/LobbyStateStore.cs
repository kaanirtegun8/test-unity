using System;
using System.Collections.Generic;

public class LobbyStateStore
{
    public static LobbyStateStore Local { get; } = new LobbyStateStore();

    private readonly Random roomIdRandom = new Random();
    private const string DefaultLocalDisplayName = "You";
    private const int MinMaxPlayers = 2;
    private const int MaxMaxPlayers = 4;

    public LocalPlayerProfile LocalPlayer { get; private set; }
    public RoomDraft CurrentDraft { get; private set; }
    public RoomState CurrentRoom { get; private set; }
    public List<RoomState> Rooms { get; } = new List<RoomState>();

    public LobbyStateStore()
    {
        InitializeLocalPlayer();
        ResetDraft();
    }

    public void ResetDraft()
    {
        CurrentDraft = new RoomDraft();
    }

    public void CreateRoomFromDraft()
    {
        InitializeLocalPlayer();

        if (CurrentDraft == null)
        {
            ResetDraft();
        }

        int safeMaxPlayers = ClampInt(CurrentDraft.maxPlayers, MinMaxPlayers, MaxMaxPlayers);
        int safeTreasureCount = Math.Max(0, CurrentDraft.treasureCount);
        int safeMapIndex = Math.Max(0, CurrentDraft.selectedMapIndex);
        string safeRoomName = string.IsNullOrWhiteSpace(CurrentDraft.roomName)
            ? "Fun Match"
            : CurrentDraft.roomName.Trim();

        RoomState createdRoom = new RoomState
        {
            roomId = GenerateUniqueRoomId(),
            roomName = safeRoomName,
            isPublic = CurrentDraft.isPublic,
            maxPlayers = safeMaxPlayers,
            selectedMapIndex = safeMapIndex,
            treasureCount = safeTreasureCount,
            players = new List<PlayerState>
            {
                new PlayerState
                {
                    playerId = LocalPlayer.playerId,
                    displayName = LocalPlayer.displayName,
                    isReady = false,
                    isHost = true,
                    selectedColorIndex = LocalPlayer.selectedColorIndex
                }
            }
        };

        CurrentRoom = createdRoom;
        Rooms.Add(createdRoom);
    }

    public void ClearCurrentRoom()
    {
        CurrentRoom = null;
    }

    public void SetCurrentRoom(RoomState room)
    {
        CurrentRoom = room;
    }

    public void LeaveCurrentRoomAsLocalPlayer()
    {
        RoomState room = CurrentRoom;
        bool localWasHost = false;

        if (room != null && room.players != null && LocalPlayer != null)
        {
            string localPlayerId = LocalPlayer.playerId != null ? LocalPlayer.playerId.Trim() : string.Empty;
            if (!string.IsNullOrWhiteSpace(localPlayerId))
            {
                for (int i = room.players.Count - 1; i >= 0; i--)
                {
                    PlayerState player = room.players[i];
                    string playerId = player != null && player.playerId != null ? player.playerId.Trim() : string.Empty;
                    if (string.Equals(playerId, localPlayerId, StringComparison.Ordinal))
                    {
                        if (player != null && player.isHost)
                        {
                            localWasHost = true;
                        }

                        room.players.RemoveAt(i);
                    }
                }
            }
        }

        if (localWasHost && room != null && Rooms != null)
        {
            Rooms.Remove(room);
        }

        ClearCurrentRoom();
    }

    public bool TryJoinRoom(RoomState room)
    {
        if (room == null)
        {
            return false;
        }

        CurrentRoom = room;
        InitializeLocalPlayer();

        if (room.players == null)
        {
            room.players = new List<PlayerState>();
        }

        if (LocalPlayer == null)
        {
            return true;
        }

        string localPlayerId = LocalPlayer.playerId != null ? LocalPlayer.playerId.Trim() : string.Empty;
        if (string.IsNullOrWhiteSpace(localPlayerId))
        {
            LocalPlayer.EnsureDefaults();
            localPlayerId = LocalPlayer.playerId != null ? LocalPlayer.playerId.Trim() : string.Empty;
            if (string.IsNullOrWhiteSpace(localPlayerId))
            {
                return true;
            }
        }

        for (int i = 0; i < room.players.Count; i++)
        {
            PlayerState existingPlayer = room.players[i];
            if (existingPlayer == null)
            {
                continue;
            }

            string existingPlayerId = existingPlayer.playerId != null ? existingPlayer.playerId.Trim() : string.Empty;
            if (string.Equals(existingPlayerId, localPlayerId, StringComparison.Ordinal))
            {
                return true;
            }
        }

        int maxPlayers = Math.Max(1, room.maxPlayers);
        if (room.players.Count >= maxPlayers)
        {
            return false;
        }

        room.players.Add(new PlayerState
        {
            playerId = localPlayerId,
            displayName = LocalPlayer.displayName,
            selectedColorIndex = LocalPlayer.selectedColorIndex,
            isHost = false,
            isReady = false
        });

        return true;
    }

    public void SetLocalDisplayName(string value)
    {
        InitializeLocalPlayer();

        string trimmedInput = value != null ? value.Trim() : string.Empty;
        if (!string.IsNullOrWhiteSpace(trimmedInput))
        {
            LocalPlayer.displayName = trimmedInput;
            return;
        }

        string existingName = LocalPlayer.displayName != null ? LocalPlayer.displayName.Trim() : string.Empty;
        LocalPlayer.displayName = string.IsNullOrWhiteSpace(existingName)
            ? DefaultLocalDisplayName
            : existingName;
    }

    public void SetLocalSelectedColorIndex(int value)
    {
        InitializeLocalPlayer();
        LocalPlayer.selectedColorIndex = Math.Max(0, value);
    }

    public void SyncLocalPlayerFromAuth(AuthUser authUser)
    {
        InitializeLocalPlayer();

        if (authUser == null)
        {
            return;
        }

        string trimmedUserId = authUser.userId != null ? authUser.userId.Trim() : string.Empty;
        if (!string.IsNullOrWhiteSpace(trimmedUserId))
        {
            LocalPlayer.playerId = trimmedUserId;
        }

        string trimmedDisplayName = authUser.displayName != null ? authUser.displayName.Trim() : string.Empty;
        if (!string.IsNullOrWhiteSpace(trimmedDisplayName))
        {
            LocalPlayer.displayName = trimmedDisplayName;
        }
        else
        {
            string existingName = LocalPlayer.displayName != null ? LocalPlayer.displayName.Trim() : string.Empty;
            LocalPlayer.displayName = string.IsNullOrWhiteSpace(existingName)
                ? DefaultLocalDisplayName
                : existingName;
        }

        LocalPlayer.EnsureDefaults();
    }

    private void InitializeLocalPlayer()
    {
        if (LocalPlayer == null)
        {
            LocalPlayer = new LocalPlayerProfile();
        }

        LocalPlayer.EnsureDefaults();
    }

    private string GenerateUniqueRoomId()
    {
        for (int attempt = 0; attempt < 32; attempt++)
        {
            string candidate = roomIdRandom.Next(0, 1000000).ToString("D6");
            bool isDuplicate = false;
            for (int i = 0; i < Rooms.Count; i++)
            {
                if (Rooms[i] != null && Rooms[i].roomId == candidate)
                {
                    isDuplicate = true;
                    break;
                }
            }

            if (!isDuplicate)
            {
                return candidate;
            }
        }

        return DateTime.UtcNow.Ticks.ToString().Substring(0, 6);
    }

    private static int ClampInt(int value, int min, int max)
    {
        if (value < min)
        {
            return min;
        }

        if (value > max)
        {
            return max;
        }

        return value;
    }
}
