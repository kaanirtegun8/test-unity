using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class LobbyMockScreenSwitcher : MonoBehaviour
{
    [SerializeField] private GameObject roomBrowserScreen;
    [SerializeField] private GameObject createRoomScreen;
    [SerializeField] private GameObject currentRoomScreen;
    [SerializeField] private RoomBrowserScreenBinder roomBrowserScreenBinder;
    [SerializeField] private Button createRoomButton;
    [SerializeField] private Button createButton;
    [SerializeField] private Button backButton;
    [SerializeField] private Button leaveButton;
    [SerializeField] private bool useUnityLobbyCreate = true;

    private UnityLobbyService unityLobbyService;
    private bool isCreateInProgress;
    private bool isLeaveInProgress;

    private void Awake()
    {
        unityLobbyService = new UnityLobbyService();
        AutoAssignReferences();
        BindButtons();
        ShowRoomBrowser();
    }

    private void OnDestroy()
    {
        UnbindButtons();
    }

    private void BindButtons()
    {
        if (createRoomButton != null)
        {
            createRoomButton.onClick.RemoveListener(ShowCreateRoom);
            createRoomButton.onClick.AddListener(ShowCreateRoom);
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveListener(ShowRoomBrowser);
            backButton.onClick.AddListener(ShowRoomBrowser);
        }

        if (createButton != null)
        {
            createButton.onClick.RemoveListener(ShowCurrentRoom);
            createButton.onClick.AddListener(ShowCurrentRoom);
        }

        if (leaveButton != null)
        {
            leaveButton.onClick.RemoveListener(OnLeaveButtonClicked);
            leaveButton.onClick.AddListener(OnLeaveButtonClicked);
        }
    }

    private void UnbindButtons()
    {
        if (createRoomButton != null)
        {
            createRoomButton.onClick.RemoveListener(ShowCreateRoom);
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveListener(ShowRoomBrowser);
        }

        if (createButton != null)
        {
            createButton.onClick.RemoveListener(ShowCurrentRoom);
        }

        if (leaveButton != null)
        {
            leaveButton.onClick.RemoveListener(OnLeaveButtonClicked);
        }
    }

    private void AutoAssignReferences()
    {
        Transform[] allTransforms = transform.root.GetComponentsInChildren<Transform>(true);

        if (roomBrowserScreen == null)
        {
            roomBrowserScreen = FindGameObjectByName(allTransforms, "RoomBrowserScreen");
        }

        if (createRoomScreen == null)
        {
            createRoomScreen = FindGameObjectByName(allTransforms, "CreateRoomScreen");
        }

        if (currentRoomScreen == null)
        {
            currentRoomScreen = FindGameObjectByName(allTransforms, "CurrentRoomScreen");
        }

        if (roomBrowserScreenBinder == null && roomBrowserScreen != null)
        {
            roomBrowserScreenBinder = roomBrowserScreen.GetComponentInChildren<RoomBrowserScreenBinder>(true);
        }

        if (createRoomButton == null)
        {
            GameObject createButtonObject = FindGameObjectByName(allTransforms, "CreateRoomButton");
            if (createButtonObject != null)
            {
                createRoomButton = createButtonObject.GetComponent<Button>();
            }
        }

        if (createButton == null)
        {
            GameObject createActionButtonObject = FindGameObjectByName(allTransforms, "CreateButton");
            if (createActionButtonObject != null)
            {
                createButton = createActionButtonObject.GetComponent<Button>();
            }
        }

        if (backButton == null)
        {
            GameObject backButtonObject = FindGameObjectByName(allTransforms, "BackButton");
            if (backButtonObject != null)
            {
                backButton = backButtonObject.GetComponent<Button>();
            }
        }

        if (leaveButton == null)
        {
            GameObject leaveButtonObject = FindGameObjectByName(allTransforms, "LeaveButton");
            if (leaveButtonObject != null)
            {
                leaveButton = leaveButtonObject.GetComponent<Button>();
            }
        }
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

    public void ShowCreateRoom()
    {
        if (roomBrowserScreen != null)
        {
            roomBrowserScreen.SetActive(false);
        }

        if (createRoomScreen != null)
        {
            createRoomScreen.SetActive(true);
        }

        if (currentRoomScreen != null)
        {
            currentRoomScreen.SetActive(false);
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

    public void ShowRoomBrowser()
    {
        if (createRoomScreen != null)
        {
            createRoomScreen.SetActive(false);
        }

        if (roomBrowserScreen != null)
        {
            roomBrowserScreen.SetActive(true);
        }

        if (currentRoomScreen != null)
        {
            currentRoomScreen.SetActive(false);
        }
    }

    public async void ShowCurrentRoom()
    {
        if (isCreateInProgress)
        {
            return;
        }

        isCreateInProgress = true;
        LobbyStateStore store = LobbyStateStore.Local;
        bool shouldOpenCurrentRoomScreen = false;

        try
        {
            RoomDraft draft = store.CurrentDraft;
            if (draft == null)
            {
                store.ResetDraft();
                draft = store.CurrentDraft;
            }

            if (useUnityLobbyCreate)
            {
                if (!IsAuthReadyForLobbyCreate())
                {
                    Debug.LogWarning("LobbyMockScreenSwitcher: Create skipped because auth is not ready.");
                }
                else
                {
                    Lobby createdLobby = await TryCreateLobbyFromDraftAsync(draft);
                    if (createdLobby != null)
                    {
                        ApplyCreatedLobbyToStore(store, createdLobby);
                        shouldOpenCurrentRoomScreen = true;
                    }
                    else
                    {
                        Debug.LogWarning("LobbyMockScreenSwitcher: Unity Lobby create failed safely. CurrentRoomScreen will not open.");
                    }
                }
            }
            else
            {
                store.CreateRoomFromDraft();
                shouldOpenCurrentRoomScreen = true;
            }
        }
        finally
        {
            isCreateInProgress = false;
        }

        if (!shouldOpenCurrentRoomScreen)
        {
            return;
        }

        if (createRoomScreen != null)
        {
            createRoomScreen.SetActive(false);
        }

        if (roomBrowserScreen != null)
        {
            roomBrowserScreen.SetActive(false);
        }

        if (currentRoomScreen != null)
        {
            currentRoomScreen.SetActive(true);
        }
    }

    private static bool IsAuthReadyForLobbyCreate()
    {
        AuthStateStore authStore = AuthStateStore.Local;
        return authStore != null && authStore.IsAuthenticated;
    }

    private async Task<Lobby> TryCreateLobbyFromDraftAsync(RoomDraft draft)
    {
        if (unityLobbyService == null)
        {
            unityLobbyService = new UnityLobbyService();
        }

        if (draft == null)
        {
            return null;
        }

        try
        {
            return await unityLobbyService.CreateLobbyAsync(
                draft.roomName,
                draft.maxPlayers,
                !draft.isPublic,
                draft.treasureCount,
                draft.selectedMapIndex);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"LobbyMockScreenSwitcher: Unity Lobby create threw safely. {exception.Message}");
            return null;
        }
    }

    private void ApplyCreatedLobbyToStore(LobbyStateStore store, Lobby createdLobby)
    {
        if (store == null || createdLobby == null)
        {
            return;
        }

        if (unityLobbyService == null)
        {
            unityLobbyService = new UnityLobbyService();
        }

        RoomState mappedRoom = unityLobbyService.MapLobbyToRoomState(createdLobby, false);
        if (mappedRoom == null)
        {
            return;
        }

        // Create response may rarely omit player list; keep local creator visible safely.
        if (mappedRoom.players == null)
        {
            mappedRoom.players = new List<PlayerState>();
        }

        if (mappedRoom.players.Count == 0 && store.LocalPlayer != null)
        {
            mappedRoom.players.Add(new PlayerState
            {
                playerId = store.LocalPlayer.playerId,
                displayName = store.LocalPlayer.displayName,
                isReady = false,
                isHost = true,
                selectedColorIndex = Mathf.Max(0, store.LocalPlayer.selectedColorIndex)
            });
        }

        store.SetCurrentRoom(mappedRoom);
        UpsertRoom(store.Rooms, mappedRoom);
    }

    private static void UpsertRoom(List<RoomState> rooms, RoomState room)
    {
        if (rooms == null || room == null)
        {
            return;
        }

        for (int i = 0; i < rooms.Count; i++)
        {
            RoomState existing = rooms[i];
            if (existing == null)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(existing.roomId) &&
                !string.IsNullOrWhiteSpace(room.roomId) &&
                existing.roomId == room.roomId)
            {
                rooms[i] = room;
                return;
            }
        }

        rooms.Add(room);
    }

    private async void OnLeaveButtonClicked()
    {
        if (isLeaveInProgress)
        {
            return;
        }

        isLeaveInProgress = true;
        bool canLeave = true;
        LobbyStateStore store = LobbyStateStore.Local;

        try
        {
            RoomState currentRoom = store.CurrentRoom;
            LocalPlayerProfile localPlayer = store.LocalPlayer;
            string lobbyId = currentRoom != null && currentRoom.roomId != null ? currentRoom.roomId.Trim() : string.Empty;
            string playerId = localPlayer != null && localPlayer.playerId != null ? localPlayer.playerId.Trim() : string.Empty;
            bool isAuthReady = AuthStateStore.Local != null && AuthStateStore.Local.IsAuthenticated;

            if (!isAuthReady)
            {
                Debug.LogWarning("LobbyMockScreenSwitcher: Leave skipped because auth is not ready.");
                return;
            }

            if (string.IsNullOrWhiteSpace(lobbyId))
            {
                Debug.LogWarning("LobbyMockScreenSwitcher: Leave skipped because lobbyId is missing.");
                return;
            }

            bool isLocalHost = IsLocalHost(currentRoom, playerId);
            if (isLocalHost)
            {
                canLeave = await TryDeleteLobbyAsync(lobbyId);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(playerId))
                {
                    Debug.LogWarning("LobbyMockScreenSwitcher: Leave skipped because playerId is missing.");
                    return;
                }

                canLeave = await TryLeaveLobbyAsync(lobbyId, playerId);
            }

            if (!canLeave)
            {
                Debug.LogWarning($"LobbyMockScreenSwitcher: Leave failed safely for lobbyId={lobbyId}.");
                return;
            }
        }
        finally
        {
            isLeaveInProgress = false;
        }

        store.LeaveCurrentRoomAsLocalPlayer();
        ShowRoomBrowser();

        if (roomBrowserScreenBinder == null && roomBrowserScreen != null)
        {
            roomBrowserScreenBinder = roomBrowserScreen.GetComponentInChildren<RoomBrowserScreenBinder>(true);
        }

        if (roomBrowserScreenBinder != null)
        {
            roomBrowserScreenBinder.RefreshRoomList();
        }
    }

    private async Task<bool> TryLeaveLobbyAsync(string lobbyId, string playerId)
    {
        if (unityLobbyService == null)
        {
            unityLobbyService = new UnityLobbyService();
        }

        try
        {
            return await unityLobbyService.LeaveLobbyAsync(lobbyId, playerId);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"LobbyMockScreenSwitcher: Unity Lobby leave threw safely. {exception.Message}");
            return false;
        }
    }

    private async Task<bool> TryDeleteLobbyAsync(string lobbyId)
    {
        if (unityLobbyService == null)
        {
            unityLobbyService = new UnityLobbyService();
        }

        try
        {
            return await unityLobbyService.DeleteLobbyAsync(lobbyId);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"LobbyMockScreenSwitcher: Unity Lobby delete threw safely. {exception.Message}");
            return false;
        }
    }

    private static bool IsLocalHost(RoomState room, string localPlayerId)
    {
        if (room == null || room.players == null || string.IsNullOrWhiteSpace(localPlayerId))
        {
            return false;
        }

        string safeLocalId = localPlayerId.Trim();
        for (int i = 0; i < room.players.Count; i++)
        {
            PlayerState player = room.players[i];
            if (player == null)
            {
                continue;
            }

            string playerId = player.playerId != null ? player.playerId.Trim() : string.Empty;
            if (string.Equals(playerId, safeLocalId, StringComparison.Ordinal))
            {
                return player.isHost;
            }
        }

        return false;
    }
}
