using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.Services.Lobbies.Models;

public class RoomBrowserScreenBinder : MonoBehaviour
{
    private const string RuntimeItemNamePrefix = "RuntimeRoomItem_";
    private const string LobbyDataKeyRoomName = "roomName";
    private const string LobbyDataKeyTreasureCount = "treasureCount";
    private const string LobbyDataKeySelectedMapIndex = "selectedMapIndex";
    private const float RowHeight = 170f;
    private const float RowSpacing = 14f;
    private const float RowHorizontalPadding = -8f;

    private static readonly Vector2 MapAnchoredPosition = new Vector2(300f, 0f);
    private static readonly Vector2 MapSize = new Vector2(230f, 132f);
    private static readonly Vector2 RoomNameAnchoredPosition = new Vector2(620f, 18f);
    private static readonly Vector2 RoomNameSize = new Vector2(430f, 48f);
    private static readonly Vector2 PlayerCountAnchoredPosition = new Vector2(1080f, 0f);
    private static readonly Vector2 PlayerCountSize = new Vector2(140f, 84f);
    private static readonly Vector2 RoomIdTextAnchoredPosition = new Vector2(620f, -20f);
    private static readonly Vector2 RoomIdTextSize = new Vector2(430f, 34f);
    private static readonly Vector2 RewardAreaAnchoredPosition = new Vector2(-390f, 0f);
    private static readonly Vector2 RewardAreaSize = new Vector2(220f, 92f);
    private static readonly Vector2 JoinButtonAnchoredPosition = new Vector2(-24f, 0f);
    private static readonly Vector2 JoinButtonSize = new Vector2(260f, 130f);
    private static readonly Color[] MapThumbnailPalette =
    {
        new Color(0.27f, 0.44f, 0.66f, 1f),
        new Color(0.35f, 0.52f, 0.40f, 1f),
        new Color(0.48f, 0.34f, 0.34f, 1f),
        new Color(0.34f, 0.55f, 0.62f, 1f),
        new Color(0.47f, 0.38f, 0.66f, 1f),
        new Color(0.64f, 0.43f, 0.26f, 1f)
    };

    [SerializeField] private RectTransform contentRoot;
    [SerializeField] private GameObject roomListItemPrefab;
    [SerializeField] private GameObject roomBrowserScreen;
    [SerializeField] private GameObject createRoomScreen;
    [SerializeField] private GameObject currentRoomScreen;
    [SerializeField] private Button createRoomButton;
    [SerializeField] private Button refreshButton;
    [SerializeField] private Button filterRoomsButton;
    [SerializeField] private bool refreshOnEnable;
    [SerializeField] private bool hideTemplateItemOnRuntime = true;
    [SerializeField] private bool logRefreshInfo = true;
    [SerializeField] private bool useUnityLobbyQuery = true;

    private Transform templateItemTransform;
    private int lastRefreshFrame = -1;
    private bool isRefreshInProgress;
    private UnityLobbyService lobbyService;

    private LobbyStateStore SharedStore => LobbyStateStore.Local;

    private void Awake()
    {
        lobbyService = new UnityLobbyService();
        AutoAssignReferences();
        BindControlButtons();
    }

    private void OnEnable()
    {
        AutoAssignReferences();
        BindControlButtons();

        if (refreshOnEnable || SharedStore.Rooms.Count > 0)
        {
            RefreshRoomList();
        }
    }

    private void OnDisable()
    {
        UnbindControlButtons();
    }

    private void OnDestroy()
    {
        UnbindControlButtons();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        AutoAssignReferences();
    }
#endif

    [ContextMenu("Refresh Room List")]
    public void RefreshRoomList()
    {
        if (Application.isPlaying && lastRefreshFrame == Time.frameCount)
        {
            return;
        }

        _ = RefreshRoomListAsync();
    }

    private async Task RefreshRoomListAsync()
    {
        if (isRefreshInProgress)
        {
            return;
        }

        isRefreshInProgress = true;
        try
        {
            AutoAssignReferences();

            if (contentRoot == null)
            {
                if (logRefreshInfo)
                {
                    Debug.LogWarning("RoomBrowserScreenBinder: ContentRoot is not assigned.");
                }

                return;
            }

            ResolveTemplateItem();
            ClearRuntimeItems();

            if (templateItemTransform != null && hideTemplateItemOnRuntime)
            {
                templateItemTransform.gameObject.SetActive(false);
            }

            bool usedUnityLobbyData = false;
            List<RoomState> rowsToRender = null;

            if (Application.isPlaying && useUnityLobbyQuery)
            {
                QueryResponse queryResponse = await QueryUnityLobbiesAsync();
                if (queryResponse != null && queryResponse.Results != null)
                {
                    usedUnityLobbyData = true;
                    rowsToRender = ConvertLobbiesToRoomStates(queryResponse.Results);
                }
                else if (logRefreshInfo)
                {
                    Debug.LogWarning("RoomBrowserScreenBinder: Unity Lobby query failed or returned null. Falling back to local rooms.");
                }
            }

            if (rowsToRender == null)
            {
                rowsToRender = SharedStore.Rooms != null ? SharedStore.Rooms : new List<RoomState>();
            }

            int rowCount = rowsToRender.Count;
            if (rowCount <= 0)
            {
                if (logRefreshInfo)
                {
                    Debug.Log(usedUnityLobbyData
                        ? "RoomBrowserScreenBinder: Unity Lobby query returned no lobbies."
                        : "RoomBrowserScreenBinder: Rooms list is empty.");
                }

                return;
            }

            if (roomListItemPrefab == null)
            {
                if (logRefreshInfo)
                {
                    Debug.LogWarning("RoomBrowserScreenBinder: RoomListItemPrefab is not assigned.");
                }

                return;
            }

            PrepareContentRootLayout(rowCount);

            for (int i = 0; i < rowCount; i++)
            {
                GameObject instance = Instantiate(roomListItemPrefab, contentRoot, false);
                instance.name = RuntimeItemNamePrefix + (i + 1).ToString("00");
                instance.SetActive(true);
                ApplyRowContainerLayout(instance, i);
                BindRoomItem(instance, rowsToRender[i], i, HandleJoinButtonClicked);
            }

            if (Application.isPlaying)
            {
                lastRefreshFrame = Time.frameCount;
            }

            if (logRefreshInfo)
            {
                Debug.Log(usedUnityLobbyData
                    ? $"RoomBrowserScreenBinder: Refreshed {rowCount} lobby row(s) from Unity Lobby query."
                    : $"RoomBrowserScreenBinder: Refreshed {rowCount} room item(s).");
            }
        }
        finally
        {
            isRefreshInProgress = false;
        }
    }

    private async Task<QueryResponse> QueryUnityLobbiesAsync()
    {
        if (lobbyService == null)
        {
            lobbyService = new UnityLobbyService();
        }

        try
        {
            return await lobbyService.QueryLobbiesAsync();
        }
        catch (Exception exception)
        {
            if (logRefreshInfo)
            {
                Debug.LogWarning($"RoomBrowserScreenBinder: Unity Lobby query threw safely. {exception.Message}");
            }

            return null;
        }
    }

    private static List<RoomState> ConvertLobbiesToRoomStates(IReadOnlyList<Lobby> lobbies)
    {
        List<RoomState> result = new List<RoomState>();
        if (lobbies == null)
        {
            return result;
        }

        for (int i = 0; i < lobbies.Count; i++)
        {
            Lobby lobby = lobbies[i];
            if (lobby == null)
            {
                continue;
            }

            int safeMaxPlayers = Mathf.Max(1, lobby.MaxPlayers);
            List<PlayerState> players = ConvertLobbyPlayers(lobby);
            int fallbackPlayerCount = Mathf.Clamp(safeMaxPlayers - lobby.AvailableSlots, 0, safeMaxPlayers);
            if (players.Count == 0 && fallbackPlayerCount > 0)
            {
                for (int playerIndex = 0; playerIndex < fallbackPlayerCount; playerIndex++)
                {
                    players.Add(new PlayerState());
                }
            }

            string roomNameFromData = GetLobbyDataValue(lobby, LobbyDataKeyRoomName);
            string safeLobbyName = !string.IsNullOrWhiteSpace(lobby.Name) ? lobby.Name : roomNameFromData;
            string safeLobbyId = !string.IsNullOrWhiteSpace(lobby.Id) ? lobby.Id : lobby.LobbyCode;
            int treasureCount = ParseLobbyDataInt(lobby, LobbyDataKeyTreasureCount, 0);
            int selectedMapIndex = ParseLobbyDataInt(lobby, LobbyDataKeySelectedMapIndex, 0);

            result.Add(new RoomState
            {
                roomId = string.IsNullOrWhiteSpace(safeLobbyId) ? "000000" : safeLobbyId,
                roomName = string.IsNullOrWhiteSpace(safeLobbyName) ? "Fun Match" : safeLobbyName,
                isPublic = !lobby.IsPrivate,
                maxPlayers = safeMaxPlayers,
                selectedMapIndex = Mathf.Max(0, selectedMapIndex),
                treasureCount = Mathf.Max(0, treasureCount),
                players = players
            });
        }

        return result;
    }

    private static List<PlayerState> ConvertLobbyPlayers(Lobby lobby)
    {
        List<PlayerState> players = new List<PlayerState>();
        if (lobby == null || lobby.Players == null)
        {
            return players;
        }

        for (int i = 0; i < lobby.Players.Count; i++)
        {
            Player lobbyPlayer = lobby.Players[i];
            if (lobbyPlayer == null)
            {
                continue;
            }

            string safePlayerId = string.IsNullOrWhiteSpace(lobbyPlayer.Id) ? string.Empty : lobbyPlayer.Id.Trim();
            string displayNameFromData = GetLobbyPlayerDataValue(lobbyPlayer, "displayName");
            string safeDisplayName = !string.IsNullOrWhiteSpace(displayNameFromData)
                ? displayNameFromData
                : (!string.IsNullOrWhiteSpace(safePlayerId) ? safePlayerId : "Player");

            players.Add(new PlayerState
            {
                playerId = safePlayerId,
                displayName = safeDisplayName,
                isReady = false,
                isHost = !string.IsNullOrWhiteSpace(lobby.HostId) &&
                         string.Equals(lobby.HostId, safePlayerId, StringComparison.Ordinal),
                selectedColorIndex = 0
            });
        }

        return players;
    }

    private static string GetLobbyPlayerDataValue(Player player, string key)
    {
        if (player == null || player.Data == null || string.IsNullOrWhiteSpace(key))
        {
            return string.Empty;
        }

        if (!player.Data.TryGetValue(key, out PlayerDataObject dataObject) || dataObject == null)
        {
            return string.Empty;
        }

        return dataObject.Value ?? string.Empty;
    }

    private static RoomState ConvertLobbyToRoomState(Lobby lobby)
    {
        if (lobby == null)
        {
            return null;
        }

        List<Lobby> singleLobbyList = new List<Lobby>(1) { lobby };
        List<RoomState> mappedRooms = ConvertLobbiesToRoomStates(singleLobbyList);
        return mappedRooms.Count > 0 ? mappedRooms[0] : null;
    }

    private async Task<Lobby> JoinUnityLobbyByIdAsync(string lobbyId)
    {
        if (lobbyService == null)
        {
            lobbyService = new UnityLobbyService();
        }

        try
        {
            return await lobbyService.JoinLobbyByIdAsync(lobbyId);
        }
        catch (Exception exception)
        {
            if (logRefreshInfo)
            {
                Debug.LogWarning($"RoomBrowserScreenBinder: Unity Lobby join threw safely. {exception.Message}");
            }

            return null;
        }
    }

    private void UpsertRoomById(RoomState room)
    {
        if (room == null || SharedStore.Rooms == null)
        {
            return;
        }

        string safeRoomId = room.roomId != null ? room.roomId.Trim() : string.Empty;
        if (string.IsNullOrWhiteSpace(safeRoomId))
        {
            SharedStore.Rooms.Add(room);
            return;
        }

        for (int i = 0; i < SharedStore.Rooms.Count; i++)
        {
            RoomState existingRoom = SharedStore.Rooms[i];
            if (existingRoom == null)
            {
                continue;
            }

            string existingId = existingRoom.roomId != null ? existingRoom.roomId.Trim() : string.Empty;
            if (string.Equals(existingId, safeRoomId, StringComparison.Ordinal))
            {
                SharedStore.Rooms[i] = room;
                return;
            }
        }

        SharedStore.Rooms.Add(room);
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

    private void ClearRuntimeItems()
    {
        for (int i = contentRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = contentRoot.GetChild(i);
            if (templateItemTransform != null && child == templateItemTransform)
            {
                continue;
            }

            if (child.name.StartsWith(RuntimeItemNamePrefix, StringComparison.Ordinal))
            {
                DestroyForCurrentMode(child.gameObject);
                continue;
            }

            // Design-time mock rows should not stay visible once runtime list is rendered.
            DestroyForCurrentMode(child.gameObject);
        }
    }

    private void AutoAssignReferences()
    {
        Transform[] allTransforms = transform.root.GetComponentsInChildren<Transform>(true);

        if (contentRoot == null)
        {
            Transform content = FindByName(allTransforms, "Content");
            if (content != null)
            {
                contentRoot = content as RectTransform;
            }
        }

        if (createRoomButton == null)
        {
            createRoomButton = FindButtonByName(allTransforms, "CreateRoomButton");
        }

        if (refreshButton == null)
        {
            refreshButton = FindButtonByName(allTransforms, "RefreshButton");
        }

        if (filterRoomsButton == null)
        {
            filterRoomsButton = FindButtonByName(allTransforms, "FilterRoomsButton");
        }

        if (roomBrowserScreen == null)
        {
            Transform browser = FindByName(allTransforms, "RoomBrowserScreen");
            if (browser != null)
            {
                roomBrowserScreen = browser.gameObject;
            }
        }

        if (currentRoomScreen == null)
        {
            Transform current = FindByName(allTransforms, "CurrentRoomScreen");
            if (current != null)
            {
                currentRoomScreen = current.gameObject;
            }
        }

        if (createRoomScreen == null)
        {
            Transform create = FindByName(allTransforms, "CreateRoomScreen");
            if (create != null)
            {
                createRoomScreen = create.gameObject;
            }
        }

        if (roomListItemPrefab == null && contentRoot != null && contentRoot.childCount > 0)
        {
            roomListItemPrefab = contentRoot.GetChild(0).gameObject;
        }
    }

    private void ResolveTemplateItem()
    {
        templateItemTransform = null;
        if (roomListItemPrefab == null || contentRoot == null)
        {
            return;
        }

        Transform prefabTransform = roomListItemPrefab.transform;
        if (prefabTransform.parent == contentRoot)
        {
            templateItemTransform = prefabTransform;
        }
    }

    private void BindControlButtons()
    {
        if (refreshButton != null)
        {
            refreshButton.onClick.RemoveListener(RefreshRoomList);
            refreshButton.onClick.AddListener(RefreshRoomList);
        }
    }

    private void UnbindControlButtons()
    {
        if (refreshButton != null)
        {
            refreshButton.onClick.RemoveListener(RefreshRoomList);
        }
    }

    private void HandleJoinButtonClicked(RoomState selectedRoom)
    {
        _ = HandleJoinButtonClickedAsync(selectedRoom);
    }

    private async Task HandleJoinButtonClickedAsync(RoomState selectedRoom)
    {
        if (selectedRoom == null)
        {
            return;
        }

        int safeMaxPlayers = Mathf.Max(1, selectedRoom.maxPlayers);
        int safePlayerCount = selectedRoom.players != null ? selectedRoom.players.Count : 0;
        bool isRoomFull = safePlayerCount >= safeMaxPlayers;
        if (isRoomFull)
        {
            if (logRefreshInfo)
            {
                Debug.LogWarning("RoomBrowserScreenBinder: Join skipped because room is full.");
            }

            return;
        }

        RoomState targetRoom = selectedRoom;
        if (Application.isPlaying && useUnityLobbyQuery)
        {
            string lobbyId = selectedRoom.roomId != null ? selectedRoom.roomId.Trim() : string.Empty;
            if (string.IsNullOrWhiteSpace(lobbyId))
            {
                if (logRefreshInfo)
                {
                    Debug.LogWarning("RoomBrowserScreenBinder: Join skipped because lobbyId is empty.");
                }

                return;
            }

            Lobby joinedLobby = await JoinUnityLobbyByIdAsync(lobbyId);
            if (joinedLobby == null)
            {
                if (logRefreshInfo)
                {
                    Debug.LogWarning($"RoomBrowserScreenBinder: Join failed for lobbyId={lobbyId}.");
                }

                return;
            }

            RoomState mappedRoom = ConvertLobbyToRoomState(joinedLobby);
            if (mappedRoom == null)
            {
                if (logRefreshInfo)
                {
                    Debug.LogWarning($"RoomBrowserScreenBinder: Join succeeded but mapping failed for lobbyId={lobbyId}.");
                }

                return;
            }

            targetRoom = mappedRoom;
            UpsertRoomById(targetRoom);
        }

        bool joinedOrAlreadyMember = SharedStore.TryJoinRoom(targetRoom);
        if (!joinedOrAlreadyMember)
        {
            if (logRefreshInfo)
            {
                Debug.LogWarning("RoomBrowserScreenBinder: Join was rejected safely by local room state.");
            }

            return;
        }

        AutoAssignReferences();

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

    private static void BindRoomItem(
        GameObject itemObject,
        RoomState roomState,
        int rowIndex,
        Action<RoomState> onJoinRequested)
    {
        if (itemObject == null)
        {
            return;
        }

        bool hasRealRoom = roomState != null;
        int playerCount = hasRealRoom && roomState.players != null ? roomState.players.Count : GetMockPlayerCount(rowIndex);
        int maxPlayers = hasRealRoom ? Mathf.Max(1, roomState.maxPlayers) : GetMockMaxPlayers(rowIndex);
        int treasureCount = hasRealRoom ? Mathf.Max(0, roomState.treasureCount) : GetMockTreasureCount(rowIndex);
        int mapIndex = hasRealRoom ? Mathf.Max(0, roomState.selectedMapIndex) : rowIndex;
        ApplyMapThumbnailVisual(itemObject.transform, mapIndex);

        TMP_Text playerCountText = FindTextByName(itemObject.transform, "PlayerCountText");
        if (playerCountText != null)
        {
            ApplyPlayerCountVisual(playerCountText);
            playerCountText.text = $"{playerCount}/{maxPlayers}";
        }

        TMP_Text rewardCountText = FindTextByName(itemObject.transform, "RewardCountText");
        if (rewardCountText != null)
        {
            ApplyRewardAreaVisual(itemObject.transform);
            rewardCountText.text = $"x{treasureCount}";
        }

        TMP_Text roomNameText = FindTextByName(itemObject.transform, "RoomNameText");
        if (roomNameText != null)
        {
            string safeRoomName = hasRealRoom && !string.IsNullOrWhiteSpace(roomState.roomName)
                ? roomState.roomName
                : "Fun Match";
            ApplyRoomNameVisual(roomNameText);
            roomNameText.text = safeRoomName;
        }

        TMP_Text roomIdText = EnsureRoomIdText(itemObject.transform, roomNameText);
        if (roomIdText != null)
        {
            string safeRoomId = hasRealRoom && !string.IsNullOrWhiteSpace(roomState.roomId)
                ? roomState.roomId
                : (456790 + rowIndex).ToString();

            if (hasRealRoom && string.IsNullOrWhiteSpace(roomState.roomId))
            {
                roomState.roomId = safeRoomId;
            }

            roomIdText.text = $"ID: {safeRoomId}";
        }

        Button joinButton = FindButtonByName(itemObject.transform, "JoinButton");
        if (joinButton != null)
        {
            ApplyJoinButtonVisual(joinButton);
            bool isRoomFull = maxPlayers > 0 && playerCount >= maxPlayers;
            bool isJoinable = hasRealRoom && !isRoomFull;
            joinButton.interactable = isJoinable;
            joinButton.onClick.RemoveAllListeners();

            Image joinButtonImage = joinButton.GetComponent<Image>();
            if (joinButtonImage != null)
            {
                joinButtonImage.color = isJoinable
                    ? Color.white
                    : new Color(0.66f, 0.70f, 0.76f, 0.92f);
            }

            if (isJoinable && onJoinRequested != null)
            {
                RoomState capturedRoom = roomState;
                joinButton.onClick.AddListener(() => onJoinRequested(capturedRoom));
            }
        }
    }

    private static int GetMockPlayerCount(int rowIndex)
    {
        int[] values = { 2, 3, 1 };
        return values[Mathf.Abs(rowIndex) % values.Length];
    }

    private static int GetMockMaxPlayers(int rowIndex)
    {
        int[] values = { 4, 6, 4 };
        return values[Mathf.Abs(rowIndex) % values.Length];
    }

    private static int GetMockTreasureCount(int rowIndex)
    {
        int[] values = { 2, 2, 1 };
        return values[Mathf.Abs(rowIndex) % values.Length];
    }

    private static Transform FindByName(Transform[] allTransforms, string targetName)
    {
        for (int i = 0; i < allTransforms.Length; i++)
        {
            if (allTransforms[i].name == targetName)
            {
                return allTransforms[i];
            }
        }

        return null;
    }

    private static Button FindButtonByName(Transform[] allTransforms, string targetName)
    {
        Transform match = FindByName(allTransforms, targetName);
        return match != null ? match.GetComponent<Button>() : null;
    }

    private static Button FindButtonByName(Transform root, string targetName)
    {
        if (root == null)
        {
            return null;
        }

        Transform[] allTransforms = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < allTransforms.Length; i++)
        {
            if (allTransforms[i].name == targetName)
            {
                return allTransforms[i].GetComponent<Button>();
            }
        }

        return null;
    }

    private static TMP_Text FindTextByName(Transform root, string targetName)
    {
        if (root == null)
        {
            return null;
        }

        Transform[] allTransforms = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < allTransforms.Length; i++)
        {
            if (allTransforms[i].name == targetName)
            {
                return allTransforms[i].GetComponent<TMP_Text>();
            }
        }

        return null;
    }

    private static void ApplyRoomNameVisual(TMP_Text roomNameText)
    {
        if (roomNameText == null)
        {
            return;
        }

        RectTransform rect = roomNameText.rectTransform;
        rect.anchorMin = new Vector2(0f, 0.5f);
        rect.anchorMax = new Vector2(0f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = RoomNameAnchoredPosition;
        rect.sizeDelta = RoomNameSize;

        roomNameText.enableWordWrapping = false;
        roomNameText.overflowMode = TextOverflowModes.Ellipsis;
        roomNameText.alignment = TextAlignmentOptions.MidlineLeft;
        roomNameText.fontStyle = FontStyles.Bold;
        roomNameText.fontSize = 32f;
        roomNameText.color = Color.white;
    }

    private static void ApplyPlayerCountVisual(TMP_Text playerCountText)
    {
        if (playerCountText == null)
        {
            return;
        }

        RectTransform rect = playerCountText.rectTransform;
        rect.anchorMin = new Vector2(0f, 0.5f);
        rect.anchorMax = new Vector2(0f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = PlayerCountAnchoredPosition;
        rect.sizeDelta = PlayerCountSize;

        playerCountText.alignment = TextAlignmentOptions.MidlineLeft;
    }

    private static void ApplyRoomIdTextVisual(TMP_Text roomIdText)
    {
        if (roomIdText == null)
        {
            return;
        }

        RectTransform rect = roomIdText.rectTransform;
        rect.anchorMin = new Vector2(0f, 0.5f);
        rect.anchorMax = new Vector2(0f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = RoomIdTextAnchoredPosition;
        rect.sizeDelta = RoomIdTextSize;

        roomIdText.enableWordWrapping = false;
        roomIdText.overflowMode = TextOverflowModes.Ellipsis;
        roomIdText.alignment = TextAlignmentOptions.MidlineLeft;
        roomIdText.fontStyle = FontStyles.Normal;
        roomIdText.fontSize = 20f;
        roomIdText.color = new Color(0.86f, 0.92f, 1f, 1f);
        roomIdText.raycastTarget = false;
    }

    private static void ApplyRewardAreaVisual(Transform itemRoot)
    {
        if (itemRoot == null)
        {
            return;
        }

        Transform rewardAreaTransform = itemRoot.Find("RewardArea");
        if (rewardAreaTransform == null)
        {
            return;
        }

        RectTransform rect = rewardAreaTransform as RectTransform;
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = new Vector2(1f, 0.5f);
        rect.anchorMax = new Vector2(1f, 0.5f);
        rect.pivot = new Vector2(1f, 0.5f);
        rect.anchoredPosition = RewardAreaAnchoredPosition;
        rect.sizeDelta = RewardAreaSize;
    }

    private static TMP_Text EnsureRoomIdText(Transform itemRoot, TMP_Text roomNameReference)
    {
        if (itemRoot == null)
        {
            return null;
        }

        TMP_Text roomIdText = FindTextByName(itemRoot, "RoomIdText");
        if (roomIdText == null)
        {
            Transform roomIdButtonTransform = itemRoot.Find("RoomIdButton");
            if (roomIdButtonTransform != null)
            {
                Transform nestedText = roomIdButtonTransform.Find("RoomIdText");
                if (nestedText != null)
                {
                    nestedText.SetParent(itemRoot, false);
                    roomIdText = nestedText.GetComponent<TMP_Text>();
                }

                DestroyForCurrentMode(roomIdButtonTransform.gameObject);
            }
        }

        if (roomIdText == null)
        {
            GameObject roomIdObject = new GameObject("RoomIdText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            roomIdObject.transform.SetParent(itemRoot, false);
            roomIdText = roomIdObject.GetComponent<TextMeshProUGUI>();
        }

        if (roomNameReference != null && roomNameReference.font != null)
        {
            roomIdText.font = roomNameReference.font;
        }

        ApplyRoomIdTextVisual(roomIdText);
        return roomIdText;
    }

    private static void ApplyMapThumbnailVisual(Transform itemRoot, int selectedMapIndex)
    {
        if (itemRoot == null)
        {
            return;
        }

        Transform mapTransform = itemRoot.Find("MapThumbnail");
        if (mapTransform == null)
        {
            return;
        }

        RectTransform mapRect = mapTransform as RectTransform;
        if (mapRect == null)
        {
            return;
        }

        mapRect.anchorMin = new Vector2(0f, 0.5f);
        mapRect.anchorMax = new Vector2(0f, 0.5f);
        mapRect.pivot = new Vector2(0f, 0.5f);
        mapRect.anchoredPosition = MapAnchoredPosition;
        mapRect.sizeDelta = MapSize;

        Image mapImage = mapTransform.GetComponent<Image>();
        if (mapImage != null && MapThumbnailPalette.Length > 0)
        {
            int safeIndex = Mathf.Abs(selectedMapIndex) % MapThumbnailPalette.Length;
            mapImage.color = MapThumbnailPalette[safeIndex];
        }
    }

    private static void ApplyJoinButtonVisual(Button joinButton)
    {
        if (joinButton == null)
        {
            return;
        }

        RectTransform rect = joinButton.GetComponent<RectTransform>();
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = new Vector2(1f, 0.5f);
        rect.anchorMax = new Vector2(1f, 0.5f);
        rect.pivot = new Vector2(1f, 0.5f);
        rect.anchoredPosition = JoinButtonAnchoredPosition;
        rect.sizeDelta = JoinButtonSize;
    }

    private static void DestroyForCurrentMode(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        if (Application.isEditor)
        {
            DestroyImmediate(target);
            return;
        }

        Destroy(target);
    }

    private void PrepareContentRootLayout(int rowCount)
    {
        if (contentRoot == null)
        {
            return;
        }

        float totalHeight = rowCount * RowHeight + Mathf.Max(0, rowCount - 1) * RowSpacing;

        contentRoot.anchorMin = new Vector2(0f, 1f);
        contentRoot.anchorMax = new Vector2(1f, 1f);
        contentRoot.pivot = new Vector2(0.5f, 1f);
        contentRoot.anchoredPosition = Vector2.zero;
        contentRoot.sizeDelta = new Vector2(0f, totalHeight);
    }

    private static void ApplyRowContainerLayout(GameObject itemObject, int index)
    {
        if (itemObject == null)
        {
            return;
        }

        RectTransform rect = itemObject.GetComponent<RectTransform>();
        if (rect == null)
        {
            return;
        }

        float top = index * (RowHeight + RowSpacing);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.offsetMin = new Vector2(RowHorizontalPadding, -(top + RowHeight));
        rect.offsetMax = new Vector2(-RowHorizontalPadding, -top);
    }
}
