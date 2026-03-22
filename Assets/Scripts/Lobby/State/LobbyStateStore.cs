using System;
using System.Collections.Generic;

public class LobbyStateStore
{
    public static LobbyStateStore Local { get; } = new LobbyStateStore();

    private readonly Random roomIdRandom = new Random();
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
