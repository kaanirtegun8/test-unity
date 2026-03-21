using UnityEngine;
using UnityEngine.UI;

public class CurrentRoomMockRoleState : MonoBehaviour
{
    private enum MockRole
    {
        Host,
        Guest
    }

    [SerializeField] private MockRole mockRole = MockRole.Host;
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private bool hideSettingsForGuest = true;

    private void Awake()
    {
        AutoAssignReferences();
        ApplyRoleState();
    }

    private void OnEnable()
    {
        ApplyRoleState();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        AutoAssignReferences();
        if (!Application.isPlaying)
        {
            ApplyRoleState();
        }
    }
#endif

    [ContextMenu("Apply Mock Role State")]
    private void ApplyRoleState()
    {
        bool isHost = mockRole == MockRole.Host;

        if (startGameButton != null)
        {
            startGameButton.interactable = isHost;
        }

        if (settingsButton != null)
        {
            if (hideSettingsForGuest)
            {
                settingsButton.gameObject.SetActive(isHost);
            }
            else
            {
                settingsButton.gameObject.SetActive(true);
                settingsButton.interactable = isHost;
            }
        }
    }

    private void AutoAssignReferences()
    {
        Transform[] allTransforms = transform.root.GetComponentsInChildren<Transform>(true);

        if (startGameButton == null)
        {
            GameObject startGameButtonObject = FindGameObjectByName(allTransforms, "StartGameButton");
            if (startGameButtonObject != null)
            {
                startGameButton = startGameButtonObject.GetComponent<Button>();
            }
        }

        if (settingsButton == null)
        {
            GameObject settingsButtonObject = FindGameObjectByName(allTransforms, "SettingsButton");
            if (settingsButtonObject != null)
            {
                settingsButton = settingsButtonObject.GetComponent<Button>();
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
}
