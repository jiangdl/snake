using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class GameOverUIController : MonoBehaviour
{
    [Header("References")]
    public SnakeGame snakeGame;
    public GameObject gameOverPanel;
    public Text finalScoreText;
    public Text leaderboardText;
    public Button restartButton;
    public Button clearHistoryButton;
    public GameObject confirmClearPanel;
    public Text confirmMessageText;
    public Button confirmYesButton;
    public Button confirmNoButton;

    [Header("Leaderboard")]
    private const int LeaderboardDisplayCount = 10;

    void Awake()
    {
        if (snakeGame == null)
            snakeGame = FindObjectOfType<SnakeGame>();

        EnsureUiReady();

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(RestartGame);
            restartButton.onClick.AddListener(RestartGame);
        }

        if (clearHistoryButton != null)
        {
            clearHistoryButton.onClick.RemoveListener(ShowClearHistoryConfirm);
            clearHistoryButton.onClick.AddListener(ShowClearHistoryConfirm);
        }

        if (confirmYesButton != null)
        {
            confirmYesButton.onClick.RemoveListener(ConfirmClearHistory);
            confirmYesButton.onClick.AddListener(ConfirmClearHistory);
        }

        if (confirmNoButton != null)
        {
            confirmNoButton.onClick.RemoveListener(CancelClearHistory);
            confirmNoButton.onClick.AddListener(CancelClearHistory);
        }

        if (confirmClearPanel != null)
            confirmClearPanel.SetActive(false);
    }

    void OnEnable()
    {
        if (snakeGame != null)
            snakeGame.GameOver += OnGameOver;
    }

    void OnDisable()
    {
        if (snakeGame != null)
            snakeGame.GameOver -= OnGameOver;
    }

    void OnGameOver(int score)
    {
        ScoreRepository.SaveScore(score);

        if (finalScoreText != null)
            finalScoreText.text = $"本局得分: {score}";

        if (leaderboardText != null)
            leaderboardText.text = BuildLeaderboardText();

        if (confirmClearPanel != null)
            confirmClearPanel.SetActive(false);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);
    }

    string BuildLeaderboardText()
    {
        var entries = ScoreRepository.GetTopEntries(LeaderboardDisplayCount);
        if (entries.Count == 0) return "排行榜\n暂无记录";

        var sb = new StringBuilder();
        sb.AppendLine("排行榜 (Top 10)");

        for (int i = 0; i < entries.Count; i++)
            sb.AppendLine($"{i + 1}. {entries[i].score}   {entries[i].timestamp}");

        return sb.ToString();
    }

    void EnsureUiReady()
    {
        if (gameOverPanel != null && finalScoreText != null && leaderboardText != null && restartButton != null && clearHistoryButton != null &&
            confirmClearPanel != null && confirmMessageText != null && confirmYesButton != null && confirmNoButton != null)
            return;

        EnsureEventSystem();

        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
        }

        if (gameOverPanel == null)
            gameOverPanel = CreatePanel(canvas.transform);

        if (finalScoreText == null)
        {
            finalScoreText = CreateText(gameOverPanel.transform, "FinalScoreText", new Vector2(0f, 200f), 44, TextAnchor.MiddleCenter);
            finalScoreText.rectTransform.sizeDelta = new Vector2(1000f, 60f);
        }

        if (leaderboardText == null)
        {
            leaderboardText = CreateText(gameOverPanel.transform, "LeaderboardText", new Vector2(0f, -40f), 34, TextAnchor.UpperCenter);
            leaderboardText.rectTransform.sizeDelta = new Vector2(1000f, 320f);
        }

        if (restartButton == null)
            restartButton = CreateButton(gameOverPanel.transform, "RestartButton", "重新开始", new Vector2(-180f, -220f));

        if (clearHistoryButton == null)
            clearHistoryButton = CreateButton(gameOverPanel.transform, "ClearHistoryButton", "清空记录", new Vector2(180f, -220f));

        if (confirmClearPanel == null)
            confirmClearPanel = CreateConfirmDialog(gameOverPanel.transform);

        if (confirmMessageText == null)
            confirmMessageText = CreateText(confirmClearPanel.transform, "ConfirmMessageText", new Vector2(0f, 40f), 30, TextAnchor.MiddleCenter);

        if (string.IsNullOrEmpty(confirmMessageText.text))
            confirmMessageText.text = "确认清空历史记录？";

        if (confirmYesButton == null)
            confirmYesButton = CreateButton(confirmClearPanel.transform, "ConfirmYesButton", "确认", new Vector2(-110f, -55f));

        if (confirmNoButton == null)
            confirmNoButton = CreateButton(confirmClearPanel.transform, "ConfirmNoButton", "取消", new Vector2(110f, -55f));
    }

    void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null) return;

        GameObject eventSystemGo = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        eventSystemGo.name = "EventSystem";
    }

    GameObject CreatePanel(Transform parent)
    {
        var panel = new GameObject("GameOverPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);

        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var image = panel.GetComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.72f);

        return panel;
    }

    Text CreateText(Transform parent, string nodeName, Vector2 anchoredPos, int fontSize, TextAnchor alignment)
    {
        var go = new GameObject(nodeName, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);

        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(1000f, 320f);
        rect.anchoredPosition = anchoredPos;

        var text = go.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        return text;
    }

    Button CreateButton(Transform parent, string buttonName, string labelText, Vector2 anchoredPos)
    {
        var buttonGo = new GameObject(buttonName, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonGo.transform.SetParent(parent, false);

        var rect = buttonGo.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(320f, 90f);
        rect.anchoredPosition = anchoredPos;

        var image = buttonGo.GetComponent<Image>();
        image.color = new Color(0.12f, 0.55f, 1f, 0.95f);

        var button = buttonGo.GetComponent<Button>();
        var colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = new Color(0.3f, 0.68f, 1f, 1f);
        colors.pressedColor = new Color(0.08f, 0.42f, 0.86f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        var label = CreateText(buttonGo.transform, "Text", Vector2.zero, 34, TextAnchor.MiddleCenter);
        label.rectTransform.sizeDelta = rect.sizeDelta;
        label.text = labelText;

        return button;
    }

    GameObject CreateConfirmDialog(Transform parent)
    {
        var dialog = new GameObject("ConfirmClearPanel", typeof(RectTransform), typeof(Image));
        dialog.transform.SetParent(parent, false);

        var rect = dialog.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(640f, 280f);
        rect.anchoredPosition = new Vector2(0f, -20f);

        var image = dialog.GetComponent<Image>();
        image.color = new Color(0.08f, 0.08f, 0.08f, 0.96f);

        return dialog;
    }

    public void RestartGame()
    {
        var scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.buildIndex);
    }

    public void ShowClearHistoryConfirm()
    {
        if (confirmClearPanel != null)
            confirmClearPanel.SetActive(true);
    }

    public void ConfirmClearHistory()
    {
        ScoreRepository.ClearHistory();

        if (leaderboardText != null)
            leaderboardText.text = BuildLeaderboardText();

        if (confirmClearPanel != null)
            confirmClearPanel.SetActive(false);
    }

    public void CancelClearHistory()
    {
        if (confirmClearPanel != null)
            confirmClearPanel.SetActive(false);
    }
}
