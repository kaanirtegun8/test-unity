using System;

[Serializable]
public class RoomDraft
{
    public string roomName = "Fun Match";
    public bool isPublic = true;
    public int maxPlayers = 4;
    public int selectedMapIndex = 0;
    public int treasureCount = 2;
}
