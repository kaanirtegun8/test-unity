using System;
using System.Collections.Generic;

public class LobbyStateStore
{
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
            players = new List<PlayerState>()
        };
    }

    public void ClearCurrentRoom()
    {
        CurrentRoom = null;
    }
}
