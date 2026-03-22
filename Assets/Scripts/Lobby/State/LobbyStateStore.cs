using System;
using System.Collections.Generic;

public class LobbyStateStore
{
    public static LobbyStateStore Local { get; } = new LobbyStateStore();

    private readonly Random roomIdRandom = new Random();

    public RoomDraft CurrentDraft { get; private set; }
    public RoomState CurrentRoom { get; private set; }

    public LobbyStateStore()
    {
        ResetDraft();
    }

    public void ResetDraft()
    {
        CurrentDraft = new RoomDraft();
    }

    public void CreateRoomFromDraft()
    {
        if (CurrentDraft == null)
        {
            ResetDraft();
        }

        CurrentRoom = new RoomState
        {
            roomId = roomIdRandom.Next(0, 1000000).ToString("D6"),
            roomName = CurrentDraft.roomName,
            isPublic = CurrentDraft.isPublic,
            maxPlayers = CurrentDraft.maxPlayers,
            selectedMapIndex = CurrentDraft.selectedMapIndex,
            treasureCount = CurrentDraft.treasureCount,
            players = new List<PlayerState>
            {
                new PlayerState
                {
                    playerId = "local_player",
                    displayName = "You",
                    isReady = false,
                    isHost = true,
                    selectedColorIndex = 0
                }
            }
        };
    }

    public void ClearCurrentRoom()
    {
        CurrentRoom = null;
    }
}
