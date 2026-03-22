using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoomBrowserScreenBinder : MonoBehaviour
{
    private const string RuntimeItemNamePrefix = "RuntimeRoomItem_";

    [SerializeField] private RectTransform contentRoot;
    [SerializeField] private GameObject roomListItemPrefab;
    [SerializeField] private Button createRoomButton;
    [SerializeField] private Button refreshButton;
    [SerializeField] private Button filterRoomsButton;
    [SerializeField] private bool refreshOnEnable;
    [SerializeField] private bool hideTemplateItemOnRuntime = true;
    [SerializeField] private bool logRefreshInfo = true;

    private Transform templateItemTransform;

    private LobbyStateStore SharedStore => LobbyStateStore.Local;

    private void Awake()
    {
        AutoAssignReferences();
    }

    private void OnEnable()
    {
        if (refreshOnEnable)
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

        if (SharedStore.Rooms == null || SharedStore.Rooms.Count == 0)
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

        for (int i = 0; i < SharedStore.Rooms.Count; i++)
        {
            GameObject instance = Instantiate(roomListItemPrefab, contentRoot, false);
            instance.name = RuntimeItemNamePrefix + (i + 1).ToString("00");
            instance.SetActive(true);
            BindRoomItem(instance, SharedStore.Rooms[i], i);
        }

        if (logRefreshInfo)
        {
            Debug.Log($"RoomBrowserScreenBinder: Refreshed {SharedStore.Rooms.Count} room item(s).");
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
                Destroy(child.gameObject);
                continue;
            }

            // Design-time mock rows should not stay visible once runtime list is rendered.
            Destroy(child.gameObject);
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

        int playerCount = roomState != null && roomState.players != null ? roomState.players.Count : 0;
        int maxPlayers = roomState != null ? Mathf.Max(0, roomState.maxPlayers) : 0;
        int treasureCount = roomState != null ? Mathf.Max(0, roomState.treasureCount) : 0;

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
            string safeRoomName = roomState != null && !string.IsNullOrWhiteSpace(roomState.roomName)
                ? roomState.roomName
                : $"Room {rowIndex + 1}";
            roomNameText.enableWordWrapping = false;
            roomNameText.overflowMode = TextOverflowModes.Ellipsis;
            roomNameText.text = safeRoomName;
        }

        TMP_Text roomIdText = FindTextByName(itemObject.transform, "RoomIdText");
        if (roomIdText != null)
        {
            string safeRoomId = roomState != null && !string.IsNullOrWhiteSpace(roomState.roomId)
                ? roomState.roomId
                : "000000";
            roomIdText.text = $"ID: {safeRoomId}";
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
}
