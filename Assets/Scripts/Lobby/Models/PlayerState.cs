using System;

[Serializable]
public class PlayerState
{
    public string playerId;
    public string displayName;
    public bool isReady;
    public bool isConfirmed;
    public bool isHost;
    public int selectedColorIndex;
}
