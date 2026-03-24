using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class CoinCollectScoreManager : MonoBehaviour
{
    private const string TargetSceneName = "CoinCollectScene";
    private const string UIRootObjectName = "UIRoot";
    private const string CanvasObjectName = "Canvas";
    private const string ScoreTextObjectName = "ScoreText";
    private const string TimerTextObjectName = "TimerText";
    private const string ResultPanelObjectName = "ResultPanel";
    private const string ResultTitleTextObjectName = "ResultTitleText";
    private const string ResultScoreTextObjectName = "ResultScoreText";
    private const int RoundDurationSeconds = 60;

    private static CoinCollectScoreManager instance;

    private int score;
    private float timeRemainingSeconds;
    private bool isRoundFinished;
    private int displayedTime = -1;
    private TextMeshProUGUI scoreText;
    private TextMeshProUGUI timerText;
    private GameObject resultPanel;
    private TextMeshProUGUI resultTitleText;
    private TextMeshProUGUI resultScoreText;

    public static bool IsRoundFinished
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<CoinCollectScoreManager>();
            }

            return instance != null && instance.isRoundFinished;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid() || activeScene.name != TargetSceneName)
        {
            return;
        }

        if (FindFirstObjectByType<CoinCollectScoreManager>() != null)
        {
            return;
        }

        GameObject managerObject = new GameObject(nameof(CoinCollectScoreManager));
        managerObject.AddComponent<CoinCollectScoreManager>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        Transform uiRoot = ResolveUiRoot();
        transform.SetParent(uiRoot, false);

        Canvas canvas = ResolveCanvas(uiRoot);
        scoreText = ResolveScoreText(canvas.transform);
        timerText = ResolveTimerText(canvas.transform);
        ResolveResultPanel(canvas.transform);
        timeRemainingSeconds = RoundDurationSeconds;
        isRoundFinished = false;
        UpdateScoreText();
        UpdateTimerText(true);
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    public static void AddScore(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        if (instance == null)
        {
            instance = FindFirstObjectByType<CoinCollectScoreManager>();
        }

        if (instance == null)
        {
            return;
        }

        if (instance.isRoundFinished)
        {
            return;
        }

        instance.score += amount;
        instance.UpdateScoreText();
    }

    private void Update()
    {
        if (isRoundFinished)
        {
            return;
        }

        timeRemainingSeconds = Mathf.Max(0f, timeRemainingSeconds - Time.deltaTime);
        if (timeRemainingSeconds <= 0f)
        {
            timeRemainingSeconds = 0f;
            isRoundFinished = true;
            HandleRoundCompleted();
        }

        UpdateTimerText();
    }

    private void UpdateScoreText()
    {
        if (scoreText == null)
        {
            return;
        }

        scoreText.text = $"Score: {score}";
    }

    private void UpdateTimerText(bool forceUpdate = false)
    {
        if (timerText == null)
        {
            return;
        }

        int safeSeconds = Mathf.CeilToInt(Mathf.Clamp(timeRemainingSeconds, 0f, RoundDurationSeconds));
        if (!forceUpdate && safeSeconds == displayedTime)
        {
            return;
        }

        displayedTime = safeSeconds;
        timerText.text = $"Time: {safeSeconds}";
    }

    private void HandleRoundCompleted()
    {
        DisablePlayerMovement();
        ShowResultPanel();
    }

    private void DisablePlayerMovement()
    {
        TestPlayerLocalMovement[] movers = FindObjectsByType<TestPlayerLocalMovement>(FindObjectsSortMode.None);
        for (int i = 0; i < movers.Length; i++)
        {
            if (movers[i] != null)
            {
                movers[i].enabled = false;
            }
        }
    }

    private void ShowResultPanel()
    {
        if (resultPanel == null)
        {
            return;
        }

        if (resultTitleText != null)
        {
            resultTitleText.text = "Round Complete";
        }

        if (resultScoreText != null)
        {
            resultScoreText.text = $"Coins Collected: {score}";
        }

        resultPanel.SetActive(true);
    }

    private static Transform ResolveUiRoot()
    {
        GameObject uiRootObject = GameObject.Find(UIRootObjectName);
        if (uiRootObject == null)
        {
            uiRootObject = new GameObject(UIRootObjectName);
        }

        return uiRootObject.transform;
    }

    private static Canvas ResolveCanvas(Transform uiRoot)
    {
        Canvas existingCanvas = uiRoot.GetComponentInChildren<Canvas>(true);
        if (existingCanvas != null)
        {
            return existingCanvas;
        }

        GameObject canvasObject = new GameObject(
            CanvasObjectName,
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(uiRoot, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
    }

    private static TextMeshProUGUI ResolveScoreText(Transform canvasTransform)
    {
        Transform scoreTextTransform = canvasTransform.Find(ScoreTextObjectName);
        if (scoreTextTransform == null)
        {
            GameObject scoreTextObject = new GameObject(
                ScoreTextObjectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            scoreTextObject.transform.SetParent(canvasTransform, false);
            scoreTextTransform = scoreTextObject.transform;
        }

        RectTransform rectTransform = scoreTextTransform as RectTransform;
        if (rectTransform != null)
        {
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(0f, 1f);
            rectTransform.pivot = new Vector2(0f, 1f);
            rectTransform.anchoredPosition = new Vector2(24f, -24f);
            rectTransform.sizeDelta = new Vector2(360f, 60f);
        }

        TextMeshProUGUI text = scoreTextTransform.GetComponent<TextMeshProUGUI>();
        if (text == null)
        {
            text = scoreTextTransform.gameObject.AddComponent<TextMeshProUGUI>();
        }

        text.fontSize = 36f;
        text.color = Color.white;
        text.enableWordWrapping = false;
        text.alignment = TextAlignmentOptions.Left;

        return text;
    }

    private static TextMeshProUGUI ResolveTimerText(Transform canvasTransform)
    {
        Transform timerTextTransform = canvasTransform.Find(TimerTextObjectName);
        if (timerTextTransform == null)
        {
            GameObject timerTextObject = new GameObject(
                TimerTextObjectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            timerTextObject.transform.SetParent(canvasTransform, false);
            timerTextTransform = timerTextObject.transform;
        }

        RectTransform rectTransform = timerTextTransform as RectTransform;
        if (rectTransform != null)
        {
            rectTransform.anchorMin = new Vector2(1f, 1f);
            rectTransform.anchorMax = new Vector2(1f, 1f);
            rectTransform.pivot = new Vector2(1f, 1f);
            rectTransform.anchoredPosition = new Vector2(-24f, -24f);
            rectTransform.sizeDelta = new Vector2(360f, 60f);
        }

        TextMeshProUGUI text = timerTextTransform.GetComponent<TextMeshProUGUI>();
        if (text == null)
        {
            text = timerTextTransform.gameObject.AddComponent<TextMeshProUGUI>();
        }

        text.fontSize = 36f;
        text.color = Color.white;
        text.enableWordWrapping = false;
        text.alignment = TextAlignmentOptions.Right;

        return text;
    }

    private void ResolveResultPanel(Transform canvasTransform)
    {
        Transform panelTransform = canvasTransform.Find(ResultPanelObjectName);
        if (panelTransform == null)
        {
            GameObject panelObject = new GameObject(
                ResultPanelObjectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            panelObject.transform.SetParent(canvasTransform, false);
            panelTransform = panelObject.transform;
        }

        resultPanel = panelTransform.gameObject;

        RectTransform panelRect = panelTransform as RectTransform;
        if (panelRect != null)
        {
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(700f, 280f);
        }

        Image panelImage = resultPanel.GetComponent<Image>();
        if (panelImage == null)
        {
            panelImage = resultPanel.AddComponent<Image>();
        }

        panelImage.color = new Color(0f, 0f, 0f, 0.75f);

        resultTitleText = ResolveResultChildText(
            panelTransform,
            ResultTitleTextObjectName,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -36f),
            new Vector2(620f, 70f),
            48f,
            TextAlignmentOptions.Center,
            "Round Complete");

        resultScoreText = ResolveResultChildText(
            panelTransform,
            ResultScoreTextObjectName,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, -28f),
            new Vector2(620f, 70f),
            38f,
            TextAlignmentOptions.Center,
            $"Coins Collected: {score}");

        resultPanel.SetActive(false);
    }

    private static TextMeshProUGUI ResolveResultChildText(
        Transform parent,
        string objectName,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 anchoredPosition,
        Vector2 sizeDelta,
        float fontSize,
        TextAlignmentOptions alignment,
        string defaultText)
    {
        Transform childTransform = parent.Find(objectName);
        if (childTransform == null)
        {
            GameObject textObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            childTransform = textObject.transform;
        }

        RectTransform rectTransform = childTransform as RectTransform;
        if (rectTransform != null)
        {
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.pivot = pivot;
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = sizeDelta;
        }

        TextMeshProUGUI text = childTransform.GetComponent<TextMeshProUGUI>();
        if (text == null)
        {
            text = childTransform.gameObject.AddComponent<TextMeshProUGUI>();
        }

        text.fontSize = fontSize;
        text.color = Color.white;
        text.enableWordWrapping = false;
        text.alignment = alignment;
        text.text = defaultText;

        return text;
    }
}
