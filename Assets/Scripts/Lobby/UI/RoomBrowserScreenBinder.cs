using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class RoomBrowserScreenBinder : MonoBehaviour
{
    private const string RuntimeItemNamePrefix = "RuntimeRoomItem_";
    private const int MockRowCount = 3;
    private const float RowHeight = 170f;
    private const float RowSpacing = 14f;
    private const float RowHorizontalPadding = -8f;

    private static readonly Vector2 MapAnchoredPosition = new Vector2(230f, 0f);
    private static readonly Vector2 MapSize = new Vector2(230f, 132f);
    private static readonly Vector2 RoomNameAnchoredPosition = new Vector2(500f, 0f);
    private static readonly Vector2 RoomNameSize = new Vector2(510f, 56f);
    private static readonly Vector2 RoomIdButtonAnchoredPosition = new Vector2(28f, 0f);
    private static readonly Vector2 RoomIdButtonSize = new Vector2(244f, 74f);
    private static readonly Vector2 RoomIdTextAnchoredPosition = new Vector2(28f, 0f);
    private static readonly Vector2 RoomIdTextSize = new Vector2(186f, 44f);
    private static readonly Vector2 RoomIdIconAnchoredPosition = new Vector2(30f, 0f);
    private static readonly Vector2 RoomIdIconSize = new Vector2(26f, 26f);

#if UNITY_EDITOR
    private const string RoomIdButtonSpritePath = "Assets/Simple Buttons/Big Buttons/BigBlue.png";
    private const string RoomIdIconSpritePath = "Assets/Simple Buttons/Icons/Icon_16.gif";
    private static Sprite cachedRoomIdButtonSprite;
    private static Sprite cachedRoomIdIconSprite;
#endif

    [SerializeField] private RectTransform contentRoot;
    [SerializeField] private GameObject roomListItemPrefab;
    [SerializeField] private Button createRoomButton;
    [SerializeField] private Button refreshButton;
    [SerializeField] private Button filterRoomsButton;
    [SerializeField] private bool refreshOnEnable;
    [SerializeField] private bool hideTemplateItemOnRuntime = true;
    [SerializeField] private bool logRefreshInfo = true;

    private Transform templateItemTransform;
    private int lastRefreshFrame = -1;

    private LobbyStateStore SharedStore => LobbyStateStore.Local;

    private void Awake()
    {
        AutoAssignReferences();
    }

    private void OnEnable()
    {
        if (refreshOnEnable || SharedStore.Rooms.Count > 0)
        {
            RefreshRoomList();
        }
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

        bool hasRealRooms = SharedStore.Rooms != null && SharedStore.Rooms.Count > 0;
        int rowCount = hasRealRooms ? SharedStore.Rooms.Count : MockRowCount;
        if (rowCount <= 0)
        {
            if (logRefreshInfo)
            {
                Debug.Log("RoomBrowserScreenBinder: Rooms list is empty.");
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

        if (templateItemTransform != null && hideTemplateItemOnRuntime)
        {
            templateItemTransform.gameObject.SetActive(false);
        }

        PrepareContentRootLayout(rowCount);

        for (int i = 0; i < rowCount; i++)
        {
            GameObject instance = Instantiate(roomListItemPrefab, contentRoot, false);
            instance.name = RuntimeItemNamePrefix + (i + 1).ToString("00");
            instance.SetActive(true);
            ApplyRowContainerLayout(instance, i);
            BindRoomItem(instance, hasRealRooms ? SharedStore.Rooms[i] : null, i);
        }

        if (Application.isPlaying)
        {
            lastRefreshFrame = Time.frameCount;
        }

        if (logRefreshInfo)
        {
            Debug.Log($"RoomBrowserScreenBinder: Refreshed {rowCount} room item(s).");
        }
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
        Transform[] allTransforms = transform.GetComponentsInChildren<Transform>(true);

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

    private static void BindRoomItem(GameObject itemObject, RoomState roomState, int rowIndex)
    {
        if (itemObject == null)
        {
            return;
        }

        bool hasRealRoom = roomState != null;
        int playerCount = hasRealRoom && roomState.players != null ? roomState.players.Count : GetMockPlayerCount(rowIndex);
        int maxPlayers = hasRealRoom ? Mathf.Max(0, roomState.maxPlayers) : GetMockMaxPlayers(rowIndex);
        int treasureCount = hasRealRoom ? Mathf.Max(0, roomState.treasureCount) : GetMockTreasureCount(rowIndex);
        ApplyMapThumbnailVisual(itemObject.transform);

        TMP_Text playerCountText = FindTextByName(itemObject.transform, "PlayerCountText");
        if (playerCountText != null)
        {
            playerCountText.text = $"{playerCount}/{maxPlayers}";
        }

        TMP_Text rewardCountText = FindTextByName(itemObject.transform, "RewardCountText");
        if (rewardCountText != null)
        {
            rewardCountText.text = $"x{treasureCount}";
        }

        TMP_Text roomNameText = FindTextByName(itemObject.transform, "RoomNameText");
        if (roomNameText != null)
        {
            string safeRoomName = hasRealRoom && !string.IsNullOrWhiteSpace(roomState.roomName)
                ? roomState.roomName
                : "Funny Match";
            ApplyRoomNameVisual(roomNameText);
            roomNameText.text = safeRoomName;
        }

        Button roomIdButton = EnsureRoomIdButton(itemObject.transform, roomNameText, out TMP_Text roomIdText);
        if (roomIdButton != null && roomIdText != null)
        {
            string safeRoomId = hasRealRoom && !string.IsNullOrWhiteSpace(roomState.roomId)
                ? roomState.roomId
                : (456790 + rowIndex).ToString();

            if (hasRealRoom && string.IsNullOrWhiteSpace(roomState.roomId))
            {
                roomState.roomId = safeRoomId;
            }

            roomIdText.text = $"ID: {safeRoomId}";

            roomIdButton.onClick.RemoveAllListeners();
            string roomIdToCopy = safeRoomId;
            roomIdButton.onClick.AddListener(() => CopyRoomIdToClipboard(roomIdToCopy));
        }

        Button joinButton = FindButtonByName(itemObject.transform, "JoinButton");
        if (joinButton != null)
        {
            bool isRoomFull = maxPlayers > 0 && playerCount >= maxPlayers;
            joinButton.interactable = !isRoomFull;

            Image joinButtonImage = joinButton.GetComponent<Image>();
            if (joinButtonImage != null)
            {
                joinButtonImage.color = isRoomFull
                    ? new Color(0.66f, 0.70f, 0.76f, 0.92f)
                    : Color.white;
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

    private static void ApplyRoomIdTextVisual(TMP_Text roomIdText)
    {
        if (roomIdText == null)
        {
            return;
        }

        RectTransform rect = roomIdText.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = RoomIdTextAnchoredPosition;
        rect.sizeDelta = RoomIdTextSize;

        roomIdText.enableWordWrapping = false;
        roomIdText.overflowMode = TextOverflowModes.Ellipsis;
        roomIdText.alignment = TextAlignmentOptions.MidlineLeft;
        roomIdText.fontStyle = FontStyles.Bold;
        roomIdText.fontSize = 24f;
        roomIdText.color = new Color(0.95f, 0.98f, 1f, 1f);
        roomIdText.raycastTarget = false;
    }

    private static Button EnsureRoomIdButton(Transform itemRoot, TMP_Text roomNameReference, out TMP_Text roomIdText)
    {
        roomIdText = null;
        if (itemRoot == null)
        {
            return null;
        }

        Transform buttonTransform = itemRoot.Find("RoomIdButton");
        Button roomIdButton = buttonTransform != null ? buttonTransform.GetComponent<Button>() : null;

        if (roomIdButton == null)
        {
            GameObject buttonObject = new GameObject("RoomIdButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(itemRoot, false);
            roomIdButton = buttonObject.GetComponent<Button>();
        }

        RectTransform buttonRect = roomIdButton.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0f, 0.5f);
        buttonRect.anchorMax = new Vector2(0f, 0.5f);
        buttonRect.pivot = new Vector2(0f, 0.5f);
        buttonRect.anchoredPosition = RoomIdButtonAnchoredPosition;
        buttonRect.sizeDelta = RoomIdButtonSize;

        Image buttonImage = roomIdButton.GetComponent<Image>();
        if (buttonImage != null)
        {
            Sprite roomIdButtonSprite = TryGetRoomIdButtonSprite();
            if (roomIdButtonSprite != null)
            {
                buttonImage.sprite = roomIdButtonSprite;
                buttonImage.type = Image.Type.Simple;
                buttonImage.preserveAspect = false;
                buttonImage.color = Color.white;
            }
            else
            {
                buttonImage.color = new Color(0.16f, 0.35f, 0.62f, 0.96f);
            }

            buttonImage.raycastTarget = true;
        }

        ColorBlock colors = roomIdButton.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.88f, 0.96f, 1f, 1f);
        colors.pressedColor = new Color(0.76f, 0.88f, 1f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.65f, 0.70f, 0.80f, 0.8f);
        roomIdButton.colors = colors;
        roomIdButton.transition = Selectable.Transition.ColorTint;
        roomIdButton.interactable = true;

        EnsureRoomIdIcon(roomIdButton.transform);

        roomIdText = FindTextByName(roomIdButton.transform, "RoomIdText");
        if (roomIdText == null)
        {
            Transform existingText = itemRoot.Find("RoomIdText");
            if (existingText != null)
            {
                existingText.SetParent(roomIdButton.transform, false);
                roomIdText = existingText.GetComponent<TMP_Text>();
            }
        }

        if (roomIdText == null)
        {
            GameObject roomIdObject = new GameObject("RoomIdText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            roomIdObject.transform.SetParent(roomIdButton.transform, false);
            roomIdText = roomIdObject.GetComponent<TextMeshProUGUI>();
        }

        if (roomNameReference != null && roomNameReference.font != null)
        {
            roomIdText.font = roomNameReference.font;
        }

        ApplyRoomIdTextVisual(roomIdText);
        return roomIdButton;
    }

    private static void EnsureRoomIdIcon(Transform roomIdButtonTransform)
    {
        if (roomIdButtonTransform == null)
        {
            return;
        }

        Transform iconTransform = roomIdButtonTransform.Find("RoomIdIcon");
        Image iconImage;
        if (iconTransform == null)
        {
            GameObject iconObject = new GameObject("RoomIdIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconObject.transform.SetParent(roomIdButtonTransform, false);
            iconTransform = iconObject.transform;
        }

        iconImage = iconTransform.GetComponent<Image>();
        if (iconImage == null)
        {
            iconImage = iconTransform.gameObject.AddComponent<Image>();
        }

        RectTransform iconRect = iconTransform as RectTransform;
        iconRect.anchorMin = new Vector2(0f, 0.5f);
        iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = RoomIdIconAnchoredPosition;
        iconRect.sizeDelta = RoomIdIconSize;

        Sprite iconSprite = TryGetRoomIdIconSprite();
        if (iconSprite != null)
        {
            iconImage.sprite = iconSprite;
            iconImage.color = Color.white;
            iconImage.type = Image.Type.Simple;
            iconImage.preserveAspect = true;
        }
        else
        {
            iconImage.sprite = null;
            iconImage.color = new Color(0.90f, 0.95f, 1f, 0.95f);
        }

        iconImage.raycastTarget = false;
    }

    private static Sprite TryGetRoomIdButtonSprite()
    {
#if UNITY_EDITOR
        if (cachedRoomIdButtonSprite == null)
        {
            cachedRoomIdButtonSprite = AssetDatabase.LoadAssetAtPath<Sprite>(RoomIdButtonSpritePath);
        }

        return cachedRoomIdButtonSprite;
#else
        return null;
#endif
    }

    private static Sprite TryGetRoomIdIconSprite()
    {
#if UNITY_EDITOR
        if (cachedRoomIdIconSprite == null)
        {
            cachedRoomIdIconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(RoomIdIconSpritePath);
        }

        return cachedRoomIdIconSprite;
#else
        return null;
#endif
    }

    private static void ApplyMapThumbnailVisual(Transform itemRoot)
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

    private static void CopyRoomIdToClipboard(string roomId)
    {
        if (string.IsNullOrWhiteSpace(roomId))
        {
            return;
        }

        GUIUtility.systemCopyBuffer = roomId;
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
