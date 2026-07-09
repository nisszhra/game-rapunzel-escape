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

    [Header("=== TIMES UP PANEL ===")]
    [Tooltip("Panel Times Up (yang sudah dibuat di hirarki).")]
    public GameObject timesUpPanel;

    [Tooltip("Tombol Restart di dalam TimesUpPanel.")]
    public Button timesUpRestartButton;

    [Tooltip("Tombol Home di dalam TimesUpPanel.")]
    public Button timesUpHomeButton;

    [Header("=== END LEVEL PANEL ===")]
    [Tooltip("Panel End Level (yang sudah dibuat di hirarki).")]
    public GameObject endLevelPanel;

    [Tooltip("Tombol Next di dalam EndLevelPanel.")]
    public Button endLevelNextButton;

    [Tooltip("Tombol Home di dalam EndLevelPanel.")]
    public Button endLevelHomeButton;

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
        // Tambahkan sistem Minimap secara otomatis
        if (GetComponent<MinimapSystem>() == null)
        {
            gameObject.AddComponent<MinimapSystem>();
        }

        // Auto-find by name jika belum di-assign di Inspector
        AutoFindReferences();
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
        if (timesUpPanel != null) timesUpPanel.SetActive(false);
        if (endLevelPanel != null) endLevelPanel.SetActive(false);

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

    /// <summary>Lanjut ke level berikutnya.</summary>
    public void NextLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("TPS 2");
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
        if (timesUpPanel != null) timesUpPanel.SetActive(true);

        Debug.Log("[GameUIManager] TIMES UP — waktu habis!");
    }

    // ──────────────────────────────────────────────────────────
    //  End Level
    // ──────────────────────────────────────────────────────────
    
    public void ShowEndLevelPanel()
    {
        if (_isGameOver) return;
        _isGameOver  = true;
        _timerActive = false;
        Time.timeScale = 0f;

        if (pausePanel   != null) pausePanel.SetActive(false);
        if (endLevelPanel != null) endLevelPanel.SetActive(true);

        Debug.Log("[GameUIManager] LEVEL COMPLETE!");
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

        // TimesUp panel buttons
        if (timesUpRestartButton != null) timesUpRestartButton.onClick.AddListener(Restart);
        if (timesUpHomeButton    != null) timesUpHomeButton.onClick.AddListener(GoHome);

        // EndLevel panel buttons
        if (endLevelNextButton != null) endLevelNextButton.onClick.AddListener(NextLevel);
        if (endLevelHomeButton != null) endLevelHomeButton.onClick.AddListener(GoHome);
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

        TryFindGO    (ref timesUpPanel, root, "TimesUpPanel");
        if (timesUpPanel != null)
        {
            TryFindButton(ref timesUpRestartButton, timesUpPanel.transform, "RestartButton");
            TryFindButton(ref timesUpHomeButton,    timesUpPanel.transform, "HomeButton");
        }

        TryFindGO    (ref endLevelPanel, root, "EndLevelPanel");
        if (endLevelPanel != null)
        {
            TryFindButton(ref endLevelNextButton, endLevelPanel.transform, "NextButton");
            TryFindButton(ref endLevelHomeButton, endLevelPanel.transform, "HomeButton");
        }

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
}
