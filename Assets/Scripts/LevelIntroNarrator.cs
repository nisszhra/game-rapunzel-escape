using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages the multi-page level introduction modal (Story → Mission → Tutorial).
/// Supports 2 levels, each with 3 pages of distinct content.
///
/// SETUP (Inspector):
///   1. Attach to the IntroPanel root GameObject.
///   2. Assign all UI references (pageIndicatorText, titleText, contentText, etc.).
///   3. Assign sceneLoad reference (SceneLoad component in the scene).
///   4. The TutorialImage is shown/hidden automatically on page 3.
///   5. LevelSelectionManager calls StartIntro(levelIndex) to begin.
/// </summary>
public class LevelIntroNarrator : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  Inspector References
    // ─────────────────────────────────────────────

    [Header("UI References")]
    [Tooltip("e.g. '1 / 3'")]
    public TextMeshProUGUI pageIndicatorText;

    [Tooltip("Page title — 'Kisah', 'Misi', 'Tutorial'")]
    public TextMeshProUGUI pageTitleText;

    [Tooltip("Main body text for Story and Mission pages.")]
    public TextMeshProUGUI pageContentText;

    [Tooltip("Image shown only on page 3 (Tutorial). Assign a control guide sprite.")]
    public Image tutorialImage;

    [Tooltip("The Next / START GAME button.")]
    public Button nextButton;

    [Tooltip("Text label on the Next button.")]
    public TextMeshProUGUI nextButtonLabel;

    [Tooltip("Back button — returns to map panel (optional).")]
    public Button backButton;

    [Header("References")]
    [Tooltip("Assign the SceneLoad component from the scene.")]
    public SceneLoad sceneLoad;

    [Tooltip("Panel to re-open when the user clicks Back (the MapPanel).")]
    public GameObject mapPanel;

    // ─────────────────────────────────────────────
    //  Private State
    // ─────────────────────────────────────────────

    private int _currentPage  = 0;   // 0-based index
    private int _currentLevel = 0;   // 0 = Level 1, 1 = Level 2
    private const int TOTAL_PAGES = 3;

    // ─────────────────────────────────────────────
    //  Page Content Data
    // ─────────────────────────────────────────────

    // Each entry: [levelIndex][pageIndex]
    private static readonly string[] PageTitles = { "✨ Kisah", "🎯 Misi", "🕹️ Tutorial" };

    private static readonly string[,] PageContent = new string[2, 3]
    {
        // ── LEVEL 1 ─────────────────────────────────────────────────────────────────
        {
            // Page 1 — Story
            "Rapunzel menatap langit dari dalam menara...\n\n" +
            "\"Gothel telah menenun ilusi yang menutupi seluruh hutan ini. " +
            "Bunga-bunga Sundrop yang bercahaya adalah satu-satunya kunci untuk " +
            "menembus tirai sihirnya.\"\n\n" +
            "\"Aku harus menemukan semua bunga itu sebelum malam festival lentera tiba. " +
            "Cahayanya adalah harapanku satu-satunya untuk pulang.\"",

            // Page 2 — Mission
            "🌸  MISI LEVEL 1\n\n" +
            "Kumpulkan  <color=#00FFCC><b>3 Sundrop Flower</b></color>  yang tersebar di hutan.\n\n" +
            "Setelah semua bunga terkumpul, portal cahaya akan terbuka.\n\n" +
            "Masuki portal untuk melanjutkan perjalananmu menuju Festival Lentera!",

            // Page 3 — Tutorial (shown alongside tutorialImage)
            "Gunakan kontrol berikut untuk menggerakkan Rapunzel:"
        },
        // ── LEVEL 2 ─────────────────────────────────────────────────────────────────
        {
            // Page 1 — Story
            "Rapunzel kini berdiri di pinggir hutan berkabut...\n\n" +
            "\"Aku berhasil melewati ilusi Gothel! Tapi kabutnya semakin tebal " +
            "dan Festival Lentera sudah terlihat dari kejauhan.\"\n\n" +
            "\"Lentera-lentera kristal kuno ini — jika aku menyalakan semuanya, " +
            "cahayanya akan memecah kabut sihir dan membuka jalanku menuju kebebasan.\"",

            // Page 2 — Mission
            "🏮  MISI LEVEL 2\n\n" +
            "Nyalakan  <color=#FFD700><b>3 Crystal Lantern</b></color>  yang tersembunyi dalam kabut.\n\n" +
            "Setiap lentera yang menyala akan mengusir sebagian kabut ajaib Gothel.\n\n" +
            "Nyalakan semua lentera untuk membuka jalan menuju Festival Lentera!",

            // Page 3 — Tutorial
            "Kontrol sama seperti sebelumnya — kamu sudah berpengalaman sekarang!"
        }
    };

    private static readonly string[] PageSceneNames = { "TPS", "TPS 2" };

    // ─────────────────────────────────────────────
    //  Mono
    // ─────────────────────────────────────────────

    private void Awake()
    {
        if (nextButton != null)
            nextButton.onClick.AddListener(OnNextClicked);

        if (backButton != null)
            backButton.onClick.AddListener(OnBackClicked);
    }

    private void OnDestroy()
    {
        if (nextButton != null)
            nextButton.onClick.RemoveListener(OnNextClicked);
        if (backButton != null)
            backButton.onClick.RemoveListener(OnBackClicked);
    }

    // ─────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────

    /// <summary>
    /// Called by LevelSelectionManager when a level node is clicked.
    /// levelIndex: 0 = Level 1, 1 = Level 2.
    /// </summary>
    public void StartIntro(int levelIndex)
    {
        _currentLevel = Mathf.Clamp(levelIndex, 0, 1);
        _currentPage  = 0;
        DisplayPage(_currentPage);
    }

    // ─────────────────────────────────────────────
    //  Button Callbacks
    // ─────────────────────────────────────────────

    private void OnNextClicked()
    {
        bool isLastPage = (_currentPage >= TOTAL_PAGES - 1);

        if (isLastPage)
        {
            // Load the gameplay scene for the selected level
            StartGameplay();
        }
        else
        {
            _currentPage++;
            DisplayPage(_currentPage);
        }
    }

    private void OnBackClicked()
    {
        if (_currentPage > 0)
        {
            // Go back one page inside the modal
            _currentPage--;
            DisplayPage(_currentPage);
        }
        else
        {
            // Close intro and reopen map
            gameObject.SetActive(false);
            if (mapPanel != null) mapPanel.SetActive(true);
        }
    }

    // ─────────────────────────────────────────────
    //  Internal Helpers
    // ─────────────────────────────────────────────

    private void DisplayPage(int pageIndex)
    {
        // Page indicator
        if (pageIndicatorText != null)
            pageIndicatorText.text = $"{pageIndex + 1}  /  {TOTAL_PAGES}";

        // Title
        if (pageTitleText != null)
            pageTitleText.text = PageTitles[pageIndex];

        // Content
        if (pageContentText != null)
            pageContentText.text = PageContent[_currentLevel, pageIndex];

        // Tutorial image — only on page 3 (index 2)
        bool isTutorialPage = (pageIndex == 2);
        if (tutorialImage != null)
            tutorialImage.gameObject.SetActive(isTutorialPage);

        // Next button label
        bool isLastPage = (pageIndex >= TOTAL_PAGES - 1);
        if (nextButtonLabel != null)
            nextButtonLabel.text = isLastPage ? "🚀  START GAME" : "Lanjut  ➜";

        // Back button — hide on first page to avoid navigating away accidentally
        if (backButton != null)
            backButton.gameObject.SetActive(true); // always show; clicking on page 0 returns to map
    }

    private void StartGameplay()
    {
        string targetScene = PageSceneNames[_currentLevel];

        // Use SceneLoad if assigned (plays any fade/transition effects on it)
        if (sceneLoad != null)
        {
            sceneLoad.LoadSceneBaru(targetScene);
        }
        else
        {
            // Fallback: direct load
            Debug.LogWarning("[LevelIntroNarrator] SceneLoad not assigned — loading scene directly.");
            UnityEngine.SceneManagement.SceneManager.LoadScene(targetScene);
        }
    }
}
