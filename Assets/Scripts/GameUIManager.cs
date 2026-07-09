using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Mengelola semua UI gameplay di MobileUI_Canvas scene TPS:
///  - PauseButton  → buka/tutup PausePanel
///  - ResumeButton, RestartButton, HomeButton → aksi dari PausePanel
///  - CollectPanel → menampilkan progress bunga terkumpul (X/3)
///  - TimerPanel   → countdown 2 menit, game over bila habis
///  - GameOverPanel → muncul otomatis saat waktu habis
///
/// SETUP:
///  1. Attach script ini ke MobileUI_Canvas (atau GameObject manapun di scene TPS).
///  2. Assign semua field di Inspector.
///  3. TIDAK perlu wire OnClick di Inspector — script ini sudah AddListener otomatis.
/// </summary>
public class GameUIManager : MonoBehaviour
{
    // ──────────────────────────────────────────────────────────
    //  Inspector Fields
    // ──────────────────────────────────────────────────────────

    [Header("=== PAUSE ===")]
    [Tooltip("Button pause (di luar PausePanel).")]
    public Button pauseButton;

    [Tooltip("Panel pause yang muncul saat game di-pause.")]
    public GameObject pausePanel;

    [Tooltip("Tombol Resume di dalam PausePanel.")]
    public Button resumeButton;

    [Tooltip("Tombol Restart di dalam PausePanel.")]
    public Button restartButton;

    [Tooltip("Tombol Home di dalam PausePanel.")]
    public Button homeButton;

    [Header("=== COLLECT PANEL ===")]
    [Tooltip("Panel yang menampilkan jumlah bunga terkumpul.")]
    public GameObject collectPanel;

    [Tooltip("Text di dalam CollectPanel untuk menampilkan 'X / 3'.")]
    public Text collectText;

    [Header("=== TIMER PANEL ===")]
    [Tooltip("Panel yang menampilkan timer countdown.")]
    public GameObject timerPanel;

    [Tooltip("Text di dalam TimerPanel untuk menampilkan waktu MM:SS.")]
    public Text timerText;

    [Tooltip("Durasi countdown dalam detik. Default 120 = 2 menit.")]
    public float countdownSeconds = 120f;

    [Tooltip("Warna timer saat normal.")]
    public Color timerNormalColor = Color.white;

    [Tooltip("Warna timer saat tersisa ≤ 30 detik (peringatan).")]
    public Color timerWarningColor = new Color(1f, 0.35f, 0.1f, 1f); // oranye-merah

    [Header("=== GAME OVER PANEL ===")]
    [Tooltip("Panel Game Over — dibuat otomatis jika dikosongkan.")]
    public GameObject gameOverPanel;

    [Tooltip("Tombol Restart di dalam GameOverPanel (opsional, bisa auto-find).")]
    public Button gameOverRestartButton;

    [Tooltip("Tombol Home di dalam GameOverPanel (opsional, bisa auto-find).")]
    public Button gameOverHomeButton;

    [Header("=== SCENE NAMES ===")]
    [Tooltip("Nama scene Main Menu persis seperti di Build Settings.")]
    public string mainMenuSceneName = "Main Menu";

    [Tooltip("Nama scene TPS persis seperti di Build Settings.")]
    public string gameSceneName = "TPS";

    // ──────────────────────────────────────────────────────────
    //  Private State
    // ──────────────────────────────────────────────────────────

    private float   _timeLeft;
    private bool    _isPaused    = false;
    private bool    _isGameOver  = false;
    private bool    _timerActive = false;

    // ──────────────────────────────────────────────────────────
    //  Unity Lifecycle
    // ──────────────────────────────────────────────────────────

    private void Awake()
    {
        // Auto-find by name jika belum di-assign di Inspector
        AutoFindReferences();

        // Pastikan GameOverPanel dibuat jika belum ada
        EnsureGameOverPanel();
    }

    private void Start()
    {
        // ── Inisialisasi state awal ──
        Time.timeScale = 1f;
        _isPaused      = false;
        _isGameOver    = false;
        _timeLeft      = countdownSeconds;
        _timerActive   = true;

        // ── Sembunyikan panel di awal ──
        if (pausePanel   != null) pausePanel.SetActive(false);
        if (gameOverPanel!= null) gameOverPanel.SetActive(false);

        // ── Wire button listeners ──
        WireButtons();

        // ── Subscribe ke event FlowerCollectionManager ──
        FlowerCollectionManager.OnFlowerCountChanged += UpdateCollectUI;

        // ── Inisialisasi tampilan awal ──
        UpdateCollectUI(0, FlowerCollectionManager.Instance != null
            ? FlowerCollectionManager.Instance.GetTotalFlowers() : 3);
        UpdateTimerUI();
    }

    private void OnDestroy()
    {
        FlowerCollectionManager.OnFlowerCountChanged -= UpdateCollectUI;
    }

    private void Update()
    {
        if (_timerActive && !_isPaused && !_isGameOver)
        {
            _timeLeft -= Time.deltaTime;
            _timeLeft  = Mathf.Max(_timeLeft, 0f);
            UpdateTimerUI();

            if (_timeLeft <= 0f)
            {
                TriggerGameOver();
            }
        }
    }

    // ──────────────────────────────────────────────────────────
    //  Pause Logic
    // ──────────────────────────────────────────────────────────

    /// <summary>Toggle pause saat PauseButton diklik.</summary>
    public void OnPauseClicked()
    {
        if (_isGameOver) return;

        if (_isPaused)
            Resume();
        else
            Pause();
    }

    /// <summary>Pause game: tampilkan PausePanel, freeze time.</summary>
    public void Pause()
    {
        _isPaused = true;
        Time.timeScale = 0f;
        if (pausePanel != null) pausePanel.SetActive(true);
    }

    /// <summary>Resume game: tutup PausePanel, lanjutkan time.</summary>
    public void Resume()
    {
        _isPaused = false;
        Time.timeScale = 1f;
        if (pausePanel != null) pausePanel.SetActive(false);
    }

    /// <summary>Restart scene TPS.</summary>
    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameSceneName);
    }

    /// <summary>Kembali ke Main Menu.</summary>
    public void GoHome()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    // ──────────────────────────────────────────────────────────
    //  Collect Panel UI
    // ──────────────────────────────────────────────────────────

    /// <summary>Dipanggil oleh event FlowerCollectionManager.OnFlowerCountChanged.</summary>
    private void UpdateCollectUI(int collected, int total)
    {
        if (collectText == null) return;
        collectText.text = $"{collected} / {total}";

        // Warna berubah jadi cyan saat semua terkumpul
        collectText.color = (collected >= total && total > 0)
            ? new Color(0f, 1f, 0.95f, 1f)
            : Color.white;
    }

    // ──────────────────────────────────────────────────────────
    //  Timer UI
    // ──────────────────────────────────────────────────────────

    private void UpdateTimerUI()
    {
        if (timerText == null) return;

        int minutes = Mathf.FloorToInt(_timeLeft / 60f);
        int seconds = Mathf.FloorToInt(_timeLeft % 60f);
        timerText.text = $"{minutes:00}:{seconds:00}";

        // Warning color saat ≤ 30 detik
        timerText.color = (_timeLeft <= 30f) ? timerWarningColor : timerNormalColor;
    }

    // ──────────────────────────────────────────────────────────
    //  Game Over
    // ──────────────────────────────────────────────────────────

    private void TriggerGameOver()
    {
        if (_isGameOver) return;
        _isGameOver  = true;
        _timerActive = false;
        Time.timeScale = 0f;

        if (pausePanel   != null) pausePanel.SetActive(false);
        if (gameOverPanel!= null) gameOverPanel.SetActive(true);

        Debug.Log("[GameUIManager] GAME OVER — waktu habis!");
    }

    // ──────────────────────────────────────────────────────────
    //  Helper: Wire Buttons
    // ──────────────────────────────────────────────────────────

    private void WireButtons()
    {
        if (pauseButton   != null) pauseButton.onClick.AddListener(OnPauseClicked);
        if (resumeButton  != null) resumeButton.onClick.AddListener(Resume);
        if (restartButton != null) restartButton.onClick.AddListener(Restart);
        if (homeButton    != null) homeButton.onClick.AddListener(GoHome);

        // GameOver panel buttons
        if (gameOverRestartButton != null) gameOverRestartButton.onClick.AddListener(Restart);
        if (gameOverHomeButton    != null) gameOverHomeButton.onClick.AddListener(GoHome);
    }

    // ──────────────────────────────────────────────────────────
    //  Helper: Auto-Find References by Name
    // ──────────────────────────────────────────────────────────

    private void AutoFindReferences()
    {
        // Cari dari dalam MobileUI_Canvas (parent canvas)
        Canvas canvas = GetComponentInParent<Canvas>();
        Transform root = (canvas != null) ? canvas.transform : transform;

        TryFindButton(ref pauseButton,   root, "PauseButton");
        TryFindGO    (ref pausePanel,    root, "PausePanel");
        TryFindButton(ref resumeButton,  root, "ResumeButton");
        TryFindButton(ref restartButton, root, "RestartButton");
        TryFindButton(ref homeButton,    root, "HomeButton");

        TryFindGO    (ref collectPanel,  root, "CollectPanel");
        TryFindGO    (ref timerPanel,    root, "TimerPanel");
        TryFindGO    (ref gameOverPanel, root, "GameOverPanel");

        // Cari Text di dalam CollectPanel & TimerPanel
        if (collectText == null && collectPanel != null)
            collectText = collectPanel.GetComponentInChildren<Text>(true);
        if (timerText   == null && timerPanel   != null)
            timerText   = timerPanel.GetComponentInChildren<Text>(true);
    }

    private static void TryFindGO(ref GameObject field, Transform root, string name)
    {
        if (field != null) return;
        Transform t = FindDeep(root, name);
        if (t != null) field = t.gameObject;
    }

    private static void TryFindButton(ref Button field, Transform root, string name)
    {
        if (field != null) return;
        Transform t = FindDeep(root, name);
        if (t != null) field = t.GetComponent<Button>();
    }

    private static Transform FindDeep(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        foreach (Transform child in parent)
        {
            Transform found = FindDeep(child, name);
            if (found != null) return found;
        }
        return null;
    }

    // ──────────────────────────────────────────────────────────
    //  Helper: Buat GameOverPanel jika belum ada
    // ──────────────────────────────────────────────────────────

    private void EnsureGameOverPanel()
    {
        if (gameOverPanel != null) return;

        // Cari canvas root
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("[GameUIManager] Tidak menemukan Canvas untuk membuat GameOverPanel.");
            return;
        }

        Transform canvasT = canvas.transform;

        // ── Root panel (full-screen semi-transparent) ──
        gameOverPanel = new GameObject("GameOverPanel");
        gameOverPanel.transform.SetParent(canvasT, false);

        Image bg = gameOverPanel.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.82f);

        RectTransform bgRT = gameOverPanel.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;

        // ── Kotak tengah ──
        GameObject box = new GameObject("Box");
        box.transform.SetParent(gameOverPanel.transform, false);

        Image boxImg = box.AddComponent<Image>();
        boxImg.color = new Color(0.08f, 0.05f, 0.12f, 0.97f);

        RectTransform boxRT = box.GetComponent<RectTransform>();
        boxRT.anchorMin = new Vector2(0.5f, 0.5f);
        boxRT.anchorMax = new Vector2(0.5f, 0.5f);
        boxRT.pivot     = new Vector2(0.5f, 0.5f);
        boxRT.sizeDelta = new Vector2(520f, 320f);
        boxRT.anchoredPosition = Vector2.zero;

        // Garis merah atas
        CreateLine(box.transform, "TopLine", new Color(1f, 0.2f, 0.2f, 1f), isTop: true);
        CreateLine(box.transform, "BotLine", new Color(1f, 0.2f, 0.2f, 1f), isTop: false);

        // ── Judul "GAME OVER" ──
        GameObject titleGO = new GameObject("TitleText");
        titleGO.transform.SetParent(box.transform, false);
        Text title = titleGO.AddComponent<Text>();
        title.text      = "⏰  WAKTU HABIS!";
        title.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        title.fontSize  = 42;
        title.fontStyle = FontStyle.Bold;
        title.alignment = TextAnchor.MiddleCenter;
        title.color     = new Color(1f, 0.3f, 0.3f, 1f);
        RectTransform titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0f, 0.6f);
        titleRT.anchorMax = new Vector2(1f, 1f);
        titleRT.offsetMin = new Vector2(10f, -10f);
        titleRT.offsetMax = new Vector2(-10f, -10f);

        // ── Sub teks ──
        GameObject subGO = new GameObject("SubText");
        subGO.transform.SetParent(box.transform, false);
        Text sub = subGO.AddComponent<Text>();
        sub.text      = "Waktu kamu telah habis.\nCoba lagi?";
        sub.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        sub.fontSize  = 22;
        sub.alignment = TextAnchor.MiddleCenter;
        sub.color     = new Color(0.85f, 0.85f, 0.9f, 1f);
        RectTransform subRT = subGO.GetComponent<RectTransform>();
        subRT.anchorMin = new Vector2(0f, 0.38f);
        subRT.anchorMax = new Vector2(1f, 0.62f);
        subRT.offsetMin = new Vector2(10f, 0f);
        subRT.offsetMax = new Vector2(-10f, 0f);

        // ── Tombol Restart ──
        gameOverRestartButton = CreateSimpleButton(
            box.transform, "GORestartButton", "🔄  Restart",
            new Vector2(-90f, -90f), new Vector2(180f, 55f),
            new Color(0.85f, 0.2f, 0.2f, 1f));

        // ── Tombol Home ──
        gameOverHomeButton = CreateSimpleButton(
            box.transform, "GOHomeButton", "🏠  Menu Utama",
            new Vector2(90f, -90f), new Vector2(180f, 55f),
            new Color(0.2f, 0.2f, 0.6f, 1f));

        gameOverPanel.SetActive(false);
        Debug.Log("[GameUIManager] GameOverPanel dibuat secara otomatis.");
    }

    private void CreateLine(Transform parent, string name, Color color, bool isTop)
    {
        GameObject lineGO = new GameObject(name);
        lineGO.transform.SetParent(parent, false);
        Image img = lineGO.AddComponent<Image>();
        img.color = color;
        RectTransform rt = lineGO.GetComponent<RectTransform>();
        rt.anchorMin = isTop ? new Vector2(0f, 1f) : new Vector2(0f, 0f);
        rt.anchorMax = isTop ? new Vector2(1f, 1f) : new Vector2(1f, 0f);
        rt.pivot     = isTop ? new Vector2(0.5f, 1f) : new Vector2(0.5f, 0f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(0f, 5f);
    }

    private Button CreateSimpleButton(Transform parent, string name, string label,
        Vector2 anchoredPos, Vector2 size, Color bgColor)
    {
        GameObject btnGO = new GameObject(name);
        btnGO.transform.SetParent(parent, false);

        Image btnImg = btnGO.AddComponent<Image>();
        btnImg.color = bgColor;

        Button btn = btnGO.AddComponent<Button>();

        ColorBlock cb = btn.colors;
        cb.normalColor      = bgColor;
        cb.highlightedColor = bgColor * 1.2f;
        cb.pressedColor     = bgColor * 0.7f;
        btn.colors = cb;

        RectTransform rt = btnGO.GetComponent<RectTransform>();
        rt.anchorMin       = new Vector2(0.5f, 0.5f);
        rt.anchorMax       = new Vector2(0.5f, 0.5f);
        rt.pivot           = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition= anchoredPos;
        rt.sizeDelta       = size;

        // Label text
        GameObject textGO = new GameObject("Label");
        textGO.transform.SetParent(btnGO.transform, false);
        Text txt = textGO.AddComponent<Text>();
        txt.text      = label;
        txt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize  = 20;
        txt.fontStyle = FontStyle.Bold;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color     = Color.white;
        RectTransform txtRT = textGO.GetComponent<RectTransform>();
        txtRT.anchorMin = Vector2.zero;
        txtRT.anchorMax = Vector2.one;
        txtRT.offsetMin = Vector2.zero;
        txtRT.offsetMax = Vector2.zero;

        return btn;
    }
}
