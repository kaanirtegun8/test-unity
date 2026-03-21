using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CurrentRoomMockCapacityState : MonoBehaviour
{
    private enum MockRoomCapacity
    {
        TwoPlayers = 2,
        ThreePlayers = 3,
        FourPlayers = 4
    }

    [SerializeField] private MockRoomCapacity mockRoomCapacity = MockRoomCapacity.FourPlayers;
    [SerializeField] private GameObject[] playerSlots = new GameObject[4];
    [SerializeField] private string unavailableText = "Locked";
    [SerializeField] [Range(0f, 1f)] private float unavailableTint = 0.45f;

    private SlotVisualState[] cachedStates;

    private sealed class SlotVisualState
    {
        public GameObject Slot;
        public Image SlotBackgroundImage;
        public Image AvatarImage;
        public Button StatusButton;
        public Image StatusButtonImage;
        public TMP_Text StatusText;

        public Color SlotBackgroundColor;
        public Color AvatarColor;
        public Color StatusButtonColor;
        public Color StatusTextColor;
        public bool StatusButtonInteractable;
        public string StatusTextValue;
    }

    private void Awake()
    {
        AutoAssignSlots();
        CacheStates();
        ApplyCapacityState();
    }

    private void OnEnable()
    {
        ApplyCapacityState();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        AutoAssignSlots();
        cachedStates = null;
    }
#endif

    [ContextMenu("Apply Mock Capacity State")]
    private void ApplyCapacityState()
    {
        if (cachedStates == null || cachedStates.Length == 0)
        {
            CacheStates();
            if (cachedStates == null || cachedStates.Length == 0)
            {
                return;
            }
        }

        int availableSlotCount = (int)mockRoomCapacity;
        for (int i = 0; i < cachedStates.Length; i++)
        {
            if (cachedStates[i] == null || cachedStates[i].Slot == null)
            {
                continue;
            }

            bool isAvailable = i < availableSlotCount;
            if (isAvailable)
            {
                ApplyAvailableState(cachedStates[i]);
            }
            else
            {
                ApplyUnavailableState(cachedStates[i]);
            }
        }
    }

    private void ApplyAvailableState(SlotVisualState state)
    {
        if (state.SlotBackgroundImage != null)
        {
            state.SlotBackgroundImage.color = state.SlotBackgroundColor;
        }

        if (state.AvatarImage != null)
        {
            state.AvatarImage.color = state.AvatarColor;
        }

        if (state.StatusButtonImage != null)
        {
            state.StatusButtonImage.color = state.StatusButtonColor;
        }

        if (state.StatusButton != null)
        {
            state.StatusButton.interactable = state.StatusButtonInteractable;
        }

        if (state.StatusText != null)
        {
            state.StatusText.text = state.StatusTextValue;
            state.StatusText.color = state.StatusTextColor;
        }
    }

    private void ApplyUnavailableState(SlotVisualState state)
    {
        if (state.SlotBackgroundImage != null)
        {
            state.SlotBackgroundImage.color = DimColor(state.SlotBackgroundColor);
        }

        if (state.AvatarImage != null)
        {
            state.AvatarImage.color = DimColor(state.AvatarColor);
        }

        if (state.StatusButtonImage != null)
        {
            state.StatusButtonImage.color = DimColor(state.StatusButtonColor);
        }

        if (state.StatusButton != null)
        {
            state.StatusButton.interactable = false;
        }

        if (state.StatusText != null)
        {
            state.StatusText.text = unavailableText;
            state.StatusText.color = DimColor(state.StatusTextColor);
        }
    }

    private Color DimColor(Color original)
    {
        float tint = Mathf.Clamp01(unavailableTint);
        return new Color(
            Mathf.Lerp(original.r, 0.5f, 1f - tint),
            Mathf.Lerp(original.g, 0.5f, 1f - tint),
            Mathf.Lerp(original.b, 0.5f, 1f - tint),
            Mathf.Lerp(original.a, original.a * 0.6f, 1f - tint));
    }

    private void CacheStates()
    {
        if (playerSlots == null || playerSlots.Length == 0)
        {
            return;
        }

        if (cachedStates != null && cachedStates.Length == playerSlots.Length)
        {
            bool allValid = true;
            for (int i = 0; i < cachedStates.Length; i++)
            {
                if (cachedStates[i] == null || cachedStates[i].Slot != playerSlots[i])
                {
                    allValid = false;
                    break;
                }
            }

            if (allValid)
            {
                return;
            }
        }

        cachedStates = new SlotVisualState[playerSlots.Length];
        for (int i = 0; i < playerSlots.Length; i++)
        {
            GameObject slot = playerSlots[i];
            if (slot == null)
            {
                continue;
            }

            SlotVisualState state = new SlotVisualState();
            state.Slot = slot;
            state.SlotBackgroundImage = slot.GetComponent<Image>();

            Transform avatarTransform = slot.transform.Find("AvatarPlaceholder");
            if (avatarTransform != null)
            {
                state.AvatarImage = avatarTransform.GetComponent<Image>();
            }

            Transform statusTransform = slot.transform.Find("StatusButton");
            if (statusTransform != null)
            {
                state.StatusButton = statusTransform.GetComponent<Button>();
                state.StatusButtonImage = statusTransform.GetComponent<Image>();

                Transform labelTransform = statusTransform.Find("Label");
                if (labelTransform != null)
                {
                    state.StatusText = labelTransform.GetComponent<TMP_Text>();
                }
            }

            if (state.SlotBackgroundImage != null)
            {
                state.SlotBackgroundColor = state.SlotBackgroundImage.color;
            }

            if (state.AvatarImage != null)
            {
                state.AvatarColor = state.AvatarImage.color;
            }

            if (state.StatusButtonImage != null)
            {
                state.StatusButtonColor = state.StatusButtonImage.color;
            }

            if (state.StatusButton != null)
            {
                state.StatusButtonInteractable = state.StatusButton.interactable;
            }

            if (state.StatusText != null)
            {
                state.StatusTextColor = state.StatusText.color;
                state.StatusTextValue = state.StatusText.text;
            }

            cachedStates[i] = state;
        }
    }

    private void AutoAssignSlots()
    {
        if (playerSlots == null || playerSlots.Length != 4)
        {
            playerSlots = new GameObject[4];
        }

        Transform[] allTransforms = transform.root.GetComponentsInChildren<Transform>(true);
        AssignSlotByName(allTransforms, "PlayerSlot01", 0);
        AssignSlotByName(allTransforms, "PlayerSlot02", 1);
        AssignSlotByName(allTransforms, "PlayerSlot03", 2);
        AssignSlotByName(allTransforms, "PlayerSlot04", 3);
    }

    private void AssignSlotByName(Transform[] allTransforms, string slotName, int index)
    {
        if (playerSlots[index] != null)
        {
            return;
        }

        for (int i = 0; i < allTransforms.Length; i++)
        {
            if (allTransforms[i].name == slotName)
            {
                playerSlots[index] = allTransforms[i].gameObject;
                return;
            }
        }
    }
}
