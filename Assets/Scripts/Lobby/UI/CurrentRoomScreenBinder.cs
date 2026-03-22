using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CurrentRoomScreenBinder : MonoBehaviour
{
    private const int DefaultSlotCount = 4;

    private enum SlotVisualState
    {
        Occupied,
        Available,
        Locked
    }

    [SerializeField] private TMP_Text roomNameText;
    [SerializeField] private TMP_Text roomIdText;
    [SerializeField] private TMP_Text roomPlayerCountText;
    [SerializeField] private TMP_Text sidePlayerCountText;
    [SerializeField] private TMP_Text sideRewardText;
    [SerializeField] private Image sideMapPreview;
    [SerializeField] private GameObject[] playerSlots = new GameObject[DefaultSlotCount];
    [SerializeField] private Button playerSlot01StatusButton;
    [SerializeField] private Image playerSlot01StatusButtonImage;
    [SerializeField] private TMP_Text playerSlot01StatusText;
    [SerializeField] private Sprite hostReadyButtonSprite;
    [SerializeField] private Sprite hostNotReadyButtonSprite;
    [SerializeField] [Range(0f, 1f)] private float sideMapPreviewAlpha = 1f;
    [SerializeField] private string occupiedSlotStatusText = "Waiting...";
    [SerializeField] private string availableSlotStatusText = "Empty";
    [SerializeField] private string lockedSlotStatusText = "Locked";
    [SerializeField] private string hostReadyActionText = "Ready Up";
    [SerializeField] private string hostReadyStateText = "Cancel Ready";
    [SerializeField] private string otherPlayerReadyText = "Ready";
    [SerializeField] private string otherPlayerNotReadyText = "Not Ready";
    [SerializeField] [Range(0f, 1f)] private float availableSlotAlpha = 0.82f;
    [SerializeField] [Range(0f, 1f)] private float lockedSlotAlpha = 0.42f;
    [SerializeField] private Color hostReadyButtonTint = new Color(0.36f, 0.86f, 0.28f, 1f);
    [SerializeField] private Color hostNotReadyButtonTint = new Color(0.62f, 0.66f, 0.73f, 0.95f);
    [SerializeField] private Color emptyStateButtonTint = new Color(0.62f, 0.66f, 0.73f, 0.85f);
    [SerializeField] private Color lockedStateButtonTint = new Color(0.52f, 0.56f, 0.62f, 0.75f);

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
        BindUiEvents();
        ApplyCurrentRoomToUi();
    }

    private void OnEnable()
    {
        BindUiEvents();
        ApplyCurrentRoomToUi();
    }

    private void OnDestroy()
    {
        UnbindUiEvents();
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
        int safeMaxPlayers = Mathf.Clamp(
            currentRoom.maxPlayers,
            1,
            Mathf.Max(1, playerSlots != null ? playerSlots.Length : DefaultSlotCount));
        int safeTreasureCount = Mathf.Max(0, currentRoom.treasureCount);
        int playerCount = currentRoom.players != null ? currentRoom.players.Count : 0;
        int safePlayerCount = Mathf.Clamp(playerCount, 0, safeMaxPlayers);

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
            roomPlayerCountText.text = $"Players: {safePlayerCount}/{safeMaxPlayers}";
        }

        if (sidePlayerCountText != null)
        {
            sidePlayerCountText.text = safePlayerCount.ToString();
        }

        if (sideRewardText != null)
        {
            sideRewardText.text = $"x{safeTreasureCount}";
        }

        ApplyPlayerSlotsVisuals(currentRoom, safePlayerCount, safeMaxPlayers);
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
            sidePlayerCountText.text = "0";
        }

        if (sideRewardText != null)
        {
            sideRewardText.text = "x2";
        }

        ApplyPlayerSlotsVisuals(null, 0, DefaultSlotCount);
        ApplySideMapPreviewVisual(0);
    }

    private void ApplyPlayerSlotsVisuals(RoomState currentRoom, int playerCount, int maxPlayers)
    {
        if (playerSlots == null || playerSlots.Length == 0)
        {
            return;
        }

        PlayerState localHostPlayer = FindLocalHostPlayer(currentRoom);
        int safeMaxPlayers = Mathf.Clamp(maxPlayers, 0, playerSlots.Length);
        int safePlayerCount = Mathf.Clamp(playerCount, 0, safeMaxPlayers);

        for (int i = 0; i < playerSlots.Length; i++)
        {
            GameObject slot = playerSlots[i];
            if (slot == null)
            {
                continue;
            }

            PlayerState slotPlayer = GetPlayerForSlot(currentRoom, localHostPlayer, i);

            if (i == 0 && safePlayerCount > 0 && localHostPlayer != null)
            {
                ApplyLocalHostSlotVisual(slot, localHostPlayer.isReady);
            }
            else if (i < safePlayerCount)
            {
                ApplyOtherPlayerSlotVisual(slot, slotPlayer != null && slotPlayer.isReady);
            }
            else if (i < safeMaxPlayers)
            {
                ApplySlotVisualState(slot, SlotVisualState.Available);
            }
            else
            {
                ApplySlotVisualState(slot, SlotVisualState.Locked);
            }
        }
    }

    private static PlayerState GetPlayerForSlot(RoomState currentRoom, PlayerState localHostPlayer, int slotIndex)
    {
        if (currentRoom == null || currentRoom.players == null || slotIndex < 0)
        {
            return null;
        }

        if (localHostPlayer == null)
        {
            return slotIndex < currentRoom.players.Count ? currentRoom.players[slotIndex] : null;
        }

        if (slotIndex == 0)
        {
            return localHostPlayer;
        }

        int targetNonHostIndex = slotIndex - 1;
        int currentNonHostIndex = 0;
        for (int i = 0; i < currentRoom.players.Count; i++)
        {
            PlayerState candidate = currentRoom.players[i];
            if (candidate == null || candidate.isHost)
            {
                continue;
            }

            if (currentNonHostIndex == targetNonHostIndex)
            {
                return candidate;
            }

            currentNonHostIndex++;
        }

        return null;
    }

    private void ApplyLocalHostSlotVisual(GameObject slot, bool isReady)
    {
        if (slot == null)
        {
            return;
        }

        Image slotBackground = slot.GetComponent<Image>();
        Transform avatarTransform = slot.transform.Find("AvatarPlaceholder");
        Image avatarImage = avatarTransform != null ? avatarTransform.GetComponent<Image>() : null;
        Transform statusTransform = slot.transform.Find("StatusButton");
        Button statusButton = statusTransform != null ? statusTransform.GetComponent<Button>() : null;
        Image statusButtonImage = statusTransform != null ? statusTransform.GetComponent<Image>() : null;
        TMP_Text statusText = null;
        if (statusTransform != null)
        {
            Transform labelTransform = statusTransform.Find("Label");
            if (labelTransform != null)
            {
                statusText = labelTransform.GetComponent<TMP_Text>();
            }
        }

        SetGraphicAlpha(slotBackground, 1f);
        SetGraphicAlpha(avatarImage, 1f);
        SetGraphicAlpha(statusButtonImage, 1f);
        SetText(statusText, isReady ? hostReadyStateText : hostReadyActionText, isReady ? 1f : 0.9f);

        EnsureHostStatusSprites();
        if (statusButtonImage != null)
        {
            Sprite targetSprite = isReady ? hostReadyButtonSprite : hostNotReadyButtonSprite;
            if (targetSprite != null)
            {
                statusButtonImage.sprite = targetSprite;
                statusButtonImage.overrideSprite = targetSprite;
            }

            statusButtonImage.color = isReady ? hostReadyButtonTint : hostNotReadyButtonTint;
        }

        if (statusButton != null)
        {
            statusButton.interactable = true;
        }
    }

    private void ApplyOtherPlayerSlotVisual(GameObject slot, bool isReady)
    {
        if (slot == null)
        {
            return;
        }

        Image slotBackground = slot.GetComponent<Image>();
        Transform avatarTransform = slot.transform.Find("AvatarPlaceholder");
        Image avatarImage = avatarTransform != null ? avatarTransform.GetComponent<Image>() : null;
        Transform statusTransform = slot.transform.Find("StatusButton");
        Button statusButton = statusTransform != null ? statusTransform.GetComponent<Button>() : null;
        Image statusButtonImage = statusTransform != null ? statusTransform.GetComponent<Image>() : null;
        TMP_Text statusText = null;
        if (statusTransform != null)
        {
            Transform labelTransform = statusTransform.Find("Label");
            if (labelTransform != null)
            {
                statusText = labelTransform.GetComponent<TMP_Text>();
            }
        }

        SetGraphicAlpha(slotBackground, 1f);
        SetGraphicAlpha(avatarImage, 1f);
        SetGraphicAlpha(statusButtonImage, 1f);
        SetText(statusText, isReady ? otherPlayerReadyText : otherPlayerNotReadyText, 0.95f);

        EnsureHostStatusSprites();
        if (statusButtonImage != null)
        {
            Sprite targetSprite = isReady ? hostReadyButtonSprite : hostNotReadyButtonSprite;
            if (targetSprite != null)
            {
                statusButtonImage.sprite = targetSprite;
                statusButtonImage.overrideSprite = targetSprite;
            }

            statusButtonImage.color = isReady ? hostReadyButtonTint : hostNotReadyButtonTint;
        }

        if (statusButton != null)
        {
            statusButton.interactable = false;
        }
    }

    private void ApplySlotVisualState(GameObject slot, SlotVisualState state)
    {
        if (slot == null)
        {
            return;
        }

        Image slotBackground = slot.GetComponent<Image>();
        Transform avatarTransform = slot.transform.Find("AvatarPlaceholder");
        Image avatarImage = avatarTransform != null ? avatarTransform.GetComponent<Image>() : null;
        Transform statusTransform = slot.transform.Find("StatusButton");
        Button statusButton = statusTransform != null ? statusTransform.GetComponent<Button>() : null;
        Image statusButtonImage = statusTransform != null ? statusTransform.GetComponent<Image>() : null;
        TMP_Text statusText = null;
        if (statusTransform != null)
        {
            Transform labelTransform = statusTransform.Find("Label");
            if (labelTransform != null)
            {
                statusText = labelTransform.GetComponent<TMP_Text>();
            }
        }

        switch (state)
        {
            case SlotVisualState.Occupied:
                SetGraphicAlpha(slotBackground, 1f);
                SetGraphicAlpha(avatarImage, 1f);
                SetGraphicAlpha(statusButtonImage, 1f);
                SetText(statusText, occupiedSlotStatusText, 1f);
                EnsureHostStatusSprites();
                if (statusButtonImage != null && hostNotReadyButtonSprite != null)
                {
                    statusButtonImage.sprite = hostNotReadyButtonSprite;
                    statusButtonImage.overrideSprite = hostNotReadyButtonSprite;
                    statusButtonImage.color = hostNotReadyButtonTint;
                }
                if (statusButton != null)
                {
                    statusButton.interactable = false;
                }
                break;
            case SlotVisualState.Available:
                SetGraphicAlpha(slotBackground, availableSlotAlpha);
                SetGraphicAlpha(avatarImage, availableSlotAlpha);
                SetGraphicAlpha(statusButtonImage, availableSlotAlpha);
                SetText(statusText, availableSlotStatusText, availableSlotAlpha);
                EnsureHostStatusSprites();
                if (statusButtonImage != null && hostNotReadyButtonSprite != null)
                {
                    statusButtonImage.sprite = hostNotReadyButtonSprite;
                    statusButtonImage.overrideSprite = hostNotReadyButtonSprite;
                    statusButtonImage.color = emptyStateButtonTint;
                }
                if (statusButton != null)
                {
                    statusButton.interactable = false;
                }
                break;
            default:
                SetGraphicAlpha(slotBackground, lockedSlotAlpha);
                SetGraphicAlpha(avatarImage, lockedSlotAlpha);
                SetGraphicAlpha(statusButtonImage, lockedSlotAlpha);
                SetText(statusText, lockedSlotStatusText, lockedSlotAlpha);
                EnsureHostStatusSprites();
                if (statusButtonImage != null && hostNotReadyButtonSprite != null)
                {
                    statusButtonImage.sprite = hostNotReadyButtonSprite;
                    statusButtonImage.overrideSprite = hostNotReadyButtonSprite;
                    statusButtonImage.color = lockedStateButtonTint;
                }
                if (statusButton != null)
                {
                    statusButton.interactable = false;
                }
                break;
        }
    }

    private static void SetText(TMP_Text text, string value, float alpha)
    {
        if (text == null)
        {
            return;
        }

        text.text = value;
        Color color = text.color;
        color.a = Mathf.Clamp01(alpha);
        text.color = color;
    }

    private static void SetGraphicAlpha(Graphic graphic, float alpha)
    {
        if (graphic == null)
        {
            return;
        }

        Color color = graphic.color;
        color.a = Mathf.Clamp01(alpha);
        graphic.color = color;
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

    private void BindUiEvents()
    {
        if (playerSlot01StatusButton != null)
        {
            playerSlot01StatusButton.onClick.RemoveListener(OnPlayerSlot01StatusClicked);
            playerSlot01StatusButton.onClick.AddListener(OnPlayerSlot01StatusClicked);
        }
    }

    private void UnbindUiEvents()
    {
        if (playerSlot01StatusButton != null)
        {
            playerSlot01StatusButton.onClick.RemoveListener(OnPlayerSlot01StatusClicked);
        }
    }

    private void OnPlayerSlot01StatusClicked()
    {
        RoomState currentRoom = SharedStore.CurrentRoom;
        PlayerState localHostPlayer = FindLocalHostPlayer(currentRoom);
        if (currentRoom == null || localHostPlayer == null)
        {
            return;
        }

        localHostPlayer.isReady = !localHostPlayer.isReady;
        ApplyCurrentRoomToUi();
    }

    private static PlayerState FindLocalHostPlayer(RoomState currentRoom)
    {
        if (currentRoom == null || currentRoom.players == null)
        {
            return null;
        }

        for (int i = 0; i < currentRoom.players.Count; i++)
        {
            PlayerState player = currentRoom.players[i];
            if (player != null && player.isHost)
            {
                return player;
            }
        }

        return null;
    }

    private void AutoAssignReferences()
    {
        Transform[] allTransforms = transform.GetComponentsInChildren<Transform>(true);

        if (playerSlots == null || playerSlots.Length != DefaultSlotCount)
        {
            playerSlots = new GameObject[DefaultSlotCount];
        }

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

        AssignSlotByName(allTransforms, "PlayerSlot01", 0);
        AssignSlotByName(allTransforms, "PlayerSlot02", 1);
        AssignSlotByName(allTransforms, "PlayerSlot03", 2);
        AssignSlotByName(allTransforms, "PlayerSlot04", 3);

        if (playerSlot01StatusButton == null && playerSlots[0] != null)
        {
            Transform statusTransform = playerSlots[0].transform.Find("StatusButton");
            if (statusTransform != null)
            {
                playerSlot01StatusButton = statusTransform.GetComponent<Button>();
            }
        }

        if (playerSlot01StatusButtonImage == null && playerSlot01StatusButton != null)
        {
            playerSlot01StatusButtonImage = playerSlot01StatusButton.GetComponent<Image>();
        }

        if (playerSlot01StatusText == null && playerSlot01StatusButton != null)
        {
            Transform labelTransform = playerSlot01StatusButton.transform.Find("Label");
            if (labelTransform != null)
            {
                playerSlot01StatusText = labelTransform.GetComponent<TMP_Text>();
            }
        }

        EnsureHostStatusSprites();
    }

    private void EnsureHostStatusSprites()
    {
        if (hostReadyButtonSprite == null && playerSlot01StatusButtonImage != null)
        {
            hostReadyButtonSprite = playerSlot01StatusButtonImage.sprite;
        }

        if (hostNotReadyButtonSprite == null)
        {
            Image preferredPassiveImage = GetSlotStatusButtonImage(DefaultSlotCount - 1);
            if (preferredPassiveImage != null && preferredPassiveImage.sprite != null)
            {
                hostNotReadyButtonSprite = preferredPassiveImage.sprite;
            }
        }

        if (hostNotReadyButtonSprite == null)
        {
            for (int i = 1; i < DefaultSlotCount; i++)
            {
                Image candidateImage = GetSlotStatusButtonImage(i);
                if (candidateImage != null && candidateImage.sprite != null)
                {
                    hostNotReadyButtonSprite = candidateImage.sprite;
                    break;
                }
            }
        }

        if (hostNotReadyButtonSprite == null)
        {
            hostNotReadyButtonSprite = hostReadyButtonSprite;
        }
    }

    private Image GetSlotStatusButtonImage(int slotIndex)
    {
        if (playerSlots == null || slotIndex < 0 || slotIndex >= playerSlots.Length || playerSlots[slotIndex] == null)
        {
            return null;
        }

        Transform statusTransform = playerSlots[slotIndex].transform.Find("StatusButton");
        return statusTransform != null ? statusTransform.GetComponent<Image>() : null;
    }

    private void AssignSlotByName(Transform[] allTransforms, string slotName, int index)
    {
        if (playerSlots == null || index < 0 || index >= playerSlots.Length)
        {
            return;
        }

        if (playerSlots[index] != null)
        {
            return;
        }

        playerSlots[index] = FindGameObjectByName(allTransforms, slotName);
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

    private static GameObject FindGameObjectByName(Transform[] allTransforms, string targetName)
    {
        for (int i = 0; i < allTransforms.Length; i++)
        {
            if (allTransforms[i].name == targetName)
            {
                return allTransforms[i].gameObject;
            }
        }

        return null;
    }
}
