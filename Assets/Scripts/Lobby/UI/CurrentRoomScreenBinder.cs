using System.Collections;
using System.Collections.Generic;
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
    [SerializeField] private Button copyRoomIdButton;
    [SerializeField] private Button startGameButton;
    [SerializeField] private Image startGameButtonImage;
    [SerializeField] private TMP_Text startGameButtonText;
    [SerializeField] private TMP_Text roomPlayerCountText;
    [SerializeField] private TMP_Text sidePlayerCountText;
    [SerializeField] private TMP_Text sideRewardText;
    [SerializeField] private Image sideMapPreview;
    [SerializeField] private GameObject[] playerSlots = new GameObject[DefaultSlotCount];
    [SerializeField] private Button playerSlot01StatusButton;
    [SerializeField] private Image playerSlot01StatusButtonImage;
    [SerializeField] private TMP_Text playerSlot01StatusText;
    [SerializeField] private TMP_Text playerSlot01NameText;
    [SerializeField] private TMP_Text playerSlot01HostBadgeText;
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
    [SerializeField] private Color startGameEnabledTint = Color.white;
    [SerializeField] private Color startGameDisabledTint = new Color(0.52f, 0.56f, 0.62f, 0.92f);
    [SerializeField] private Color startGameEnabledTextColor = Color.white;
    [SerializeField] private Color startGameDisabledTextColor = new Color(0.84f, 0.88f, 0.94f, 0.95f);
    [SerializeField] private string localPlayerFallbackName = "Player";
    [SerializeField] private string localPlayerHostBadgeLabel = "Host";
    [SerializeField] private Color localPlayerNameColor = Color.white;
    [SerializeField] private Color localPlayerHostBadgeColor = new Color(0.77f, 0.92f, 0.56f, 1f);
    [SerializeField] private string copyTooltipMessage = "Copied";
    [SerializeField] private float copyTooltipDuration = 1.2f;
    [SerializeField] private Vector2 copyTooltipSize = new Vector2(120f, 34f);
    [SerializeField] private Color copyTooltipBackgroundColor = new Color(0.19f, 0.70f, 0.29f, 0.96f);
    [SerializeField] private Color copyTooltipTextColor = Color.white;

    private GameObject copyTooltipObject;
    private TMP_Text copyTooltipText;
    private Coroutine copyTooltipHideRoutine;

    private static readonly Color[] SideMapPreviewPalette =
    {
        new Color(0.17f, 0.58f, 0.85f, 1f),
        new Color(0.32f, 0.70f, 0.33f, 1f),
        new Color(0.82f, 0.56f, 0.20f, 1f),
        new Color(0.48f, 0.36f, 0.80f, 1f),
        new Color(0.80f, 0.33f, 0.53f, 1f),
        new Color(0.20f, 0.69f, 0.63f, 1f),
        new Color(0.77f, 0.32f, 0.32f, 1f),
        new Color(0.56f, 0.66f, 0.24f, 1f),
        new Color(0.25f, 0.44f, 0.82f, 1f),
        new Color(0.76f, 0.49f, 0.24f, 1f)
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
        AutoAssignReferences();
        BindUiEvents();
        ApplyCurrentRoomToUi();
    }

    private void Start()
    {
        // Ensure first visible frame uses CurrentRoom-driven state
        // even if another presenter touched button interactable in OnEnable.
        ApplyCurrentRoomToUi();
    }

    private void OnDestroy()
    {
        UnbindUiEvents();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        AutoAssignReferences(false);
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
        int rawPlayerCount = currentRoom.players != null ? currentRoom.players.Count : 0;
        int safePlayerCount = Mathf.Max(0, rawPlayerCount);
        int clampedPlayerCount = Mathf.Clamp(safePlayerCount, 0, safeMaxPlayers);

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

        ApplyStartGameButtonState(safePlayerCount);
        ApplyPlayerSlotsVisuals(currentRoom, clampedPlayerCount, safeMaxPlayers);
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
            sideRewardText.text = "x0";
        }

        ApplyStartGameButtonState(0);
        ApplyPlayerSlotsVisuals(null, 0, DefaultSlotCount);
        ApplySideMapPreviewVisual(0);
    }

    private void ApplyStartGameButtonState(int playerCount)
    {
        if (startGameButton == null)
        {
            return;
        }

        bool canStart = playerCount >= 2;
        startGameButton.interactable = canStart;

        if (startGameButtonImage == null)
        {
            startGameButtonImage = startGameButton.GetComponent<Image>();
        }

        if (startGameButtonText == null)
        {
            startGameButtonText = startGameButton.GetComponentInChildren<TMP_Text>(true);
        }

        ColorBlock colors = startGameButton.colors;
        colors.normalColor = startGameEnabledTint;
        colors.highlightedColor = startGameEnabledTint;
        colors.pressedColor = new Color(
            Mathf.Clamp01(startGameEnabledTint.r * 0.9f),
            Mathf.Clamp01(startGameEnabledTint.g * 0.9f),
            Mathf.Clamp01(startGameEnabledTint.b * 0.9f),
            startGameEnabledTint.a);
        colors.selectedColor = startGameEnabledTint;
        colors.disabledColor = startGameDisabledTint;
        startGameButton.colors = colors;

        if (startGameButtonImage != null)
        {
            startGameButtonImage.color = canStart ? startGameEnabledTint : startGameDisabledTint;
        }

        if (startGameButtonText != null)
        {
            startGameButtonText.color = canStart ? startGameEnabledTextColor : startGameDisabledTextColor;
        }
    }

    private void ApplyPlayerSlotsVisuals(RoomState currentRoom, int playerCount, int maxPlayers)
    {
        if (playerSlots == null || playerSlots.Length == 0)
        {
            return;
        }

        string localPlayerId = SharedStore.LocalPlayer != null ? SharedStore.LocalPlayer.playerId : string.Empty;
        PlayerState localPlayer = FindLocalPlayer(currentRoom, localPlayerId);
        int safeMaxPlayers = Mathf.Clamp(maxPlayers, 0, playerSlots.Length);
        List<PlayerState> slotPlayers = BuildSlotPlayers(currentRoom, localPlayer, safeMaxPlayers);
        int occupiedSlotCount = Mathf.Clamp(slotPlayers.Count, 0, safeMaxPlayers);

        for (int i = 0; i < playerSlots.Length; i++)
        {
            GameObject slot = playerSlots[i];
            if (slot == null)
            {
                continue;
            }

            if (i == 0)
            {
                ClearLocalPlayerIdentity();
            }

            if (i < occupiedSlotCount)
            {
                PlayerState slotPlayer = slotPlayers[i];
                bool isLocalSlot = localPlayer != null && AreSamePlayer(slotPlayer, localPlayer);
                if (i == 0 && isLocalSlot)
                {
                    ApplyLocalPlayerSlotVisual(slot, slotPlayer);
                }
                else
                {
                    ApplyOtherPlayerSlotVisual(slot, slotPlayer != null && slotPlayer.isReady);
                }
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

    private static List<PlayerState> BuildSlotPlayers(RoomState currentRoom, PlayerState localPlayer, int maxSlots)
    {
        List<PlayerState> slotPlayers = new List<PlayerState>();
        if (currentRoom == null || currentRoom.players == null || maxSlots <= 0)
        {
            return slotPlayers;
        }

        if (localPlayer != null)
        {
            slotPlayers.Add(localPlayer);
        }

        for (int i = 0; i < currentRoom.players.Count; i++)
        {
            PlayerState candidate = currentRoom.players[i];
            if (candidate == null)
            {
                continue;
            }

            if (localPlayer != null && AreSamePlayer(candidate, localPlayer))
            {
                continue;
            }

            slotPlayers.Add(candidate);
            if (slotPlayers.Count >= maxSlots)
            {
                break;
            }
        }

        return slotPlayers;
    }

    private static bool AreSamePlayer(PlayerState first, PlayerState second)
    {
        if (first == null || second == null)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(first.playerId) && !string.IsNullOrWhiteSpace(second.playerId))
        {
            return first.playerId == second.playerId;
        }

        return ReferenceEquals(first, second);
    }

    private void ApplyLocalPlayerSlotVisual(GameObject slot, PlayerState localPlayer)
    {
        if (slot == null)
        {
            return;
        }

        bool isReady = localPlayer != null && localPlayer.isReady;

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

        ApplyLocalPlayerIdentity(localPlayer);
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

        Outline previewOutline = sideMapPreview.GetComponent<Outline>();
        if (previewOutline == null)
        {
            previewOutline = sideMapPreview.gameObject.AddComponent<Outline>();
        }

        previewOutline.effectDistance = new Vector2(2f, -2f);
        previewOutline.useGraphicAlpha = true;
        previewOutline.effectColor = new Color(
            Mathf.Clamp01(previewColor.r * 0.45f),
            Mathf.Clamp01(previewColor.g * 0.45f),
            Mathf.Clamp01(previewColor.b * 0.45f),
            0.95f);
    }

    private void BindUiEvents()
    {
        if (playerSlot01StatusButton != null)
        {
            playerSlot01StatusButton.onClick.RemoveListener(OnPlayerSlot01StatusClicked);
            playerSlot01StatusButton.onClick.AddListener(OnPlayerSlot01StatusClicked);
        }

        if (copyRoomIdButton != null)
        {
            copyRoomIdButton.onClick.RemoveListener(OnCopyRoomIdButtonClicked);
            copyRoomIdButton.onClick.AddListener(OnCopyRoomIdButtonClicked);
        }
    }

    private void UnbindUiEvents()
    {
        if (playerSlot01StatusButton != null)
        {
            playerSlot01StatusButton.onClick.RemoveListener(OnPlayerSlot01StatusClicked);
        }

        if (copyRoomIdButton != null)
        {
            copyRoomIdButton.onClick.RemoveListener(OnCopyRoomIdButtonClicked);
        }
    }

    private void OnPlayerSlot01StatusClicked()
    {
        RoomState currentRoom = SharedStore.CurrentRoom;
        string localPlayerId = SharedStore.LocalPlayer != null ? SharedStore.LocalPlayer.playerId : string.Empty;
        PlayerState localPlayer = FindLocalPlayer(currentRoom, localPlayerId);
        if (currentRoom == null || localPlayer == null)
        {
            return;
        }

        localPlayer.isReady = !localPlayer.isReady;
        ApplyCurrentRoomToUi();
    }

    private void OnCopyRoomIdButtonClicked()
    {
        RoomState currentRoom = SharedStore.CurrentRoom;
        if (currentRoom == null || string.IsNullOrWhiteSpace(currentRoom.roomId))
        {
            return;
        }

        GUIUtility.systemCopyBuffer = currentRoom.roomId;
        ShowCopyTooltip();
    }

    private void ShowCopyTooltip()
    {
        EnsureCopyTooltip();
        if (copyTooltipObject == null || copyTooltipText == null)
        {
            return;
        }

        copyTooltipText.text = string.IsNullOrWhiteSpace(copyTooltipMessage) ? "Copied" : copyTooltipMessage;
        copyTooltipObject.SetActive(true);

        if (copyTooltipHideRoutine != null)
        {
            StopCoroutine(copyTooltipHideRoutine);
        }

        copyTooltipHideRoutine = StartCoroutine(HideCopyTooltipAfterDelay());
    }

    private IEnumerator HideCopyTooltipAfterDelay()
    {
        yield return new WaitForSeconds(Mathf.Max(0.2f, copyTooltipDuration));

        if (copyTooltipObject != null)
        {
            copyTooltipObject.SetActive(false);
        }

        copyTooltipHideRoutine = null;
    }

    private void EnsureCopyTooltip()
    {
        if (copyTooltipObject != null || copyRoomIdButton == null)
        {
            return;
        }

        GameObject tooltipRoot = new GameObject("CopyTooltip", typeof(RectTransform), typeof(Image));
        tooltipRoot.transform.SetParent(copyRoomIdButton.transform, false);

        RectTransform tooltipRect = tooltipRoot.GetComponent<RectTransform>();
        tooltipRect.anchorMin = new Vector2(1f, 0.5f);
        tooltipRect.anchorMax = new Vector2(1f, 0.5f);
        tooltipRect.pivot = new Vector2(0f, 0.5f);
        tooltipRect.anchoredPosition = new Vector2(10f, 0f);
        tooltipRect.sizeDelta = copyTooltipSize;

        Image tooltipBackground = tooltipRoot.GetComponent<Image>();
        tooltipBackground.color = copyTooltipBackgroundColor;
        tooltipBackground.raycastTarget = false;
        tooltipBackground.type = Image.Type.Sliced;

        Image copyButtonImage = copyRoomIdButton.GetComponent<Image>();
        if (copyButtonImage != null && copyButtonImage.sprite != null)
        {
            tooltipBackground.sprite = copyButtonImage.sprite;
        }

        Shadow backgroundShadow = tooltipRoot.AddComponent<Shadow>();
        backgroundShadow.effectColor = new Color(0f, 0f, 0f, 0.35f);
        backgroundShadow.effectDistance = new Vector2(1.5f, -1.5f);
        backgroundShadow.useGraphicAlpha = true;

        GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(tooltipRoot.transform, false);

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(6f, 2f);
        labelRect.offsetMax = new Vector2(-6f, -2f);

        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.alignment = TextAlignmentOptions.Center;
        label.text = copyTooltipMessage;
        label.fontSize = roomIdText != null ? roomIdText.fontSize : 14f;
        label.color = copyTooltipTextColor;
        label.raycastTarget = false;
        label.enableWordWrapping = false;
        label.overflowMode = TextOverflowModes.Overflow;
        if (roomIdText != null && roomIdText.font != null)
        {
            label.font = roomIdText.font;
        }

        Outline labelOutline = labelObject.AddComponent<Outline>();
        labelOutline.effectColor = new Color(0f, 0f, 0f, 0.35f);
        labelOutline.effectDistance = new Vector2(1f, -1f);
        labelOutline.useGraphicAlpha = true;

        tooltipRoot.SetActive(false);
        copyTooltipObject = tooltipRoot;
        copyTooltipText = label;
    }

    private static PlayerState FindLocalPlayer(RoomState currentRoom, string localPlayerId)
    {
        if (currentRoom == null || currentRoom.players == null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(localPlayerId))
        {
            for (int i = 0; i < currentRoom.players.Count; i++)
            {
                PlayerState player = currentRoom.players[i];
                if (player != null && player.playerId == localPlayerId)
                {
                    return player;
                }
            }
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

    private void AutoAssignReferences(bool allowCreateGeneratedUi = true)
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

        if (copyRoomIdButton == null)
        {
            copyRoomIdButton = FindButtonByName(allTransforms, "CopyRoomIdButton");
        }

        if (startGameButton == null)
        {
            startGameButton = FindButtonByName(allTransforms, "StartGameButton");
        }

        if (startGameButtonImage == null && startGameButton != null)
        {
            startGameButtonImage = startGameButton.GetComponent<Image>();
        }

        if (startGameButtonText == null && startGameButton != null)
        {
            startGameButtonText = startGameButton.GetComponentInChildren<TMP_Text>(true);
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

        EnsurePlayerSlot01IdentityTexts(allowCreateGeneratedUi);
        EnsureHostStatusSprites();
    }

    private void EnsurePlayerSlot01IdentityTexts(bool allowCreateGeneratedUi = true)
    {
        if (playerSlots == null || playerSlots.Length == 0 || playerSlots[0] == null)
        {
            return;
        }

        Transform slotRoot = playerSlots[0].transform;
        if (playerSlot01NameText == null)
        {
            Transform existingName = slotRoot.Find("PlayerNameText");
            if (existingName != null)
            {
                playerSlot01NameText = existingName.GetComponent<TMP_Text>();
            }
        }

        if (playerSlot01HostBadgeText == null)
        {
            Transform existingHostBadge = slotRoot.Find("HostBadgeText");
            if (existingHostBadge != null)
            {
                playerSlot01HostBadgeText = existingHostBadge.GetComponent<TMP_Text>();
            }
        }

        if (playerSlot01NameText == null && allowCreateGeneratedUi)
        {
            playerSlot01NameText = CreateSlotIdentityText(slotRoot, "PlayerNameText", new Vector2(14f, -12f), new Vector2(200f, 24f), 16f);
        }

        if (playerSlot01HostBadgeText == null && allowCreateGeneratedUi)
        {
            playerSlot01HostBadgeText = CreateSlotIdentityText(slotRoot, "HostBadgeText", new Vector2(14f, -32f), new Vector2(120f, 20f), 14f);
        }

        if (playerSlot01NameText != null)
        {
            playerSlot01NameText.color = localPlayerNameColor;
            playerSlot01NameText.fontStyle = FontStyles.Bold;
            playerSlot01NameText.alignment = TextAlignmentOptions.MidlineLeft;
            playerSlot01NameText.overflowMode = TextOverflowModes.Ellipsis;
            playerSlot01NameText.enableWordWrapping = false;
            playerSlot01NameText.raycastTarget = false;
        }

        if (playerSlot01HostBadgeText != null)
        {
            playerSlot01HostBadgeText.color = localPlayerHostBadgeColor;
            playerSlot01HostBadgeText.fontStyle = FontStyles.Bold;
            playerSlot01HostBadgeText.alignment = TextAlignmentOptions.MidlineLeft;
            playerSlot01HostBadgeText.overflowMode = TextOverflowModes.Ellipsis;
            playerSlot01HostBadgeText.enableWordWrapping = false;
            playerSlot01HostBadgeText.raycastTarget = false;
        }

        TMP_FontAsset fallbackFont = playerSlot01StatusText != null ? playerSlot01StatusText.font : null;
        if (fallbackFont != null)
        {
            if (playerSlot01NameText != null && playerSlot01NameText.font == null)
            {
                playerSlot01NameText.font = fallbackFont;
            }

            if (playerSlot01HostBadgeText != null && playerSlot01HostBadgeText.font == null)
            {
                playerSlot01HostBadgeText.font = fallbackFont;
            }
        }
    }

    private static TMP_Text CreateSlotIdentityText(
        Transform parent,
        string objectName,
        Vector2 anchoredPosition,
        Vector2 sizeDelta,
        float fontSize)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(0f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = sizeDelta;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.text = string.Empty;
        return text;
    }

    private void ApplyLocalPlayerIdentity(PlayerState localPlayer)
    {
        EnsurePlayerSlot01IdentityTexts();

        if (playerSlot01NameText == null || playerSlot01HostBadgeText == null)
        {
            return;
        }

        if (localPlayer == null)
        {
            ClearLocalPlayerIdentity();
            return;
        }

        string displayName = ResolveLocalPlayerDisplayName(localPlayer);

        playerSlot01NameText.text = displayName;
        playerSlot01NameText.gameObject.SetActive(true);

        bool isHost = localPlayer.isHost;
        playerSlot01HostBadgeText.text = isHost ? localPlayerHostBadgeLabel : string.Empty;
        playerSlot01HostBadgeText.gameObject.SetActive(isHost);
    }

    private string ResolveLocalPlayerDisplayName(PlayerState localPlayer)
    {
        if (localPlayer != null)
        {
            string playerName = localPlayer.displayName != null ? localPlayer.displayName.Trim() : string.Empty;
            if (!string.IsNullOrWhiteSpace(playerName))
            {
                return playerName;
            }
        }

        if (SharedStore.LocalPlayer != null)
        {
            string localProfileName = SharedStore.LocalPlayer.displayName != null
                ? SharedStore.LocalPlayer.displayName.Trim()
                : string.Empty;
            if (!string.IsNullOrWhiteSpace(localProfileName))
            {
                return localProfileName;
            }
        }

        string configuredFallback = localPlayerFallbackName != null ? localPlayerFallbackName.Trim() : string.Empty;
        return string.IsNullOrWhiteSpace(configuredFallback) ? "Player" : configuredFallback;
    }

    private void ClearLocalPlayerIdentity()
    {
        if (playerSlot01NameText != null)
        {
            playerSlot01NameText.text = string.Empty;
            playerSlot01NameText.gameObject.SetActive(false);
        }

        if (playerSlot01HostBadgeText != null)
        {
            playerSlot01HostBadgeText.text = string.Empty;
            playerSlot01HostBadgeText.gameObject.SetActive(false);
        }
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

    private static Button FindButtonByName(Transform[] allTransforms, string targetName)
    {
        for (int i = 0; i < allTransforms.Length; i++)
        {
            if (allTransforms[i].name == targetName)
            {
                return allTransforms[i].GetComponent<Button>();
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
