using System;

[Serializable]
public class LocalPlayerProfile
{
    public string playerId = "local_player";
    public string displayName = "You";
    public int selectedColorIndex = 0;

    public void EnsureDefaults()
    {
        if (string.IsNullOrWhiteSpace(playerId))
        {
            playerId = "local_player";
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = "You";
        }

        if (selectedColorIndex < 0)
        {
            selectedColorIndex = 0;
        }
    }
}
