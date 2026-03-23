using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.Events;
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
    [SerializeField] private bool enableLobbyEventTracking = true;
    [SerializeField] private float fallbackLobbyRefreshIntervalSeconds = 2.5f;
    [SerializeField] private LobbyMockScreenSwitcher screenSwitcher;
    [SerializeField] private RoomBrowserScreenBinder roomBrowserScreenBinder;
    [SerializeField] private MiniGameReadyScreenBinder miniGameReadyScreenBinder;
    [SerializeField] private GameObject roomBrowserScreen;
    [SerializeField] private GameObject createRoomScreen;
    [SerializeField] private GameObject currentRoomScreen;
    [SerializeField] private GameObject miniGameReadyScreen;

    private GameObject copyTooltipObject;
    private TMP_Text copyTooltipText;
    private Coroutine copyTooltipHideRoutine;
    private Coroutine fallbackLobbyRefreshCoroutine;
    private UnityLobbyService lobbyService;
    private bool isFallbackLobbyRefreshInProgress;
    private bool isLobbyEventsSubscriptionInProgress;
    private bool isReadySyncInProgress;
    private bool isStartGamePhaseUpdateInProgress;
    private bool hasHandledMissingLobby;
    private ILobbyEvents lobbyEventsSubscription;
    private LobbyEventCallbacks lobbyEventCallbacks;
    private string subscribedLobbyId = string.Empty;
    private LobbyEventConnectionState lobbyEventConnectionState = LobbyEventConnectionState.Unknown;
    private readonly Queue<Action> mainThreadActions = new Queue<Action>();
    private readonly object mainThreadActionsLock = new object();
    private readonly UnityAction[] slotStatusButtonHandlers = new UnityAction[DefaultSlotCount];

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
        lobbyService = new UnityLobbyService();
        AutoAssignReferences();
        BindUiEvents();
        ApplyCurrentRoomToUi();
    }

    private void OnEnable()
    {
        AutoAssignReferences();
        BindUiEvents();
        ApplyCurrentRoomToUi();
        StartLobbyTrackingLoop();
    }

    private void Start()
    {
        // Ensure first visible frame uses CurrentRoom-driven state
        // even if another presenter touched button interactable in OnEnable.
        ApplyCurrentRoomToUi();
    }

    private void OnDisable()
    {
        StopLobbyTrackingLoop();
    }

    private void Update()
    {
        FlushMainThreadActions();
    }

    private void OnDestroy()
    {
        StopLobbyTrackingLoop();
        UnbindUiEvents();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        AutoAssignReferences(false);
    }
#endif

    private void StartLobbyTrackingLoop()
    {
        if (!Application.isPlaying || !enableLobbyEventTracking)
        {
            return;
        }

        StopLobbyTrackingLoop();
        hasHandledMissingLobby = false;
        TryStartLobbyEventsSubscriptionAsync();
        TryRefreshCurrentLobbyAsync();
        fallbackLobbyRefreshCoroutine = StartCoroutine(FallbackLobbyRefreshCoroutine());
    }

    private void StopLobbyTrackingLoop()
    {
        if (fallbackLobbyRefreshCoroutine != null)
        {
            StopCoroutine(fallbackLobbyRefreshCoroutine);
            fallbackLobbyRefreshCoroutine = null;
        }

        isFallbackLobbyRefreshInProgress = false;
        TryStopLobbyEventsSubscriptionAsync();
    }

    private IEnumerator FallbackLobbyRefreshCoroutine()
    {
        float safeInterval = Mathf.Max(1f, fallbackLobbyRefreshIntervalSeconds);
        WaitForSeconds waitForInterval = new WaitForSeconds(safeInterval);

        while (isActiveAndEnabled && gameObject.activeInHierarchy)
        {
            yield return waitForInterval;

            if (!HasCurrentLobbyId())
            {
                continue;
            }

            if (!HasActiveLobbyEventsSubscription())
            {
                TryStartLobbyEventsSubscriptionAsync();
            }

            TryRefreshCurrentLobbyAsync();
        }

        fallbackLobbyRefreshCoroutine = null;
    }

    private bool HasActiveLobbyEventsSubscription()
    {
        string currentLobbyId = SharedStore.CurrentRoom != null && SharedStore.CurrentRoom.roomId != null
            ? SharedStore.CurrentRoom.roomId.Trim()
            : string.Empty;

        return lobbyEventsSubscription != null &&
               !string.IsNullOrWhiteSpace(subscribedLobbyId) &&
               string.Equals(subscribedLobbyId, currentLobbyId, StringComparison.Ordinal) &&
               lobbyEventConnectionState != LobbyEventConnectionState.Unsubscribed;
    }

    private bool HasCurrentLobbyId()
    {
        RoomState currentRoom = SharedStore.CurrentRoom;
        return currentRoom != null && !string.IsNullOrWhiteSpace(currentRoom.roomId);
    }

    private static bool IsAuthReadyForLobbySync()
    {
        AuthStateStore authStore = AuthStateStore.Local;
        return authStore != null && authStore.IsAuthenticated;
    }

    private async void TryStartLobbyEventsSubscriptionAsync()
    {
        await StartLobbyEventsSubscriptionAsync();
    }

    private async Task StartLobbyEventsSubscriptionAsync()
    {
        if (!Application.isPlaying || !enableLobbyEventTracking || isLobbyEventsSubscriptionInProgress)
        {
            return;
        }

        if (!IsAuthReadyForLobbySync())
        {
            return;
        }

        RoomState currentRoom = SharedStore.CurrentRoom;
        string lobbyId = currentRoom != null && currentRoom.roomId != null ? currentRoom.roomId.Trim() : string.Empty;
        if (string.IsNullOrWhiteSpace(lobbyId))
        {
            return;
        }

        if (lobbyEventsSubscription != null &&
            string.Equals(subscribedLobbyId, lobbyId, StringComparison.Ordinal))
        {
            return;
        }

        isLobbyEventsSubscriptionInProgress = true;
        try
        {
            await StopLobbyEventsSubscriptionAsync();

            lobbyEventCallbacks = new LobbyEventCallbacks();
            lobbyEventCallbacks.LobbyChanged += OnLobbyChangedEventReceived;
            lobbyEventCallbacks.LobbyDeleted += OnLobbyDeletedEventReceived;
            lobbyEventCallbacks.KickedFromLobby += OnKickedFromLobbyEventReceived;
            lobbyEventCallbacks.LobbyEventConnectionStateChanged += OnLobbyConnectionStateChanged;

            lobbyEventsSubscription = await LobbyService.Instance.SubscribeToLobbyEventsAsync(lobbyId, lobbyEventCallbacks);
            subscribedLobbyId = lobbyId;
            lobbyEventConnectionState = LobbyEventConnectionState.Subscribing;
            hasHandledMissingLobby = false;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"CurrentRoomScreenBinder: Lobby events subscribe failed safely. {exception.Message}");
            await StopLobbyEventsSubscriptionAsync();
        }
        finally
        {
            isLobbyEventsSubscriptionInProgress = false;
        }
    }

    private async void TryStopLobbyEventsSubscriptionAsync()
    {
        await StopLobbyEventsSubscriptionAsync();
    }

    private async Task StopLobbyEventsSubscriptionAsync()
    {
        ILobbyEvents subscriptionToStop = lobbyEventsSubscription;
        LobbyEventCallbacks callbacksToDetach = lobbyEventCallbacks;

        lobbyEventsSubscription = null;
        lobbyEventCallbacks = null;
        subscribedLobbyId = string.Empty;
        lobbyEventConnectionState = LobbyEventConnectionState.Unsubscribed;

        if (callbacksToDetach != null)
        {
            callbacksToDetach.LobbyChanged -= OnLobbyChangedEventReceived;
            callbacksToDetach.LobbyDeleted -= OnLobbyDeletedEventReceived;
            callbacksToDetach.KickedFromLobby -= OnKickedFromLobbyEventReceived;
            callbacksToDetach.LobbyEventConnectionStateChanged -= OnLobbyConnectionStateChanged;
        }

        if (subscriptionToStop == null)
        {
            return;
        }

        try
        {
            await subscriptionToStop.UnsubscribeAsync();
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"CurrentRoomScreenBinder: Lobby events unsubscribe failed safely. {exception.Message}");
        }
    }

    private void OnLobbyChangedEventReceived(ILobbyChanges _)
    {
        EnqueueMainThreadAction(HandleLobbyChangedOnMainThread);
    }

    private void HandleLobbyChangedOnMainThread()
    {
        if (!isActiveAndEnabled || !Application.isPlaying)
        {
            return;
        }

        TryRefreshCurrentLobbyAsync();
    }

    private void OnLobbyDeletedEventReceived()
    {
        EnqueueMainThreadAction(() => HandleLobbyNotFound("deleted"));
    }

    private void OnKickedFromLobbyEventReceived()
    {
        EnqueueMainThreadAction(() => HandleLobbyNotFound("kicked"));
    }

    private void OnLobbyConnectionStateChanged(LobbyEventConnectionState connectionState)
    {
        EnqueueMainThreadAction(() => HandleLobbyConnectionStateChangedOnMainThread(connectionState));
    }

    private void HandleLobbyConnectionStateChangedOnMainThread(LobbyEventConnectionState connectionState)
    {
        lobbyEventConnectionState = connectionState;

        if (!isActiveAndEnabled || !Application.isPlaying)
        {
            return;
        }

        if (connectionState == LobbyEventConnectionState.Error ||
            connectionState == LobbyEventConnectionState.Unsynced ||
            connectionState == LobbyEventConnectionState.Unsubscribed)
        {
            TryStartLobbyEventsSubscriptionAsync();
            TryRefreshCurrentLobbyAsync();
        }
    }

    private async void TryRefreshCurrentLobbyAsync()
    {
        await RefreshCurrentLobbyAsync();
    }

    private async Task RefreshCurrentLobbyAsync()
    {
        if (!Application.isPlaying || isFallbackLobbyRefreshInProgress || hasHandledMissingLobby)
        {
            return;
        }

        if (!IsAuthReadyForLobbySync())
        {
            return;
        }

        RoomState currentRoom = SharedStore.CurrentRoom;
        string lobbyId = currentRoom != null && currentRoom.roomId != null ? currentRoom.roomId.Trim() : string.Empty;
        if (string.IsNullOrWhiteSpace(lobbyId))
        {
            return;
        }

        isFallbackLobbyRefreshInProgress = true;
        try
        {
            if (lobbyService == null)
            {
                lobbyService = new UnityLobbyService();
            }

            Lobby lobby = await lobbyService.GetLobbyAsync(lobbyId);
            if (lobby == null)
            {
                if (lobbyService.LastGetLobbyWasNotFound)
                {
                    HandleLobbyNotFound("not found");
                }

                return;
            }

            hasHandledMissingLobby = false;
            RoomState mappedRoom = lobbyService.MapLobbyToRoomState(lobby, false);
            if (mappedRoom != null && SharedStore.ApplyMappedCurrentRoom(mappedRoom, true))
            {
                ApplyCurrentRoomToUi();
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"CurrentRoomScreenBinder: Lobby fallback refresh failed safely. {exception.Message}");
        }
        finally
        {
            isFallbackLobbyRefreshInProgress = false;
        }
    }

    private void HandleLobbyNotFound(string reason)
    {
        if (hasHandledMissingLobby)
        {
            return;
        }

        hasHandledMissingLobby = true;
        Debug.Log($"CurrentRoomScreenBinder: Current lobby is no longer available ({reason}). Returning to browser.");

        SharedStore.ClearCurrentRoom();
        TryStopLobbyEventsSubscriptionAsync();
        NavigateBackToBrowser();
    }

    private void NavigateBackToBrowser()
    {
        AutoAssignReferences();

        if (screenSwitcher != null)
        {
            screenSwitcher.ShowRoomBrowser();
        }
        else
        {
            if (createRoomScreen != null)
            {
                createRoomScreen.SetActive(false);
            }

            if (currentRoomScreen != null)
            {
                currentRoomScreen.SetActive(false);
            }

            if (roomBrowserScreen != null)
            {
                roomBrowserScreen.SetActive(true);
            }
        }

        if (roomBrowserScreenBinder == null && roomBrowserScreen != null)
        {
            roomBrowserScreenBinder = roomBrowserScreen.GetComponentInChildren<RoomBrowserScreenBinder>(true);
        }

        if (roomBrowserScreenBinder != null)
        {
            roomBrowserScreenBinder.RefreshRoomList();
        }
    }

    private void EnqueueMainThreadAction(Action action)
    {
        if (action == null)
        {
            return;
        }

        lock (mainThreadActionsLock)
        {
            mainThreadActions.Enqueue(action);
        }
    }

    private void FlushMainThreadActions()
    {
        while (true)
        {
            Action action = null;
            lock (mainThreadActionsLock)
            {
                if (mainThreadActions.Count > 0)
                {
                    action = mainThreadActions.Dequeue();
                }
            }

            if (action == null)
            {
                break;
            }

            try
            {
                action.Invoke();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"CurrentRoomScreenBinder: Main-thread event action failed safely. {exception.Message}");
            }
        }
    }

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

        ApplyStartGameButtonState(currentRoom, safePlayerCount);
        ApplyPlayerSlotsVisuals(currentRoom, clampedPlayerCount, safeMaxPlayers);
        ApplySideMapPreviewVisual(currentRoom.selectedMapIndex);
        TryOpenMiniGameReadyIfNeeded(currentRoom);
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

        ApplyStartGameButtonState(null, 0);
        ApplyPlayerSlotsVisuals(null, 0, DefaultSlotCount);
        ApplySideMapPreviewVisual(0);
    }

    private void ApplyStartGameButtonState(RoomState currentRoom, int playerCount)
    {
        if (startGameButton == null)
        {
            return;
        }

        bool isLocalHost = IsLocalPlayerHost(currentRoom);
        startGameButton.gameObject.SetActive(isLocalHost);
        if (!isLocalHost)
        {
            startGameButton.interactable = false;
            return;
        }

        bool canStart = playerCount >= 2 && !IsMiniGameReadyPhase(currentRoom);
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

    private bool IsLocalPlayerHost(RoomState currentRoom)
    {
        if (currentRoom == null || currentRoom.players == null || SharedStore.LocalPlayer == null)
        {
            return false;
        }

        string localPlayerId = SharedStore.LocalPlayer.playerId != null
            ? SharedStore.LocalPlayer.playerId.Trim()
            : string.Empty;
        if (string.IsNullOrWhiteSpace(localPlayerId))
        {
            return false;
        }

        for (int i = 0; i < currentRoom.players.Count; i++)
        {
            PlayerState player = currentRoom.players[i];
            string playerId = player != null && player.playerId != null ? player.playerId.Trim() : string.Empty;
            if (!string.IsNullOrWhiteSpace(playerId) &&
                string.Equals(playerId, localPlayerId, StringComparison.Ordinal))
            {
                return player != null && player.isHost;
            }
        }

        return false;
    }

    private static bool IsMiniGameReadyPhase(RoomState room)
    {
        if (room == null || string.IsNullOrWhiteSpace(room.currentPhase))
        {
            return false;
        }

        return string.Equals(room.currentPhase.Trim(), RoomState.PhaseMiniGameReady, StringComparison.Ordinal);
    }

    private void TryOpenMiniGameReadyIfNeeded(RoomState currentRoom)
    {
        if (!isActiveAndEnabled || !IsMiniGameReadyPhase(currentRoom))
        {
            return;
        }

        if (miniGameReadyScreen != null && miniGameReadyScreen.activeSelf)
        {
            return;
        }

        OpenMiniGameReadyScreen();
    }

    private void ApplyPlayerSlotsVisuals(RoomState currentRoom, int playerCount, int maxPlayers)
    {
        if (playerSlots == null || playerSlots.Length == 0)
        {
            return;
        }

        string localPlayerId = SharedStore.LocalPlayer != null && SharedStore.LocalPlayer.playerId != null
            ? SharedStore.LocalPlayer.playerId.Trim()
            : string.Empty;
        PlayerState localPlayer = FindLocalPlayer(currentRoom, localPlayerId);
        int safeMaxPlayers = Mathf.Clamp(maxPlayers, 0, playerSlots.Length);
        List<PlayerState> slotPlayers = GetOrderedPlayersForSlots(currentRoom, safeMaxPlayers);
        int occupiedSlotCount = Mathf.Clamp(slotPlayers.Count, 0, safeMaxPlayers);

        for (int i = 0; i < playerSlots.Length; i++)
        {
            GameObject slot = playerSlots[i];
            if (slot == null)
            {
                continue;
            }

            if (i < occupiedSlotCount)
            {
                PlayerState slotPlayer = slotPlayers[i];
                bool isLocalSlot = localPlayer != null && AreSamePlayer(slotPlayer, localPlayer);
                if (isLocalSlot)
                {
                    ApplyLocalPlayerSlotVisual(i, slot, slotPlayer);
                }
                else
                {
                    ApplyOtherPlayerSlotVisual(i, slot, slotPlayer);
                }
            }
            else if (i < safeMaxPlayers)
            {
                ApplySlotVisualState(slot, SlotVisualState.Available);
                ClearSlotIdentity(i);
            }
            else
            {
                ApplySlotVisualState(slot, SlotVisualState.Locked);
                ClearSlotIdentity(i);
            }
        }
    }

    private static List<PlayerState> GetOrderedPlayersForSlots(RoomState currentRoom, int maxSlots)
    {
        List<PlayerState> slotPlayers = new List<PlayerState>();
        if (currentRoom == null || currentRoom.players == null || maxSlots <= 0)
        {
            return slotPlayers;
        }

        PlayerState hostPlayer = FindHostPlayer(currentRoom.players);
        if (hostPlayer != null)
        {
            slotPlayers.Add(hostPlayer);
        }

        for (int i = 0; i < currentRoom.players.Count; i++)
        {
            PlayerState candidate = currentRoom.players[i];
            if (candidate == null)
            {
                continue;
            }

            if (hostPlayer != null && AreSamePlayer(candidate, hostPlayer))
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

    private static PlayerState FindHostPlayer(List<PlayerState> players)
    {
        if (players == null || players.Count == 0)
        {
            return null;
        }

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

    private void ApplyLocalPlayerSlotVisual(int slotIndex, GameObject slot, PlayerState localPlayer)
    {
        if (slot == null)
        {
            return;
        }

        bool isReady = localPlayer != null && localPlayer.isReady;

        Image slotBackground = slot.GetComponent<Image>();
        Transform avatarTransform = slot.transform.Find("AvatarPlaceholder");
        Image avatarImage = avatarTransform != null ? avatarTransform.GetComponent<Image>() : null;
        Button statusButton = GetSlotStatusButton(slotIndex);
        Image statusButtonImage = GetSlotStatusButtonImage(slotIndex);
        TMP_Text statusText = GetSlotStatusText(slotIndex);

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

        ApplySlotIdentity(slotIndex, localPlayer, true);
    }

    private void ApplyOtherPlayerSlotVisual(int slotIndex, GameObject slot, PlayerState slotPlayer)
    {
        if (slot == null)
        {
            return;
        }

        bool isReady = slotPlayer != null && slotPlayer.isReady;

        Image slotBackground = slot.GetComponent<Image>();
        Transform avatarTransform = slot.transform.Find("AvatarPlaceholder");
        Image avatarImage = avatarTransform != null ? avatarTransform.GetComponent<Image>() : null;
        Button statusButton = GetSlotStatusButton(slotIndex);
        Image statusButtonImage = GetSlotStatusButtonImage(slotIndex);
        TMP_Text statusText = GetSlotStatusText(slotIndex);

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

        ApplySlotIdentity(slotIndex, slotPlayer, false);
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
        BindSlotStatusButtonEvents();

        if (copyRoomIdButton != null)
        {
            copyRoomIdButton.onClick.RemoveListener(OnCopyRoomIdButtonClicked);
            copyRoomIdButton.onClick.AddListener(OnCopyRoomIdButtonClicked);
        }

        if (startGameButton != null)
        {
            startGameButton.onClick.RemoveListener(OnStartGameButtonClicked);
            startGameButton.onClick.AddListener(OnStartGameButtonClicked);
        }
    }

    private void UnbindUiEvents()
    {
        UnbindSlotStatusButtonEvents();

        if (copyRoomIdButton != null)
        {
            copyRoomIdButton.onClick.RemoveListener(OnCopyRoomIdButtonClicked);
        }

        if (startGameButton != null)
        {
            startGameButton.onClick.RemoveListener(OnStartGameButtonClicked);
        }
    }

    private async void OnStartGameButtonClicked()
    {
        if (isStartGamePhaseUpdateInProgress)
        {
            return;
        }

        RoomState currentRoom = SharedStore.CurrentRoom;
        if (currentRoom == null || !startGameButton.interactable)
        {
            return;
        }

        string localPlayerId = SharedStore.LocalPlayer != null ? SharedStore.LocalPlayer.playerId : string.Empty;
        if (string.IsNullOrWhiteSpace(localPlayerId) || currentRoom.players == null)
        {
            return;
        }

        PlayerState localPlayer = null;
        for (int i = 0; i < currentRoom.players.Count; i++)
        {
            PlayerState candidate = currentRoom.players[i];
            string candidateId = candidate != null && candidate.playerId != null ? candidate.playerId.Trim() : string.Empty;
            if (!string.IsNullOrWhiteSpace(candidateId) &&
                string.Equals(candidateId, localPlayerId, StringComparison.Ordinal))
            {
                localPlayer = candidate;
                break;
            }
        }

        if (localPlayer == null || !localPlayer.isHost)
        {
            return;
        }

        string lobbyId = currentRoom.roomId != null ? currentRoom.roomId.Trim() : string.Empty;
        if (!IsAuthReadyForLobbySync() || string.IsNullOrWhiteSpace(lobbyId))
        {
            return;
        }

        if (IsMiniGameReadyPhase(currentRoom))
        {
            OpenMiniGameReadyScreen();
            return;
        }

        if (lobbyService == null)
        {
            lobbyService = new UnityLobbyService();
        }

        isStartGamePhaseUpdateInProgress = true;
        bool phaseUpdated = false;
        try
        {
            phaseUpdated = await lobbyService.UpdateLobbyPhaseAsync(lobbyId, RoomState.PhaseMiniGameReady);
        }
        finally
        {
            isStartGamePhaseUpdateInProgress = false;
        }

        if (!phaseUpdated)
        {
            Debug.LogWarning("CurrentRoomScreenBinder: Start Game phase update failed safely.");
            return;
        }

        currentRoom.currentPhase = RoomState.PhaseMiniGameReady;
        SharedStore.ApplyMappedCurrentRoom(currentRoom, true);
        OpenMiniGameReadyScreen();
    }

    private void OpenMiniGameReadyScreen()
    {
        AutoAssignReferences();

        if (miniGameReadyScreen != null && miniGameReadyScreen.activeSelf)
        {
            return;
        }

        if (roomBrowserScreen != null)
        {
            roomBrowserScreen.SetActive(false);
        }

        if (createRoomScreen != null)
        {
            createRoomScreen.SetActive(false);
        }

        if (currentRoomScreen != null)
        {
            currentRoomScreen.SetActive(false);
        }

        if (miniGameReadyScreen != null)
        {
            miniGameReadyScreen.SetActive(true);
        }

        if (miniGameReadyScreenBinder == null && miniGameReadyScreen != null)
        {
            miniGameReadyScreenBinder = miniGameReadyScreen.GetComponent<MiniGameReadyScreenBinder>();
        }

        if (miniGameReadyScreenBinder != null)
        {
            miniGameReadyScreenBinder.RefreshFromCurrentRoom();
        }
    }

    private void OnPlayerSlot01StatusClicked()
    {
        OnPlayerSlotStatusClicked(0);
    }

    private async void OnPlayerSlotStatusClicked(int slotIndex)
    {
        if (slotIndex < 0 || isReadySyncInProgress)
        {
            return;
        }

        RoomState currentRoom = SharedStore.CurrentRoom;
        string localPlayerId = SharedStore.LocalPlayer != null ? SharedStore.LocalPlayer.playerId : string.Empty;
        PlayerState localPlayer = FindLocalPlayer(currentRoom, localPlayerId);
        if (currentRoom == null || localPlayer == null)
        {
            return;
        }

        int safeMaxPlayers = Mathf.Clamp(
            currentRoom.maxPlayers,
            1,
            Mathf.Max(1, playerSlots != null ? playerSlots.Length : DefaultSlotCount));
        List<PlayerState> slotPlayers = GetOrderedPlayersForSlots(currentRoom, safeMaxPlayers);
        if (slotIndex >= slotPlayers.Count)
        {
            return;
        }

        PlayerState slotPlayer = slotPlayers[slotIndex];
        if (slotPlayer == null || !AreSamePlayer(slotPlayer, localPlayer))
        {
            return;
        }

        bool previousReadyState = localPlayer.isReady;
        bool nextReadyState = !previousReadyState;
        localPlayer.isReady = nextReadyState;
        ApplyCurrentRoomToUi();

        string lobbyId = currentRoom.roomId != null ? currentRoom.roomId.Trim() : string.Empty;
        if (!IsAuthReadyForLobbySync() || string.IsNullOrWhiteSpace(lobbyId))
        {
            return;
        }

        if (lobbyService == null)
        {
            lobbyService = new UnityLobbyService();
        }

        bool updateSucceeded = false;
        isReadySyncInProgress = true;
        try
        {
            string safeDisplayName = ResolveSlotDisplayName(localPlayer, true);
            updateSucceeded = await lobbyService.UpdatePlayerReadyAsync(
                lobbyId,
                nextReadyState,
                localPlayer.playerId,
                safeDisplayName);
        }
        finally
        {
            isReadySyncInProgress = false;
        }

        if (!updateSucceeded)
        {
            localPlayer.isReady = previousReadyState;
            ApplyCurrentRoomToUi();
            return;
        }

        TryRefreshCurrentLobbyAsync();
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
        Transform[] allRootTransforms = transform.root.GetComponentsInChildren<Transform>(true);

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

        if (screenSwitcher == null)
        {
            screenSwitcher = FindObjectOfType<LobbyMockScreenSwitcher>(true);
        }

        if (roomBrowserScreen == null)
        {
            roomBrowserScreen = FindGameObjectByName(allRootTransforms, "RoomBrowserScreen");
        }

        if (createRoomScreen == null)
        {
            createRoomScreen = FindGameObjectByName(allRootTransforms, "CreateRoomScreen");
        }

        if (currentRoomScreen == null)
        {
            currentRoomScreen = FindGameObjectByName(allRootTransforms, "CurrentRoomScreen");
        }

        if (miniGameReadyScreen == null)
        {
            miniGameReadyScreen = FindGameObjectByName(allRootTransforms, "MiniGameReadyScreen");
        }

        if (roomBrowserScreenBinder == null && roomBrowserScreen != null)
        {
            roomBrowserScreenBinder = roomBrowserScreen.GetComponentInChildren<RoomBrowserScreenBinder>(true);
        }

        if (miniGameReadyScreenBinder == null && miniGameReadyScreen != null)
        {
            miniGameReadyScreenBinder = miniGameReadyScreen.GetComponent<MiniGameReadyScreenBinder>();
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

        EnsureSlotIdentityTexts(allowCreateGeneratedUi);
        EnsurePlayerSlot01IdentityTexts(allowCreateGeneratedUi);
        EnsureHostStatusSprites();
    }

    private void BindSlotStatusButtonEvents()
    {
        if (slotStatusButtonHandlers == null)
        {
            return;
        }

        for (int i = 0; i < DefaultSlotCount; i++)
        {
            Button slotStatusButton = GetSlotStatusButton(i);
            if (slotStatusButton == null)
            {
                continue;
            }

            if (slotStatusButtonHandlers[i] != null)
            {
                slotStatusButton.onClick.RemoveListener(slotStatusButtonHandlers[i]);
            }

            int capturedIndex = i;
            UnityAction handler = () => OnPlayerSlotStatusClicked(capturedIndex);
            slotStatusButtonHandlers[i] = handler;
            slotStatusButton.onClick.AddListener(handler);
        }
    }

    private void UnbindSlotStatusButtonEvents()
    {
        if (slotStatusButtonHandlers == null)
        {
            return;
        }

        for (int i = 0; i < DefaultSlotCount; i++)
        {
            UnityAction handler = slotStatusButtonHandlers[i];
            if (handler == null)
            {
                continue;
            }

            Button slotStatusButton = GetSlotStatusButton(i);
            if (slotStatusButton != null)
            {
                slotStatusButton.onClick.RemoveListener(handler);
            }

            slotStatusButtonHandlers[i] = null;
        }
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

    private void ApplySlotIdentity(int slotIndex, PlayerState slotPlayer, bool isLocalSlot)
    {
        TMP_Text slotNameText = GetSlotNameText(slotIndex);
        TMP_Text slotHostBadgeText = GetSlotHostBadgeText(slotIndex);

        if (slotNameText == null && slotHostBadgeText == null)
        {
            return;
        }

        if (slotPlayer == null)
        {
            ClearSlotIdentity(slotIndex);
            return;
        }

        string displayName = ResolveSlotDisplayName(slotPlayer, isLocalSlot);
        if (slotNameText != null)
        {
            slotNameText.text = displayName;
            slotNameText.gameObject.SetActive(true);
        }

        if (slotHostBadgeText != null)
        {
            bool isHost = slotPlayer.isHost;
            slotHostBadgeText.text = isHost ? localPlayerHostBadgeLabel : string.Empty;
            slotHostBadgeText.gameObject.SetActive(isHost);
        }
    }

    private void ClearSlotIdentity(int slotIndex)
    {
        TMP_Text slotNameText = GetSlotNameText(slotIndex, false);
        TMP_Text slotHostBadgeText = GetSlotHostBadgeText(slotIndex, false);

        if (slotNameText != null)
        {
            slotNameText.text = string.Empty;
            slotNameText.gameObject.SetActive(false);
        }

        if (slotHostBadgeText != null)
        {
            slotHostBadgeText.text = string.Empty;
            slotHostBadgeText.gameObject.SetActive(false);
        }
    }

    private string ResolveSlotDisplayName(PlayerState slotPlayer, bool isLocalSlot)
    {
        if (slotPlayer != null)
        {
            string playerName = slotPlayer.displayName != null ? slotPlayer.displayName.Trim() : string.Empty;
            if (!string.IsNullOrWhiteSpace(playerName))
            {
                return playerName;
            }
        }

        if (isLocalSlot && SharedStore.LocalPlayer != null)
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
        Button slotStatusButton = GetSlotStatusButton(slotIndex);
        if (slotStatusButton == null)
        {
            return null;
        }

        return slotStatusButton.GetComponent<Image>();
    }

    private Button GetSlotStatusButton(int slotIndex)
    {
        if (playerSlots == null || slotIndex < 0 || slotIndex >= playerSlots.Length || playerSlots[slotIndex] == null)
        {
            return null;
        }

        Transform statusTransform = playerSlots[slotIndex].transform.Find("StatusButton");
        return statusTransform != null ? statusTransform.GetComponent<Button>() : null;
    }

    private TMP_Text GetSlotStatusText(int slotIndex)
    {
        Button slotStatusButton = GetSlotStatusButton(slotIndex);
        if (slotStatusButton == null)
        {
            return null;
        }

        Transform labelTransform = slotStatusButton.transform.Find("Label");
        return labelTransform != null ? labelTransform.GetComponent<TMP_Text>() : null;
    }

    private void EnsureSlotIdentityTexts(bool allowCreateGeneratedUi = true)
    {
        if (playerSlots == null)
        {
            return;
        }

        for (int i = 0; i < playerSlots.Length; i++)
        {
            GetSlotNameText(i, allowCreateGeneratedUi);
            GetSlotHostBadgeText(i, allowCreateGeneratedUi);
        }
    }

    private TMP_Text GetSlotNameText(int slotIndex, bool allowCreateGeneratedUi = true)
    {
        if (slotIndex == 0)
        {
            EnsurePlayerSlot01IdentityTexts(allowCreateGeneratedUi);
            return playerSlot01NameText;
        }

        Transform slotRoot = GetSlotRoot(slotIndex);
        if (slotRoot == null)
        {
            return null;
        }

        Transform existingName = slotRoot.Find("PlayerNameText");
        TMP_Text slotNameText = existingName != null ? existingName.GetComponent<TMP_Text>() : null;

        if (slotNameText == null && allowCreateGeneratedUi)
        {
            slotNameText = CreateSlotIdentityText(slotRoot, "PlayerNameText", new Vector2(14f, -12f), new Vector2(200f, 24f), 16f);
        }

        TMP_Text slotHostBadgeText = GetSlotHostBadgeText(slotIndex, allowCreateGeneratedUi);
        ApplySlotIdentityTextStyle(slotNameText, slotHostBadgeText, slotIndex);
        return slotNameText;
    }

    private TMP_Text GetSlotHostBadgeText(int slotIndex, bool allowCreateGeneratedUi = true)
    {
        if (slotIndex == 0)
        {
            EnsurePlayerSlot01IdentityTexts(allowCreateGeneratedUi);
            return playerSlot01HostBadgeText;
        }

        Transform slotRoot = GetSlotRoot(slotIndex);
        if (slotRoot == null)
        {
            return null;
        }

        Transform existingHostBadge = slotRoot.Find("HostBadgeText");
        TMP_Text slotHostBadgeText = existingHostBadge != null ? existingHostBadge.GetComponent<TMP_Text>() : null;

        if (slotHostBadgeText == null && allowCreateGeneratedUi)
        {
            slotHostBadgeText = CreateSlotIdentityText(slotRoot, "HostBadgeText", new Vector2(14f, -32f), new Vector2(120f, 20f), 14f);
        }

        TMP_Text slotNameText = null;
        Transform existingName = slotRoot.Find("PlayerNameText");
        if (existingName != null)
        {
            slotNameText = existingName.GetComponent<TMP_Text>();
        }

        ApplySlotIdentityTextStyle(slotNameText, slotHostBadgeText, slotIndex);
        return slotHostBadgeText;
    }

    private void ApplySlotIdentityTextStyle(TMP_Text slotNameText, TMP_Text slotHostBadgeText, int slotIndex)
    {
        if (slotNameText != null)
        {
            slotNameText.color = localPlayerNameColor;
            slotNameText.fontStyle = FontStyles.Bold;
            slotNameText.alignment = TextAlignmentOptions.MidlineLeft;
            slotNameText.overflowMode = TextOverflowModes.Ellipsis;
            slotNameText.enableWordWrapping = false;
            slotNameText.raycastTarget = false;
        }

        if (slotHostBadgeText != null)
        {
            slotHostBadgeText.color = localPlayerHostBadgeColor;
            slotHostBadgeText.fontStyle = FontStyles.Bold;
            slotHostBadgeText.alignment = TextAlignmentOptions.MidlineLeft;
            slotHostBadgeText.overflowMode = TextOverflowModes.Ellipsis;
            slotHostBadgeText.enableWordWrapping = false;
            slotHostBadgeText.raycastTarget = false;
        }

        TMP_Text statusText = GetSlotStatusText(slotIndex);
        TMP_FontAsset fallbackFont = statusText != null ? statusText.font : null;
        if (fallbackFont == null)
        {
            return;
        }

        if (slotNameText != null && slotNameText.font == null)
        {
            slotNameText.font = fallbackFont;
        }

        if (slotHostBadgeText != null && slotHostBadgeText.font == null)
        {
            slotHostBadgeText.font = fallbackFont;
        }
    }

    private Transform GetSlotRoot(int slotIndex)
    {
        if (playerSlots == null || slotIndex < 0 || slotIndex >= playerSlots.Length)
        {
            return null;
        }

        GameObject slot = playerSlots[slotIndex];
        return slot != null ? slot.transform : null;
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
