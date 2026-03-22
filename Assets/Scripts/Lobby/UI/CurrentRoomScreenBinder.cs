using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CurrentRoomScreenBinder : MonoBehaviour
{
    [SerializeField] private TMP_Text roomNameText;
    [SerializeField] private TMP_Text roomIdText;
    [SerializeField] private TMP_Text roomPlayerCountText;
    [SerializeField] private TMP_Text sidePlayerCountText;
    [SerializeField] private TMP_Text sideRewardText;
    [SerializeField] private Image sideMapPreview;
    [SerializeField] [Range(0f, 1f)] private float sideMapPreviewAlpha = 1f;

    private static readonly Color[] SideMapPreviewPalette =
    {
        new Color(0.16f, 0.55f, 0.76f, 1f),
        new Color(0.34f, 0.63f, 0.36f, 1f),
        new Color(0.15f, 0.72f, 0.58f, 1f),
        new Color(0.12f, 0.57f, 0.77f, 1f),
        new Color(0.17f, 0.52f, 0.70f, 1f),
        new Color(0.21f, 0.73f, 0.43f, 1f),
        new Color(0.16f, 0.61f, 0.80f, 1f),
        new Color(0.29f, 0.68f, 0.41f, 1f),
        new Color(0.14f, 0.51f, 0.75f, 1f),
        new Color(0.20f, 0.66f, 0.50f, 1f)
    };

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
            ApplyFallbackUi();
            return;
        }

        string safeRoomName = string.IsNullOrWhiteSpace(currentRoom.roomName) ? "Fun Match" : currentRoom.roomName;
        string safeRoomId = string.IsNullOrWhiteSpace(currentRoom.roomId) ? "000000" : currentRoom.roomId;
        int safeMaxPlayers = Mathf.Max(1, currentRoom.maxPlayers);
        int safeTreasureCount = Mathf.Max(0, currentRoom.treasureCount);
        int playerCount = currentRoom.players != null ? currentRoom.players.Count : 0;

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
            roomPlayerCountText.text = $"Players: {playerCount}/{safeMaxPlayers}";
        }

        if (sidePlayerCountText != null)
        {
            sidePlayerCountText.text = safeMaxPlayers.ToString();
        }

        if (sideRewardText != null)
        {
            sideRewardText.text = $"x{safeTreasureCount}";
        }

        ApplySideMapPreviewVisual(currentRoom.selectedMapIndex);
    }

    private void ApplyFallbackUi()
    {
        if (roomNameText != null)
        {
            roomNameText.text = "Room: Fun Match";
        }

        if (roomIdText != null)
        {
            roomIdText.text = "ID: 000000";
        }

        if (roomPlayerCountText != null)
        {
            roomPlayerCountText.text = "Players: 0/4";
        }

        if (sidePlayerCountText != null)
        {
            sidePlayerCountText.text = "4";
        }

        if (sideRewardText != null)
        {
            sideRewardText.text = "x2";
        }

        ApplySideMapPreviewVisual(0);
    }

    private void ApplySideMapPreviewVisual(int selectedMapIndex)
    {
        if (sideMapPreview == null || SideMapPreviewPalette.Length == 0)
        {
            return;
        }

        int safeIndex = Mathf.Abs(selectedMapIndex) % SideMapPreviewPalette.Length;
        Color previewColor = SideMapPreviewPalette[safeIndex];
        previewColor.a = Mathf.Clamp01(sideMapPreviewAlpha);
        sideMapPreview.color = previewColor;
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

        if (sidePlayerCountText == null)
        {
            sidePlayerCountText = FindTextByName(allTransforms, "SidePlayerCountText");
        }

        if (sideRewardText == null)
        {
            sideRewardText = FindTextByName(allTransforms, "SideRewardText");
        }

        if (sideMapPreview == null)
        {
            sideMapPreview = FindImageByName(allTransforms, "SideMapPreview");
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

    private static Image FindImageByName(Transform[] allTransforms, string targetName)
    {
        for (int i = 0; i < allTransforms.Length; i++)
        {
            if (allTransforms[i].name == targetName)
            {
                return allTransforms[i].GetComponent<Image>();
            }
        }

        return null;
    }
}
