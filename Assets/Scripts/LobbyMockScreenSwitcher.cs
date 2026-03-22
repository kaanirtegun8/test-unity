using UnityEngine;
using UnityEngine.UI;

public class LobbyMockScreenSwitcher : MonoBehaviour
{
    [SerializeField] private GameObject roomBrowserScreen;
    [SerializeField] private GameObject createRoomScreen;
    [SerializeField] private GameObject currentRoomScreen;
    [SerializeField] private Button createRoomButton;
    [SerializeField] private Button createButton;
    [SerializeField] private Button backButton;
    [SerializeField] private Button leaveButton;

    private void Awake()
    {
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
            leaveButton.onClick.RemoveListener(ShowRoomBrowser);
            leaveButton.onClick.AddListener(ShowRoomBrowser);
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
            leaveButton.onClick.RemoveListener(ShowRoomBrowser);
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

    public void ShowCurrentRoom()
    {
        LobbyStateStore.Local.CreateRoomFromDraft();

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
}
