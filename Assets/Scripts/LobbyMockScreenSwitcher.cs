using UnityEngine;
using UnityEngine.UI;

public class LobbyMockScreenSwitcher : MonoBehaviour
{
    [SerializeField] private GameObject roomBrowserScreen;
    [SerializeField] private GameObject createRoomScreen;
    [SerializeField] private Button createRoomButton;
    [SerializeField] private Button backButton;

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

        if (createRoomButton == null)
        {
            GameObject createButtonObject = FindGameObjectByName(allTransforms, "CreateRoomButton");
            if (createButtonObject != null)
            {
                createRoomButton = createButtonObject.GetComponent<Button>();
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
    }
}
