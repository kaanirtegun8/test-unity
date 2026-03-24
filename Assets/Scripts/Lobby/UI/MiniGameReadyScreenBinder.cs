using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Collections;
using Unity.Services.Lobbies.Models;

public class MiniGameReadyScreenBinder : MonoBehaviour
{
    private const int SlotCount = 4;
    private const string LegacyConfirmedText = "Confirmed";
    private const string LegacyConfirmActionText = "Confirm";
    private const string DefaultWaitingText = "Waiting";
    private const string DefaultLockedInText = "Locked In";
    private const string DefaultLockInActionText = "Lock In";

    private enum MatchStartReadinessState
    {
        None = 0,
        ReadyByEarlyConfirm = 1,
        ReadyByTimeout = 2
    }

    private const string EarlyConfirmReadyLog = "MiniGameReadyScreenBinder: Ready to start early (all players confirmed).";
    private const string TimeoutReadyLog = "MiniGameReadyScreenBinder: Ready to start by timeout (countdown reached 00:00).";

    [SerializeField] private TMP_Text countdownText;
    [SerializeField] private int countdownStartSeconds = 30;
    [SerializeField] private TMP_Text playersHintText;
    [SerializeField] private string playersHintMessage = "Hızlı geçiş için herkes confirm etmeli.";
    [SerializeField] private TMP_Text[] playerNameTexts = new TMP_Text[SlotCount];
    [SerializeField] private TMP_Text[] playerStateTexts = new TMP_Text[SlotCount];
    [SerializeField] private Button[] playerConfirmButtons = new Button[SlotCount];
    [SerializeField] private Sprite lockInButtonSprite;
    [SerializeField] private string emptySlotNameText = "";
    [SerializeField] private string emptySlotStateText = "Empty";
    [SerializeField] private string lockedSlotNameText = "";
    [SerializeField] private string lockedSlotStateText = "Locked";
    [SerializeField] private string waitingText = DefaultWaitingText;
    [SerializeField] private string confirmedText = DefaultLockedInText;
    [SerializeField] private string confirmActionText = DefaultLockInActionText;
    [SerializeField] private Color waitingTextColor = new Color(0.78f, 0.87f, 0.95f, 0.95f);
    [SerializeField] private Color confirmedTextColor = new Color(0.62f, 0.9f, 0.67f, 1f);
    [SerializeField] private Color confirmActionColor = new Color(0.9f, 0.95f, 1f, 1f);
    [SerializeField] private Color lockInButtonTintColor = Color.white;
    [SerializeField] private float lobbyRefreshIntervalSeconds = 1.5f;

    private readonly UnityAction[] slotConfirmHandlers = new UnityAction[SlotCount];
    private Coroutine countdownCoroutine;
    private bool isCountdownCompleted;
    private int remainingSeconds;
    private MatchStartReadinessState matchStartReadinessState = MatchStartReadinessState.None;
    private Sprite resolvedLockInButtonSprite;
    private bool hasResolvedLockInButtonSprite;
    private Coroutine lobbyRefreshCoroutine;
    private bool isLobbyRefreshInProgress;
    private bool isLockInUpdateInProgress;
    private UnityLobbyService lobbyService;

    private LobbyStateStore SharedStore => LobbyStateStore.Local;

    private void OnEnable()
    {
        if (lobbyService == null)
        {
            lobbyService = new UnityLobbyService();
        }

        AutoAssignReferences();
        NormalizeStatusTexts();
        ApplyPlayersHintText();
        BindConfirmButtons();
        ResetMatchStartReadinessState();
        RefreshFromCurrentRoom();
        StartCountdown();
        StartLobbyRefreshLoop();
    }

    private void OnDisable()
    {
        StopCountdown();
        StopLobbyRefreshLoop();
        UnbindConfirmButtons();
    }

    private void OnDestroy()
    {
        StopCountdown();
        StopLobbyRefreshLoop();
        UnbindConfirmButtons();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        AutoAssignReferences(false);
        NormalizeStatusTexts();
    }
#endif

    private void NormalizeStatusTexts()
    {
        waitingText = string.IsNullOrWhiteSpace(waitingText)
            ? DefaultWaitingText
            : waitingText.Trim();

        if (string.IsNullOrWhiteSpace(confirmedText) ||
            string.Equals(confirmedText.Trim(), LegacyConfirmedText, StringComparison.OrdinalIgnoreCase))
        {
            confirmedText = DefaultLockedInText;
        }
        else
        {
            confirmedText = confirmedText.Trim();
        }

        if (string.IsNullOrWhiteSpace(confirmActionText) ||
            string.Equals(confirmActionText.Trim(), LegacyConfirmActionText, StringComparison.OrdinalIgnoreCase))
        {
            confirmActionText = DefaultLockInActionText;
        }
        else
        {
            confirmActionText = confirmActionText.Trim();
        }
    }

    private void ApplyPlayersHintText()
    {
        TMP_Text hintLabel = EnsurePlayersHintText();
        if (hintLabel == null)
        {
            return;
        }

        string safeHint = string.IsNullOrWhiteSpace(playersHintMessage)
            ? "Hızlı geçiş için herkes confirm etmeli."
            : playersHintMessage.Trim();
        hintLabel.text = safeHint;
        hintLabel.gameObject.SetActive(true);
    }

    public void RefreshFromCurrentRoom()
    {
        ApplyPlayers(SharedStore.CurrentRoom);
    }

    private void StartCountdown()
    {
        StopCountdown();
        isCountdownCompleted = false;
        remainingSeconds = Mathf.Max(0, countdownStartSeconds);
        ApplyCountdownText(remainingSeconds);
        countdownCoroutine = StartCoroutine(CountdownCoroutine());
    }

    private void StopCountdown()
    {
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
        }
    }

    private System.Collections.IEnumerator CountdownCoroutine()
    {
        WaitForSeconds waitOneSecond = new WaitForSeconds(1f);

        while (remainingSeconds > 0)
        {
            yield return waitOneSecond;
            remainingSeconds = Mathf.Max(0, remainingSeconds - 1);
            ApplyCountdownText(remainingSeconds);
        }

        countdownCoroutine = null;
        if (!isCountdownCompleted)
        {
            isCountdownCompleted = true;
            SetMatchStartReadinessState(MatchStartReadinessState.ReadyByTimeout);
        }
    }

    private void ApplyCountdownText(int seconds)
    {
        if (countdownText == null)
        {
            return;
        }

        int safeSeconds = Mathf.Max(0, seconds);
        int minutesPart = safeSeconds / 60;
        int secondsPart = safeSeconds % 60;
        countdownText.text = $"{minutesPart:00}:{secondsPart:00}";
    }

    private void ApplyPlayers(RoomState room)
    {
        int maxPlayers = Mathf.Clamp(room != null ? room.maxPlayers : SlotCount, 1, SlotCount);
        List<PlayerState> orderedPlayers = GetOrderedPlayers(room, maxPlayers);
        string localPlayerId = SharedStore.LocalPlayer != null
            ? NormalizePlayerId(SharedStore.LocalPlayer.playerId)
            : string.Empty;

        for (int i = 0; i < SlotCount; i++)
        {
            if (i < orderedPlayers.Count)
            {
                PlayerState player = orderedPlayers[i];
                string displayName = ResolveDisplayName(player, i + 1);
                ApplyOccupiedSlot(i, displayName, player, localPlayerId);
            }
            else if (i < maxPlayers)
            {
                ApplyPassiveSlot(i, emptySlotNameText, emptySlotStateText, waitingTextColor, false);
            }
            else
            {
                ApplyPassiveSlot(i, lockedSlotNameText, lockedSlotStateText, waitingTextColor, false);
            }
        }

        EvaluateEarlyConfirmReadiness(room);
    }

    private void ApplyOccupiedSlot(int slotIndex, string displayName, PlayerState player, string localPlayerId)
    {
        bool isLocalPlayer = IsLocalPlayer(player, localPlayerId);
        bool isConfirmed = player != null && player.isConfirmed;
        string stateText = waitingText;
        Color stateColor = waitingTextColor;
        bool showConfirmActionButton = false;

        if (isConfirmed)
        {
            stateText = confirmedText;
            stateColor = confirmedTextColor;
        }
        else if (isLocalPlayer)
        {
            showConfirmActionButton = true;
        }

        ApplyPassiveSlot(slotIndex, displayName, stateText, stateColor, true);
        SetStateTextVisibility(slotIndex, !showConfirmActionButton);
        SetConfirmButtonState(slotIndex, showConfirmActionButton);
    }

    private void ApplyPassiveSlot(int slotIndex, string nameText, string stateText, Color stateColor, bool occupied)
    {
        TMP_Text nameLabel = GetPlayerNameText(slotIndex);
        TMP_Text stateLabel = GetPlayerStateText(slotIndex);

        if (nameLabel != null)
        {
            nameLabel.text = nameText ?? string.Empty;
            nameLabel.gameObject.SetActive(occupied || !string.IsNullOrWhiteSpace(nameLabel.text));
        }

        if (stateLabel != null)
        {
            stateLabel.text = stateText ?? string.Empty;
            stateLabel.color = stateColor;
            stateLabel.gameObject.SetActive(true);
        }

        SetStateTextVisibility(slotIndex, true);
        SetConfirmButtonState(slotIndex, false);
    }

    private void SetStateTextVisibility(int slotIndex, bool visible)
    {
        TMP_Text stateLabel = GetPlayerStateText(slotIndex);
        if (stateLabel != null)
        {
            stateLabel.gameObject.SetActive(visible);
        }
    }

    private static List<PlayerState> GetOrderedPlayers(RoomState room, int maxPlayers)
    {
        List<PlayerState> orderedPlayers = new List<PlayerState>();
        if (room == null || room.players == null || room.players.Count == 0 || maxPlayers <= 0)
        {
            return orderedPlayers;
        }

        PlayerState hostPlayer = FindHostPlayer(room.players);
        if (hostPlayer != null)
        {
            orderedPlayers.Add(hostPlayer);
        }

        for (int i = 0; i < room.players.Count; i++)
        {
            PlayerState player = room.players[i];
            if (player == null)
            {
                continue;
            }

            if (hostPlayer != null && AreSamePlayer(player, hostPlayer))
            {
                continue;
            }

            orderedPlayers.Add(player);
            if (orderedPlayers.Count >= maxPlayers)
            {
                break;
            }
        }

        return orderedPlayers;
    }

    private static PlayerState FindHostPlayer(List<PlayerState> players)
    {
        for (int i = 0; i < players.Count; i++)
        {
            PlayerState player = players[i];
            if (player != null && player.isHost)
            {
                return player;
            }
        }

        for (int i = 0; i < players.Count; i++)
        {
            if (players[i] != null)
            {
                return players[i];
            }
        }

        return null;
    }

    private static bool AreSamePlayer(PlayerState a, PlayerState b)
    {
        if (ReferenceEquals(a, b))
        {
            return true;
        }

        if (a == null || b == null)
        {
            return false;
        }

        string aId = a.playerId != null ? a.playerId.Trim() : string.Empty;
        string bId = b.playerId != null ? b.playerId.Trim() : string.Empty;

        if (!string.IsNullOrWhiteSpace(aId) && !string.IsNullOrWhiteSpace(bId))
        {
            return string.Equals(aId, bId, System.StringComparison.Ordinal);
        }

        return false;
    }

    private static string ResolveDisplayName(PlayerState player, int fallbackIndex)
    {
        if (player != null)
        {
            string safeName = player.displayName != null ? player.displayName.Trim() : string.Empty;
            if (!string.IsNullOrWhiteSpace(safeName))
            {
                return safeName;
            }
        }

        return $"Player {Mathf.Max(1, fallbackIndex)}";
    }

    private void OnConfirmButtonClicked(int slotIndex)
    {
        TryLockInAsync(slotIndex);
    }

    private async void TryLockInAsync(int slotIndex)
    {
        if (isLockInUpdateInProgress)
        {
            return;
        }

        RoomState room = SharedStore.CurrentRoom;
        if (room == null || room.players == null || slotIndex < 0 || lobbyService == null)
        {
            return;
        }

        if (!IsAuthReadyForLobbySync())
        {
            return;
        }

        string lobbyId = room.roomId != null ? room.roomId.Trim() : string.Empty;
        if (string.IsNullOrWhiteSpace(lobbyId))
        {
            return;
        }

        int maxPlayers = Mathf.Clamp(room.maxPlayers, 1, SlotCount);
        List<PlayerState> orderedPlayers = GetOrderedPlayers(room, maxPlayers);
        if (slotIndex >= orderedPlayers.Count)
        {
            return;
        }

        PlayerState targetPlayer = orderedPlayers[slotIndex];
        if (targetPlayer == null || targetPlayer.isConfirmed)
        {
            return;
        }

        string localPlayerId = SharedStore.LocalPlayer != null
            ? NormalizePlayerId(SharedStore.LocalPlayer.playerId)
            : string.Empty;
        if (string.IsNullOrWhiteSpace(localPlayerId) || !IsLocalPlayer(targetPlayer, localPlayerId))
        {
            return;
        }

        string localDisplayName = SharedStore.LocalPlayer != null ? SharedStore.LocalPlayer.displayName : string.Empty;

        isLockInUpdateInProgress = true;
        bool updateSucceeded = false;
        try
        {
            updateSucceeded = await lobbyService.UpdatePlayerLockInAsync(
                lobbyId,
                true,
                localPlayerId,
                localDisplayName);
        }
        finally
        {
            isLockInUpdateInProgress = false;
        }

        if (!updateSucceeded)
        {
            return;
        }

        targetPlayer.isConfirmed = true;
        SharedStore.ApplyMappedCurrentRoom(room, true);
        RefreshFromCurrentRoom();
        TryRefreshLobbyStateAsync();
    }

    private TMP_Text GetPlayerNameText(int slotIndex)
    {
        if (playerNameTexts == null || slotIndex < 0 || slotIndex >= playerNameTexts.Length)
        {
            return null;
        }

        return playerNameTexts[slotIndex];
    }

    private TMP_Text GetPlayerStateText(int slotIndex)
    {
        if (playerStateTexts == null || slotIndex < 0 || slotIndex >= playerStateTexts.Length)
        {
            return null;
        }

        return playerStateTexts[slotIndex];
    }

    private Button GetPlayerConfirmButton(int slotIndex)
    {
        if (playerConfirmButtons == null || slotIndex < 0 || slotIndex >= playerConfirmButtons.Length)
        {
            return null;
        }

        return playerConfirmButtons[slotIndex];
    }

    private void BindConfirmButtons()
    {
        for (int i = 0; i < SlotCount; i++)
        {
            Button button = EnsurePlayerConfirmButton(i);
            if (button == null)
            {
                continue;
            }

            if (slotConfirmHandlers[i] == null)
            {
                int capturedIndex = i;
                slotConfirmHandlers[i] = () => OnConfirmButtonClicked(capturedIndex);
            }

            button.onClick.RemoveListener(slotConfirmHandlers[i]);
            button.onClick.AddListener(slotConfirmHandlers[i]);
        }
    }

    private void UnbindConfirmButtons()
    {
        for (int i = 0; i < SlotCount; i++)
        {
            Button button = GetPlayerConfirmButton(i);
            UnityAction handler = slotConfirmHandlers[i];

            if (button != null && handler != null)
            {
                button.onClick.RemoveListener(handler);
            }
        }
    }

    private Button EnsurePlayerConfirmButton(int slotIndex)
    {
        Button existingButton = GetPlayerConfirmButton(slotIndex);
        if (existingButton != null)
        {
            return existingButton;
        }

        TMP_Text stateLabel = GetPlayerStateText(slotIndex);
        if (stateLabel == null)
        {
            return null;
        }

        RectTransform stateRect = stateLabel.rectTransform;
        RectTransform parentRect = stateRect.parent as RectTransform;
        if (parentRect == null)
        {
            return null;
        }

        GameObject buttonObject = new GameObject($"LockInButton0{slotIndex + 1}", typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.SetParent(parentRect, false);
        buttonRect.anchorMin = stateRect.anchorMin;
        buttonRect.anchorMax = stateRect.anchorMax;
        buttonRect.anchoredPosition = stateRect.anchoredPosition;
        buttonRect.sizeDelta = stateRect.sizeDelta;
        buttonRect.pivot = stateRect.pivot;

        Image buttonImage = buttonObject.GetComponent<Image>();
        Sprite resolvedSprite = ResolveLockInButtonSprite();
        if (resolvedSprite != null)
        {
            buttonImage.sprite = resolvedSprite;
            buttonImage.type = Image.Type.Sliced;
            buttonImage.preserveAspect = true;
        }
        buttonImage.color = lockInButtonTintColor;
        buttonImage.raycastTarget = true;

        Button createdButton = buttonObject.GetComponent<Button>();
        createdButton.targetGraphic = buttonImage;
        ColorBlock colors = createdButton.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.95f, 0.95f, 0.95f, 1f);
        colors.pressedColor = new Color(0.84f, 0.84f, 0.84f, 1f);
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color(1f, 1f, 1f, 0.6f);
        createdButton.colors = colors;
        createdButton.transition = Selectable.Transition.ColorTint;
        createdButton.interactable = false;
        buttonObject.SetActive(false);

        GameObject labelObject = new GameObject($"LockInButtonText0{slotIndex + 1}", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.SetParent(buttonRect, false);
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.anchoredPosition = Vector2.zero;
        labelRect.sizeDelta = new Vector2(-14f, -6f);

        TMP_Text buttonLabel = labelObject.GetComponent<TMP_Text>();
        buttonLabel.text = confirmActionText;
        buttonLabel.fontSize = stateLabel.fontSize;
        buttonLabel.fontStyle = stateLabel.fontStyle;
        buttonLabel.alignment = TextAlignmentOptions.Center;
        buttonLabel.color = confirmActionColor;
        buttonLabel.raycastTarget = false;
        buttonLabel.enableAutoSizing = false;
        if (stateLabel.font != null)
        {
            buttonLabel.font = stateLabel.font;
        }
        if (stateLabel.fontSharedMaterial != null)
        {
            buttonLabel.fontSharedMaterial = stateLabel.fontSharedMaterial;
        }

        stateLabel.raycastTarget = false;

        playerConfirmButtons[slotIndex] = createdButton;
        return createdButton;
    }

    private void SetConfirmButtonState(int slotIndex, bool interactable)
    {
        TMP_Text stateLabel = GetPlayerStateText(slotIndex);
        Button button = EnsurePlayerConfirmButton(slotIndex);
        if (stateLabel != null)
        {
            stateLabel.raycastTarget = false;
        }

        if (button != null)
        {
            TMP_Text buttonLabel = button.GetComponentInChildren<TMP_Text>(true);
            if (buttonLabel != null)
            {
                buttonLabel.text = confirmActionText;
                buttonLabel.color = confirmActionColor;
            }

            button.gameObject.SetActive(interactable);
            button.interactable = interactable;
        }
    }

    private Sprite ResolveLockInButtonSprite()
    {
        if (lockInButtonSprite != null)
        {
            return lockInButtonSprite;
        }

        if (hasResolvedLockInButtonSprite)
        {
            return resolvedLockInButtonSprite;
        }

        hasResolvedLockInButtonSprite = true;
        Image[] allImages = transform.root != null
            ? transform.root.GetComponentsInChildren<Image>(true)
            : Array.Empty<Image>();

        string[] preferredButtonNames =
        {
            "StartGameButton",
            "CreateButton",
            "CreateRoomButton",
            "JoinButton",
            "BackButton"
        };

        for (int nameIndex = 0; nameIndex < preferredButtonNames.Length; nameIndex++)
        {
            string preferredName = preferredButtonNames[nameIndex];
            for (int i = 0; i < allImages.Length; i++)
            {
                Image image = allImages[i];
                if (image == null || image.sprite == null || image.gameObject == null)
                {
                    continue;
                }

                if (string.Equals(image.gameObject.name, preferredName, StringComparison.Ordinal))
                {
                    resolvedLockInButtonSprite = image.sprite;
                    return resolvedLockInButtonSprite;
                }
            }
        }

        for (int i = 0; i < allImages.Length; i++)
        {
            Image image = allImages[i];
            if (image == null || image.sprite == null || image.gameObject == null)
            {
                continue;
            }

            if (image.gameObject.name.IndexOf("Button", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                resolvedLockInButtonSprite = image.sprite;
                return resolvedLockInButtonSprite;
            }
        }

        return null;
    }

    private void ResetPlayersToWaiting()
    {
        RoomState room = SharedStore.CurrentRoom;
        if (room == null || room.players == null)
        {
            return;
        }

        for (int i = 0; i < room.players.Count; i++)
        {
            PlayerState player = room.players[i];
            if (player == null)
            {
                continue;
            }

            player.isConfirmed = false;
        }
    }

    private void ResetMatchStartReadinessState()
    {
        matchStartReadinessState = MatchStartReadinessState.None;
        isCountdownCompleted = false;
    }

    private void EvaluateEarlyConfirmReadiness(RoomState room)
    {
        if (matchStartReadinessState != MatchStartReadinessState.None || room == null || room.players == null)
        {
            return;
        }

        bool hasAnyPlayer = false;
        for (int i = 0; i < room.players.Count; i++)
        {
            PlayerState player = room.players[i];
            if (player == null)
            {
                continue;
            }

            hasAnyPlayer = true;
            if (!player.isConfirmed)
            {
                return;
            }
        }

        if (!hasAnyPlayer)
        {
            return;
        }

        SetMatchStartReadinessState(MatchStartReadinessState.ReadyByEarlyConfirm);
        StopCountdown();
    }

    private void SetMatchStartReadinessState(MatchStartReadinessState nextState)
    {
        if (nextState == MatchStartReadinessState.None || matchStartReadinessState != MatchStartReadinessState.None)
        {
            return;
        }

        matchStartReadinessState = nextState;

        if (nextState == MatchStartReadinessState.ReadyByEarlyConfirm)
        {
            Debug.Log(EarlyConfirmReadyLog);
            return;
        }

        if (nextState == MatchStartReadinessState.ReadyByTimeout)
        {
            Debug.Log(TimeoutReadyLog);
        }
    }

    private static string NormalizePlayerId(string playerId)
    {
        return playerId != null ? playerId.Trim() : string.Empty;
    }

    private static bool IsLocalPlayer(PlayerState player, string localPlayerId)
    {
        if (player == null || string.IsNullOrWhiteSpace(localPlayerId))
        {
            return false;
        }

        string playerId = NormalizePlayerId(player.playerId);
        return !string.IsNullOrWhiteSpace(playerId) &&
               string.Equals(playerId, localPlayerId, StringComparison.Ordinal);
    }

    private void StartLobbyRefreshLoop()
    {
        StopLobbyRefreshLoop();
        if (!Application.isPlaying)
        {
            return;
        }

        TryRefreshLobbyStateAsync();
        lobbyRefreshCoroutine = StartCoroutine(LobbyRefreshCoroutine());
    }

    private void StopLobbyRefreshLoop()
    {
        if (lobbyRefreshCoroutine != null)
        {
            StopCoroutine(lobbyRefreshCoroutine);
            lobbyRefreshCoroutine = null;
        }

        isLobbyRefreshInProgress = false;
    }

    private IEnumerator LobbyRefreshCoroutine()
    {
        float safeInterval = Mathf.Max(1f, lobbyRefreshIntervalSeconds);
        WaitForSeconds wait = new WaitForSeconds(safeInterval);

        while (isActiveAndEnabled && gameObject.activeInHierarchy)
        {
            yield return wait;
            TryRefreshLobbyStateAsync();
        }

        lobbyRefreshCoroutine = null;
    }

    private async void TryRefreshLobbyStateAsync()
    {
        if (isLobbyRefreshInProgress || lobbyService == null || !IsAuthReadyForLobbySync())
        {
            return;
        }

        RoomState currentRoom = SharedStore.CurrentRoom;
        string lobbyId = currentRoom != null && currentRoom.roomId != null ? currentRoom.roomId.Trim() : string.Empty;
        if (string.IsNullOrWhiteSpace(lobbyId))
        {
            return;
        }

        isLobbyRefreshInProgress = true;
        Lobby lobby = null;
        try
        {
            lobby = await lobbyService.GetLobbyAsync(lobbyId);
        }
        finally
        {
            isLobbyRefreshInProgress = false;
        }

        if (lobby == null)
        {
            return;
        }

        RoomState mappedRoom = lobbyService.MapLobbyToRoomState(lobby, false);
        if (!SharedStore.ApplyMappedCurrentRoom(mappedRoom, true))
        {
            return;
        }

        RefreshFromCurrentRoom();
    }

    private static bool IsAuthReadyForLobbySync()
    {
        AuthStateStore authStore = AuthStateStore.Local;
        return authStore != null && authStore.IsAuthenticated;
    }

    private TMP_Text EnsurePlayersHintText()
    {
        if (playersHintText != null)
        {
            return playersHintText;
        }

        RectTransform rightPlayersArea = transform.Find("ContentArea/RightPlayersArea") as RectTransform;
        if (rightPlayersArea == null)
        {
            return null;
        }

        RectTransform playersList = transform.Find("ContentArea/RightPlayersArea/PlayersList") as RectTransform;

        GameObject hintObject = new GameObject("PlayersHintText", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform hintRect = hintObject.GetComponent<RectTransform>();
        hintRect.SetParent(rightPlayersArea, false);
        hintRect.anchorMin = new Vector2(0.06f, 0.89f);
        hintRect.anchorMax = new Vector2(0.94f, 0.98f);
        hintRect.anchoredPosition = Vector2.zero;
        hintRect.sizeDelta = Vector2.zero;

        TMP_Text hintLabel = hintObject.GetComponent<TMP_Text>();
        hintLabel.text = playersHintMessage;
        hintLabel.alignment = TextAlignmentOptions.Left;
        hintLabel.fontSize = 14f;
        hintLabel.fontStyle = FontStyles.Italic;
        hintLabel.color = new Color(0.8f, 0.9f, 0.99f, 0.9f);
        hintLabel.enableWordWrapping = true;
        hintLabel.overflowMode = TextOverflowModes.Ellipsis;
        hintLabel.raycastTarget = false;
        TMP_Text styleSource = GetPlayerNameText(0) ?? GetPlayerStateText(0);
        if (styleSource != null)
        {
            if (styleSource.font != null)
            {
                hintLabel.font = styleSource.font;
            }

            if (styleSource.fontSharedMaterial != null)
            {
                hintLabel.fontSharedMaterial = styleSource.fontSharedMaterial;
            }
        }

        if (playersList != null)
        {
            Vector2 anchorMin = playersList.anchorMin;
            Vector2 anchorMax = playersList.anchorMax;
            playersList.anchorMin = new Vector2(anchorMin.x, 0.01f);
            playersList.anchorMax = new Vector2(anchorMax.x, 0.87f);
            playersList.anchoredPosition = Vector2.zero;
            playersList.sizeDelta = Vector2.zero;
        }

        playersHintText = hintLabel;
        return playersHintText;
    }

    private void AutoAssignReferences(bool includeInactive = true)
    {
        if (countdownText == null)
        {
            countdownText = transform.Find("HeaderArea/CountdownText")?.GetComponent<TMP_Text>();
        }

        if (playersHintText == null)
        {
            playersHintText = transform.Find("ContentArea/RightPlayersArea/PlayersHintText")?.GetComponent<TMP_Text>();
        }

        for (int i = 0; i < SlotCount; i++)
        {
            if (GetPlayerNameText(i) == null)
            {
                string namePath = $"ContentArea/RightPlayersArea/PlayersList/PlayerRow0{i + 1}/PlayerNameText0{i + 1}";
                TMP_Text resolved = transform.Find(namePath)?.GetComponent<TMP_Text>();
                if (resolved != null)
                {
                    playerNameTexts[i] = resolved;
                }
            }

            if (GetPlayerStateText(i) == null)
            {
                string statePath = $"ContentArea/RightPlayersArea/PlayersList/PlayerRow0{i + 1}/ReadyStateText0{i + 1}";
                TMP_Text resolved = transform.Find(statePath)?.GetComponent<TMP_Text>();
                if (resolved != null)
                {
                    playerStateTexts[i] = resolved;
                }
            }

            if (GetPlayerConfirmButton(i) == null)
            {
                TMP_Text stateLabel = GetPlayerStateText(i);
                if (stateLabel != null)
                {
                    playerConfirmButtons[i] = stateLabel.GetComponent<Button>();
                }
            }
        }

        if (!includeInactive || Application.isPlaying)
        {
            return;
        }

        TMP_Text[] allTexts = GetComponentsInChildren<TMP_Text>(true);
        if (countdownText == null)
        {
            countdownText = FindTextByName(allTexts, "CountdownText");
        }

        for (int i = 0; i < SlotCount; i++)
        {
            if (playerNameTexts[i] == null)
            {
                playerNameTexts[i] = FindTextByName(allTexts, $"PlayerNameText0{i + 1}");
            }

            if (playerStateTexts[i] == null)
            {
                playerStateTexts[i] = FindTextByName(allTexts, $"ReadyStateText0{i + 1}");
            }

            if (playerConfirmButtons[i] == null && playerStateTexts[i] != null)
            {
                playerConfirmButtons[i] = playerStateTexts[i].GetComponent<Button>();
            }
        }
    }

    private static TMP_Text FindTextByName(TMP_Text[] allTexts, string targetName)
    {
        if (allTexts == null || string.IsNullOrWhiteSpace(targetName))
        {
            return null;
        }

        for (int i = 0; i < allTexts.Length; i++)
        {
            TMP_Text text = allTexts[i];
            if (text != null && text.name == targetName)
            {
                return text;
            }
        }

        return null;
    }
}
