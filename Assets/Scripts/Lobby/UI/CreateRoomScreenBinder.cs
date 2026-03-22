using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class CreateRoomScreenBinder : MonoBehaviour
{
    private const int MinPlayers = 2;
    private const int MaxPlayers = 4;
    private const int MapItemCount = 10;

    [SerializeField] private TMP_InputField roomNameInput;
    [SerializeField] private Button player2Button;
    [SerializeField] private Button player3Button;
    [SerializeField] private Button player4Button;
    [SerializeField] private Image publicToggleVisual;
    [SerializeField] private Button visibilityToggleButton;
    [SerializeField] private RectTransform toggleKnob;
    [SerializeField] private TMP_Text publicText;
    [SerializeField] private TMP_Text privateText;
    [SerializeField] private Button minusButton;
    [SerializeField] private Button plusButton;
    [SerializeField] private TMP_Text countText;
    [SerializeField] private string roomNamePlaceholderText = "Enter room name";
    [SerializeField] private int minTreasureCount = 1;
    [SerializeField] private int maxTreasureCount = 9;
    [SerializeField] [Range(0f, 1f)] private float selectedMapAlpha = 1f;
    [SerializeField] [Range(0f, 1f)] private float unselectedMapAlpha = 0.56f;
    [SerializeField] private Color switchOnColor = Color.white;
    [SerializeField] private Color switchOffColor = new Color(0.7f, 0.75f, 0.85f, 1f);
    [SerializeField] private Sprite selectedMapSprite;
    [SerializeField] private Sprite unselectedMapSprite;

    private readonly Button[] mapItemButtons = new Button[MapItemCount];
    private readonly Image[] mapItemImages = new Image[MapItemCount];
    private readonly UnityAction[] mapItemClickHandlers = new UnityAction[MapItemCount];

    private Sprite selectedButtonSprite;
    private Sprite unselectedButtonSprite;
    private Sprite toggleBaseSprite;
    private Vector2 publicKnobPosition;
    private Vector2 privateKnobPosition;
    private bool initialized;

    private LobbyStateStore SharedStore => LobbyStateStore.Local;

    private void Awake()
    {
        InitializeIfNeeded();
        BindUiEvents();
        ApplyDraftToUi();
    }

    private void OnEnable()
    {
        InitializeIfNeeded();
        BindUiEvents();
        ApplyDraftToUi();
    }

    private void OnDestroy()
    {
        UnbindUiEvents();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        AutoAssignReferences(false);
    }
#endif

    private void InitializeIfNeeded()
    {
        if (initialized)
        {
            return;
        }

        AutoAssignReferences(true);
        EnsureRoomNameInputSetup();
        CacheVisualPresets();
        initialized = true;
    }

    private void BindUiEvents()
    {
        if (roomNameInput != null)
        {
            roomNameInput.onValueChanged.RemoveListener(OnRoomNameValueChanged);
            roomNameInput.onValueChanged.AddListener(OnRoomNameValueChanged);
        }

        if (player2Button != null)
        {
            player2Button.onClick.RemoveListener(OnPlayer2ButtonClicked);
            player2Button.onClick.AddListener(OnPlayer2ButtonClicked);
            player2Button.interactable = true;
        }

        if (player3Button != null)
        {
            player3Button.onClick.RemoveListener(OnPlayer3ButtonClicked);
            player3Button.onClick.AddListener(OnPlayer3ButtonClicked);
            player3Button.interactable = true;
        }

        if (player4Button != null)
        {
            player4Button.onClick.RemoveListener(OnPlayer4ButtonClicked);
            player4Button.onClick.AddListener(OnPlayer4ButtonClicked);
            player4Button.interactable = true;
        }

        if (visibilityToggleButton != null)
        {
            visibilityToggleButton.onClick.RemoveListener(OnVisibilityToggleClicked);
            visibilityToggleButton.onClick.AddListener(OnVisibilityToggleClicked);
            visibilityToggleButton.interactable = true;
        }

        if (minusButton != null)
        {
            minusButton.onClick.RemoveListener(OnMinusButtonClicked);
            minusButton.onClick.AddListener(OnMinusButtonClicked);
            minusButton.interactable = true;
        }

        if (plusButton != null)
        {
            plusButton.onClick.RemoveListener(OnPlusButtonClicked);
            plusButton.onClick.AddListener(OnPlusButtonClicked);
            plusButton.interactable = true;
        }

        BindMapItemEvents();
    }

    private void BindMapItemEvents()
    {
        for (int i = 0; i < MapItemCount; i++)
        {
            Button mapButton = mapItemButtons[i];
            if (mapButton == null)
            {
                continue;
            }

            if (mapItemClickHandlers[i] == null)
            {
                int capturedIndex = i;
                mapItemClickHandlers[i] = () => OnMapItemClicked(capturedIndex);
            }

            mapButton.onClick.RemoveListener(mapItemClickHandlers[i]);
            mapButton.onClick.AddListener(mapItemClickHandlers[i]);
            mapButton.interactable = true;
        }
    }

    private void UnbindUiEvents()
    {
        if (roomNameInput != null)
        {
            roomNameInput.onValueChanged.RemoveListener(OnRoomNameValueChanged);
        }

        if (player2Button != null)
        {
            player2Button.onClick.RemoveListener(OnPlayer2ButtonClicked);
        }

        if (player3Button != null)
        {
            player3Button.onClick.RemoveListener(OnPlayer3ButtonClicked);
        }

        if (player4Button != null)
        {
            player4Button.onClick.RemoveListener(OnPlayer4ButtonClicked);
        }

        if (visibilityToggleButton != null)
        {
            visibilityToggleButton.onClick.RemoveListener(OnVisibilityToggleClicked);
        }

        if (minusButton != null)
        {
            minusButton.onClick.RemoveListener(OnMinusButtonClicked);
        }

        if (plusButton != null)
        {
            plusButton.onClick.RemoveListener(OnPlusButtonClicked);
        }

        for (int i = 0; i < MapItemCount; i++)
        {
            if (mapItemButtons[i] == null || mapItemClickHandlers[i] == null)
            {
                continue;
            }

            mapItemButtons[i].onClick.RemoveListener(mapItemClickHandlers[i]);
        }
    }

    private void ApplyDraftToUi()
    {
        RefreshUiFromDraft(EnsureDraft(), true);
    }

    private RoomDraft EnsureDraft()
    {
        if (SharedStore.CurrentDraft == null)
        {
            SharedStore.ResetDraft();
        }

        return SharedStore.CurrentDraft;
    }

    private void RefreshUiFromDraft(RoomDraft draft, bool updateRoomNameField)
    {
        if (draft == null)
        {
            return;
        }

        draft.maxPlayers = Mathf.Clamp(draft.maxPlayers, MinPlayers, MaxPlayers);
        int safeMinTreasure = Mathf.Min(minTreasureCount, maxTreasureCount);
        int safeMaxTreasure = Mathf.Max(minTreasureCount, maxTreasureCount);
        draft.treasureCount = Mathf.Clamp(draft.treasureCount, safeMinTreasure, safeMaxTreasure);

        int availableMapItems = GetAvailableMapItemCount();
        if (availableMapItems > 0)
        {
            draft.selectedMapIndex = Mathf.Clamp(draft.selectedMapIndex, 0, availableMapItems - 1);
        }
        else
        {
            draft.selectedMapIndex = 0;
        }

        if (updateRoomNameField && roomNameInput != null)
        {
            roomNameInput.SetTextWithoutNotify(draft.roomName ?? string.Empty);
        }

        ApplyPlayerSelectionVisual(draft.maxPlayers);
        ApplyVisibilityVisual(draft.isPublic);
        ApplyTreasureCountVisual(draft.treasureCount);
        ApplyMapSelectionVisuals(draft.selectedMapIndex);
    }

    private void OnRoomNameValueChanged(string value)
    {
        RoomDraft draft = EnsureDraft();
        draft.roomName = value ?? string.Empty;
    }

    private void OnPlayer2ButtonClicked()
    {
        SetMaxPlayers(2);
    }

    private void OnPlayer3ButtonClicked()
    {
        SetMaxPlayers(3);
    }

    private void OnPlayer4ButtonClicked()
    {
        SetMaxPlayers(4);
    }

    private void SetMaxPlayers(int maxPlayers)
    {
        RoomDraft draft = EnsureDraft();
        draft.maxPlayers = Mathf.Clamp(maxPlayers, MinPlayers, MaxPlayers);
        RefreshUiFromDraft(draft, false);
    }

    private void OnVisibilityToggleClicked()
    {
        RoomDraft draft = EnsureDraft();
        draft.isPublic = !draft.isPublic;
        RefreshUiFromDraft(draft, false);
    }

    private void OnMinusButtonClicked()
    {
        ChangeTreasureCount(-1);
    }

    private void OnPlusButtonClicked()
    {
        ChangeTreasureCount(1);
    }

    private void ChangeTreasureCount(int delta)
    {
        RoomDraft draft = EnsureDraft();
        int safeMinTreasure = Mathf.Min(minTreasureCount, maxTreasureCount);
        int safeMaxTreasure = Mathf.Max(minTreasureCount, maxTreasureCount);
        draft.treasureCount = Mathf.Clamp(draft.treasureCount + delta, safeMinTreasure, safeMaxTreasure);
        RefreshUiFromDraft(draft, false);
    }

    private void OnMapItemClicked(int index)
    {
        RoomDraft draft = EnsureDraft();
        int availableMapItems = GetAvailableMapItemCount();
        if (availableMapItems <= 0)
        {
            return;
        }

        draft.selectedMapIndex = Mathf.Clamp(index, 0, availableMapItems - 1);
        RefreshUiFromDraft(draft, false);
    }

    private void ApplyTreasureCountVisual(int treasureCount)
    {
        if (countText != null)
        {
            countText.text = treasureCount.ToString();
        }
    }

    private void ApplyPlayerSelectionVisual(int maxPlayers)
    {
        int clamped = Mathf.Clamp(maxPlayers, MinPlayers, MaxPlayers);
        SetPlayerButtonState(player2Button, clamped == 2);
        SetPlayerButtonState(player3Button, clamped == 3);
        SetPlayerButtonState(player4Button, clamped == 4);
    }

    private void SetPlayerButtonState(Button button, bool isSelected)
    {
        if (button == null)
        {
            return;
        }

        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            if (isSelected && selectedButtonSprite != null)
            {
                image.sprite = selectedButtonSprite;
            }
            else if (!isSelected && unselectedButtonSprite != null)
            {
                image.sprite = unselectedButtonSprite;
            }

            image.color = Color.white;
        }

        button.interactable = true;
    }

    private void ApplyVisibilityVisual(bool isPublic)
    {
        if (publicToggleVisual != null)
        {
            if (toggleBaseSprite != null)
            {
                publicToggleVisual.sprite = toggleBaseSprite;
            }

            publicToggleVisual.color = isPublic ? switchOnColor : switchOffColor;
        }

        if (toggleKnob != null)
        {
            toggleKnob.anchoredPosition = isPublic ? publicKnobPosition : privateKnobPosition;
        }

        if (publicText != null)
        {
            publicText.color = isPublic ? Color.white : new Color(1f, 1f, 1f, 0.65f);
        }

        if (privateText != null)
        {
            privateText.color = isPublic ? new Color(1f, 1f, 1f, 0.65f) : Color.white;
        }
    }

    private void ApplyMapSelectionVisuals(int selectedIndex)
    {
        float selectedAlpha = Mathf.Clamp01(selectedMapAlpha);
        float unselectedAlpha = Mathf.Clamp01(unselectedMapAlpha);

        for (int i = 0; i < MapItemCount; i++)
        {
            Image mapImage = mapItemImages[i];
            if (mapImage == null)
            {
                continue;
            }

            bool isSelected = i == selectedIndex;
            Sprite targetSprite = isSelected ? selectedMapSprite : unselectedMapSprite;
            if (targetSprite != null)
            {
                mapImage.sprite = targetSprite;
                mapImage.overrideSprite = targetSprite;
            }

            // Remove per-item material/style differences so only selectedMapIndex drives visuals.
            mapImage.material = null;

            Color color = mapImage.color;
            color.a = isSelected ? selectedAlpha : unselectedAlpha;
            mapImage.color = color;
            mapImage.raycastTarget = true;

            if (mapItemButtons[i] != null)
            {
                mapItemButtons[i].interactable = true;
            }
        }
    }

    private int GetAvailableMapItemCount()
    {
        int count = 0;
        for (int i = 0; i < MapItemCount; i++)
        {
            if (mapItemImages[i] != null)
            {
                count++;
            }
        }

        return count;
    }

    private void EnsureRoomNameInputSetup()
    {
        if (roomNameInput == null)
        {
            return;
        }

        TMP_Text inputText = roomNameInput.textComponent;
        if (inputText == null)
        {
            Transform inputTextTransform = roomNameInput.transform.Find("InputText");
            if (inputTextTransform != null)
            {
                inputText = inputTextTransform.GetComponent<TMP_Text>();
            }
        }

        if (inputText != null)
        {
            roomNameInput.textComponent = inputText;
            inputText.enableWordWrapping = false;
            inputText.fontStyle = FontStyles.Normal;

            RectTransform inputTextRect = inputText.GetComponent<RectTransform>();
            ApplyInputPadding(inputTextRect);
        }

        TMP_Text placeholderText = roomNameInput.placeholder as TMP_Text;
        if (placeholderText == null)
        {
            Transform placeholderTransform = roomNameInput.transform.Find("Placeholder");
            if (placeholderTransform != null)
            {
                placeholderText = placeholderTransform.GetComponent<TMP_Text>();
            }
        }

        if (placeholderText == null)
        {
            GameObject placeholderObject = new GameObject(
                "Placeholder",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));

            RectTransform placeholderRect = placeholderObject.GetComponent<RectTransform>();
            placeholderRect.SetParent(roomNameInput.transform, false);

            placeholderText = placeholderObject.GetComponent<TMP_Text>();
        }

        if (placeholderText != null)
        {
            if (inputText != null)
            {
                placeholderText.font = inputText.font;
                placeholderText.fontSize = inputText.fontSize;
                placeholderText.alignment = inputText.alignment;
            }

            placeholderText.text = roomNamePlaceholderText;
            placeholderText.enableWordWrapping = false;
            placeholderText.fontStyle = FontStyles.Italic;
            placeholderText.color = new Color(1f, 1f, 1f, 0.55f);

            RectTransform placeholderRect = placeholderText.GetComponent<RectTransform>();
            ApplyInputPadding(placeholderRect);
            roomNameInput.placeholder = placeholderText;
        }

        Image inputImage = roomNameInput.GetComponent<Image>();
        if (inputImage != null)
        {
            roomNameInput.targetGraphic = inputImage;
        }

        if (roomNameInput.textViewport == null)
        {
            roomNameInput.textViewport = roomNameInput.GetComponent<RectTransform>();
        }

        roomNameInput.lineType = TMP_InputField.LineType.SingleLine;
        roomNameInput.richText = false;
    }

    private void ApplyInputPadding(RectTransform textRect)
    {
        if (textRect == null)
        {
            return;
        }

        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.offsetMin = new Vector2(20f, 8f);
        textRect.offsetMax = new Vector2(-20f, -8f);
    }

    private void CacheVisualPresets()
    {
        selectedButtonSprite = GetButtonSprite(player4Button) ??
                               GetButtonSprite(player3Button) ??
                               GetButtonSprite(player2Button);

        unselectedButtonSprite = GetButtonSprite(player2Button) ??
                                 GetButtonSprite(player3Button) ??
                                 GetButtonSprite(player4Button);

        if (publicToggleVisual != null)
        {
            toggleBaseSprite = publicToggleVisual.sprite;
        }

        if (toggleKnob != null)
        {
            Vector2 knobPositions = CalculateKnobPositions();
            publicKnobPosition = new Vector2(knobPositions.x, toggleKnob.anchoredPosition.y);
            privateKnobPosition = new Vector2(knobPositions.y, toggleKnob.anchoredPosition.y);
        }

        CacheMapVisualPresets();
    }

    private void CacheMapVisualPresets()
    {
        if (unselectedMapSprite == null)
        {
            if (mapItemImages[0] != null && mapItemImages[0].sprite != null)
            {
                unselectedMapSprite = mapItemImages[0].sprite;
            }
            else
            {
                for (int i = 0; i < MapItemCount; i++)
                {
                    if (mapItemImages[i] != null && mapItemImages[i].sprite != null)
                    {
                        unselectedMapSprite = mapItemImages[i].sprite;
                        break;
                    }
                }
            }
        }

        if (selectedMapSprite == null)
        {
            if (mapItemImages.Length > 2 && mapItemImages[2] != null && mapItemImages[2].sprite != null)
            {
                selectedMapSprite = mapItemImages[2].sprite;
            }
            else
            {
                for (int i = 0; i < MapItemCount; i++)
                {
                    if (mapItemImages[i] != null &&
                        mapItemImages[i].sprite != null &&
                        mapItemImages[i].sprite != unselectedMapSprite)
                    {
                        selectedMapSprite = mapItemImages[i].sprite;
                        break;
                    }
                }
            }
        }

        if (selectedMapSprite == null)
        {
            selectedMapSprite = unselectedMapSprite;
        }
    }

    private Vector2 CalculateKnobPositions()
    {
        if (toggleKnob == null)
        {
            return Vector2.zero;
        }

        if (publicToggleVisual == null)
        {
            float fallback = Mathf.Max(0f, Mathf.Abs(toggleKnob.anchoredPosition.x));
            return new Vector2(fallback, -fallback);
        }

        RectTransform trackRect = publicToggleVisual.rectTransform;
        bool leftAnchored =
            Mathf.Abs(toggleKnob.anchorMin.x) < 0.001f &&
            Mathf.Abs(toggleKnob.anchorMax.x) < 0.001f &&
            Mathf.Abs(toggleKnob.pivot.x) < 0.001f;

        if (leftAnchored)
        {
            float trackWidth = trackRect.rect.width;
            float trackHeight = trackRect.rect.height;
            float knobWidth = toggleKnob.rect.width;
            float knobHeight = toggleKnob.rect.height;
            float sidePadding = Mathf.Max(4f, (trackHeight - knobHeight) * 0.5f);

            float leftX = sidePadding;
            float rightX = Mathf.Max(leftX, trackWidth - knobWidth - sidePadding);
            return new Vector2(leftX, rightX);
        }

        float centeredRange = Mathf.Max(10f, (trackRect.rect.width - toggleKnob.rect.width) * 0.5f - 2f);
        return new Vector2(-centeredRange, centeredRange);
    }

    private static Sprite GetButtonSprite(Button button)
    {
        return button != null && button.image != null ? button.image.sprite : null;
    }

    private void AutoAssignReferences(bool allowAddComponents)
    {
        Transform[] allTransforms = transform.GetComponentsInChildren<Transform>(true);

        if (roomNameInput == null)
        {
            GameObject roomNameInputObject = FindGameObjectByName(allTransforms, "RoomNameInput");
            if (roomNameInputObject != null)
            {
                roomNameInput = roomNameInputObject.GetComponent<TMP_InputField>();
                if (roomNameInput == null && allowAddComponents)
                {
                    roomNameInput = roomNameInputObject.AddComponent<TMP_InputField>();
                }
            }
        }

        if (player2Button == null)
        {
            player2Button = FindButtonByName(allTransforms, "Player2Button");
        }

        if (player3Button == null)
        {
            player3Button = FindButtonByName(allTransforms, "Player3Button");
        }

        if (player4Button == null)
        {
            player4Button = FindButtonByName(allTransforms, "Player4Button");
        }

        if (publicToggleVisual == null)
        {
            GameObject toggleObject = FindGameObjectByName(allTransforms, "PublicToggleVisual");
            if (toggleObject != null)
            {
                publicToggleVisual = toggleObject.GetComponent<Image>();
            }
        }

        if (visibilityToggleButton == null)
        {
            visibilityToggleButton = FindButtonByName(allTransforms, "PublicToggleVisual");
        }

        if (toggleKnob == null)
        {
            GameObject knobObject = FindGameObjectByName(allTransforms, "ToggleKnob");
            if (knobObject != null)
            {
                toggleKnob = knobObject.GetComponent<RectTransform>();
            }
        }

        if (publicText == null)
        {
            publicText = FindTextByName(allTransforms, "PublicText");
        }

        if (privateText == null)
        {
            privateText = FindTextByName(allTransforms, "PrivateText");
        }

        if (minusButton == null)
        {
            minusButton = FindButtonByName(allTransforms, "MinusButton");
        }

        if (plusButton == null)
        {
            plusButton = FindButtonByName(allTransforms, "PlusButton");
        }

        if (countText == null)
        {
            countText = FindTextByName(allTransforms, "CountText");
        }

        AutoAssignMapReferences(allTransforms, allowAddComponents);
    }

    private void AutoAssignMapReferences(Transform[] allTransforms, bool allowAddComponents)
    {
        for (int i = 0; i < MapItemCount; i++)
        {
            string mapItemName = $"MapItem{i + 1:00}";
            GameObject mapItemObject = FindGameObjectByName(allTransforms, mapItemName);
            if (mapItemObject == null)
            {
                continue;
            }

            if (mapItemImages[i] == null)
            {
                mapItemImages[i] = mapItemObject.GetComponent<Image>();
            }

            if (mapItemImages[i] != null)
            {
                mapItemImages[i].raycastTarget = true;
            }

            if (mapItemButtons[i] == null)
            {
                mapItemButtons[i] = mapItemObject.GetComponent<Button>();
                if (mapItemButtons[i] == null && allowAddComponents)
                {
                    mapItemButtons[i] = mapItemObject.AddComponent<Button>();
                }
            }

            if (mapItemButtons[i] != null && mapItemImages[i] != null)
            {
                mapItemButtons[i].targetGraphic = mapItemImages[i];
            }
        }
    }

    private static Button FindButtonByName(Transform[] allTransforms, string targetName)
    {
        GameObject target = FindGameObjectByName(allTransforms, targetName);
        return target != null ? target.GetComponent<Button>() : null;
    }

    private static TMP_Text FindTextByName(Transform[] allTransforms, string targetName)
    {
        GameObject target = FindGameObjectByName(allTransforms, targetName);
        return target != null ? target.GetComponent<TMP_Text>() : null;
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
