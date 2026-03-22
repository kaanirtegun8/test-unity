using TMPro;
using UnityEngine;

public class CurrentRoomScreenBinder : MonoBehaviour
{
    [SerializeField] private TMP_Text roomNameText;
    [SerializeField] private TMP_Text roomIdText;
    [SerializeField] private TMP_Text roomPlayerCountText;

    private LobbyStateStore SharedStore => LobbyStateStore.Local;

    private void Awake()
    {
        AutoAssignReferences();
        ApplyCurrentRoomToUi();
    }

    private void OnEnable()
    {
        ApplyCurrentRoomToUi();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        AutoAssignReferences();
    }
#endif

    private void ApplyCurrentRoomToUi()
    {
        RoomState currentRoom = SharedStore.CurrentRoom;
        if (currentRoom == null)
        {
            return;
        }

        string safeRoomName = string.IsNullOrWhiteSpace(currentRoom.roomName) ? "Fun Match" : currentRoom.roomName;
        string safeRoomId = string.IsNullOrWhiteSpace(currentRoom.roomId) ? "000000" : currentRoom.roomId;

        if (roomNameText != null)
        {
            roomNameText.text = $"Room: {safeRoomName}";
        }

        if (roomIdText != null)
        {
            roomIdText.text = $"ID: {safeRoomId}";
        }

        if (roomPlayerCountText != null)
        {
            int playerCount = currentRoom.players != null ? currentRoom.players.Count : 0;
            int maxPlayers = Mathf.Clamp(currentRoom.maxPlayers, 2, 4);
            roomPlayerCountText.text = $"Players: {playerCount}/{maxPlayers}";
        }
    }

    private void AutoAssignReferences()
    {
        Transform[] allTransforms = transform.GetComponentsInChildren<Transform>(true);

        if (roomNameText == null)
        {
            roomNameText = FindTextByName(allTransforms, "RoomNameText");
        }

        if (roomIdText == null)
        {
            roomIdText = FindTextByName(allTransforms, "RoomIdText");
        }

        if (roomPlayerCountText == null)
        {
            roomPlayerCountText = FindTextByName(allTransforms, "RoomPlayerCountText");
        }
    }

    private static TMP_Text FindTextByName(Transform[] allTransforms, string targetName)
    {
        for (int i = 0; i < allTransforms.Length; i++)
        {
            if (allTransforms[i].name == targetName)
            {
                return allTransforms[i].GetComponent<TMP_Text>();
            }
        }

        return null;
    }
}
