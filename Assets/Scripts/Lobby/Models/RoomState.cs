using System;
using System.Collections.Generic;

[Serializable]
public class RoomState
{
    public const string PhaseCurrentRoom = "CurrentRoom";
    public const string PhaseMiniGameReady = "MiniGameReady";

    public string roomId;
    public string roomName;
    public bool isPublic;
    public int maxPlayers;
    public int selectedMapIndex;
    public int treasureCount;
    public string currentPhase = PhaseCurrentRoom;
    public List<PlayerState> players = new List<PlayerState>();
}
