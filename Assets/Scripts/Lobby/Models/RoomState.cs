using System;
using System.Collections.Generic;

[Serializable]
public class RoomState
{
    public string roomId;
    public string roomName;
    public bool isPublic;
    public int maxPlayers;
    public int selectedMapIndex;
    public int treasureCount;
    public List<PlayerState> players = new List<PlayerState>();
}
