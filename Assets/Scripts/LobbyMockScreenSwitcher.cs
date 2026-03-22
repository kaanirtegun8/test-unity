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

        try
        {
            RoomDraft draft = store.CurrentDraft;
            if (draft == null)
            {
                store.ResetDraft();
                draft = store.CurrentDraft;
            }

            Lobby createdLobby = null;
            if (useUnityLobbyCreate && draft != null)
            {
                createdLobby = await TryCreateLobbyFromDraftAsync(draft);
            }

            if (createdLobby != null)
            {
                ApplyCreatedLobbyToStore(store, createdLobby, draft);
            }
            else
            {
                store.CreateRoomFromDraft();
            }
        }
        finally
        {
            isCreateInProgress = false;
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

    private static void ApplyCreatedLobbyToStore(LobbyStateStore store, Lobby createdLobby, RoomDraft draft)
    {
        if (store == null || createdLobby == null)
        {
            return;
        }

        int safeMaxPlayers = Mathf.Max(1, createdLobby.MaxPlayers);
        string lobbyRoomName = !string.IsNullOrWhiteSpace(createdLobby.Name) ? createdLobby.Name : GetLobbyDataValue(createdLobby, "roomName");
        string safeRoomName = !string.IsNullOrWhiteSpace(lobbyRoomName)
            ? lobbyRoomName
            : (draft != null && !string.IsNullOrWhiteSpace(draft.roomName) ? draft.roomName : "Fun Match");
        int treasureCount = ParseLobbyDataInt(createdLobby, "treasureCount", draft != null ? draft.treasureCount : 0);
        int selectedMapIndex = ParseLobbyDataInt(createdLobby, "selectedMapIndex", draft != null ? draft.selectedMapIndex : 0);

        LocalPlayerProfile localPlayer = store.LocalPlayer;
        string localPlayerId = localPlayer != null && !string.IsNullOrWhiteSpace(localPlayer.playerId)
            ? localPlayer.playerId
            : "local_player";
        string localDisplayName = localPlayer != null && !string.IsNullOrWhiteSpace(localPlayer.displayName)
            ? localPlayer.displayName
            : "You";
        int localColorIndex = localPlayer != null ? Mathf.Max(0, localPlayer.selectedColorIndex) : 0;

        RoomState mappedRoom = new RoomState
        {
            roomId = !string.IsNullOrWhiteSpace(createdLobby.Id)
                ? createdLobby.Id
                : (!string.IsNullOrWhiteSpace(createdLobby.LobbyCode) ? createdLobby.LobbyCode : "000000"),
            roomName = safeRoomName,
            isPublic = !createdLobby.IsPrivate,
            maxPlayers = safeMaxPlayers,
            selectedMapIndex = Mathf.Max(0, selectedMapIndex),
            treasureCount = Mathf.Max(0, treasureCount),
            players = new List<PlayerState>
            {
                new PlayerState
                {
                    playerId = localPlayerId,
                    displayName = localDisplayName,
                    isReady = false,
                    isHost = true,
                    selectedColorIndex = localColorIndex
                }
            }
        };

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

    private static int ParseLobbyDataInt(Lobby lobby, string key, int fallback)
    {
        string value = GetLobbyDataValue(lobby, key);
        if (int.TryParse(value, out int parsed))
        {
            return parsed;
        }

        return fallback;
    }

    private static string GetLobbyDataValue(Lobby lobby, string key)
    {
        if (lobby == null || lobby.Data == null || string.IsNullOrWhiteSpace(key))
        {
            return string.Empty;
        }

        if (!lobby.Data.TryGetValue(key, out DataObject dataObject) || dataObject == null)
        {
            return string.Empty;
        }

        return dataObject.Value ?? string.Empty;
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

            bool hasLeaveIdentifiers = !string.IsNullOrWhiteSpace(lobbyId) && !string.IsNullOrWhiteSpace(playerId);
            if (hasLeaveIdentifiers)
            {
                canLeave = await TryLeaveLobbyAsync(lobbyId, playerId);
                if (!canLeave)
                {
                    Debug.LogWarning($"LobbyMockScreenSwitcher: Leave failed safely for lobbyId={lobbyId}.");
                    return;
                }
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
}
