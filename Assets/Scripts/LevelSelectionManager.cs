using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the map-based level selection overlay in the Main Menu scene.
///
/// SETUP (Inspector):
///   1. Attach to a persistent GameObject in the Main Menu scene (e.g. "LevelSelectionManager").
///   2. Assign MapPanel (the overlay root), LevelNode buttons, padlock overlay, and IntroPanel.
///   3. Wire the PLAY button's OnClick() → LevelSelectionManager.OpenMapPanel().
///   4. Wire each LevelNode button's OnClick() → LevelSelectionManager.OnLevelNodeClicked(0/1).
///   5. Wire CloseButton's OnClick() → LevelSelectionManager.CloseMapPanel().
/// </summary>
public class LevelSelectionManager : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  Inspector References
    // ─────────────────────────────────────────────

    [Header("Panels")]
    [Tooltip("The root panel of the map overlay (dark BG + map nodes).")]
    public GameObject mapPanel;

    [Tooltip("The root panel of the level intro (3-page modal). Driven by LevelIntroNarrator.")]
    public GameObject introPanel;

    [Header("Level Nodes")]
    [Tooltip("Button for Level 1 node on the map.")]
    public Button levelNode1Button;

    [Tooltip("Button for Level 2 node on the map.")]
    public Button levelNode2Button;

    [Tooltip("Padlock icon image overlaid on Level 2 node when locked.")]
    public GameObject padlockOverlay;

    [Header("Toast")]
    [Tooltip("Optional: A short-lived text panel shown when clicking a locked node.")]
    public GameObject lockedToastPanel;
    public Text lockedToastText;
    private Coroutine _toastCoroutine;

    [Header("References")]
    [Tooltip("The LevelIntroNarrator component (usually on the same GameObject as IntroPanel).")]
    public LevelIntroNarrator introNarrator;

    // ─────────────────────────────────────────────
    //  Mono
    // ─────────────────────────────────────────────

    private void Start()
    {
        // Make sure both panels start closed
        if (mapPanel   != null) mapPanel.SetActive(false);
        if (introPanel != null) introPanel.SetActive(false);
        if (lockedToastPanel != null) lockedToastPanel.SetActive(false);

        RefreshNodeStates();
    }

    // ─────────────────────────────────────────────
    //  Public API — wired from Inspector buttons
    // ─────────────────────────────────────────────

    /// <summary>Called by the PLAY button in the Main Menu.</summary>
    public void OpenMapPanel()
    {
        RefreshNodeStates();
        if (mapPanel != null) mapPanel.SetActive(true);
    }

    /// <summary>Called by the Close / Back button on the Map Panel.</summary>
    public void CloseMapPanel()
    {
        if (mapPanel != null) mapPanel.SetActive(false);
    }

    /// <summary>
    /// Called by each Level Node button's OnClick() event.
    /// Pass 0 for Level 1, 1 for Level 2.
    /// </summary>
    public void OnLevelNodeClicked(int levelIndex)
    {
        // Check if this level is unlocked
        if (!IsLevelUnlocked(levelIndex))
        {
            ShowLockedToast();
            return;
        }

        // Hide the map and open the intro narrator for the selected level
        CloseMapPanel();
        OpenIntroPanel(levelIndex);
    }

    // ─────────────────────────────────────────────
    //  Internal helpers
    // ─────────────────────────────────────────────

    /// <summary>
    /// Reads PlayerPrefs and updates node interactivity + padlock visibility.
    /// Level 1 is always unlocked.
    /// Level 2 requires PlayerPrefs key "Level2Unlocked" == 1.
    /// </summary>
    private void RefreshNodeStates()
    {
        bool level2Unlocked = PlayerPrefs.GetInt("Level2Unlocked", 0) == 1;

        // Level 1 is always interactable
        if (levelNode1Button != null)
            levelNode1Button.interactable = true;

        // Level 2 node
        if (levelNode2Button != null)
            levelNode2Button.interactable = level2Unlocked;

        // Padlock overlay
        if (padlockOverlay != null)
            padlockOverlay.SetActive(!level2Unlocked);
    }

    private bool IsLevelUnlocked(int levelIndex)
    {
        if (levelIndex == 0) return true;                                    // Level 1 always unlocked
        if (levelIndex == 1) return PlayerPrefs.GetInt("Level2Unlocked", 0) == 1;
        return false;
    }

    private void OpenIntroPanel(int levelIndex)
    {
        if (introPanel == null || introNarrator == null)
        {
            Debug.LogError("[LevelSelectionManager] IntroPanel or LevelIntroNarrator not assigned!");
            return;
        }

        introPanel.SetActive(true);
        introNarrator.StartIntro(levelIndex);
    }

    private void ShowLockedToast()
    {
        if (lockedToastPanel == null) return;

        if (_toastCoroutine != null) StopCoroutine(_toastCoroutine);
        if (lockedToastText != null)
            lockedToastText.text = "🔒  Selesaikan Level 1 terlebih dahulu!";

        lockedToastPanel.SetActive(true);
        _toastCoroutine = StartCoroutine(HideToastAfterDelay(2.5f));
    }

    private System.Collections.IEnumerator HideToastAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (lockedToastPanel != null) lockedToastPanel.SetActive(false);
    }

#if UNITY_EDITOR
    // Quick test hotkeys in editor
    [ContextMenu("DEBUG: Unlock Level 2")]
    private void DebugUnlockLevel2()
    {
        PlayerPrefs.SetInt("Level2Unlocked", 1);
        PlayerPrefs.Save();
        RefreshNodeStates();
        Debug.Log("[LevelSelectionManager] Level 2 unlocked via debug context menu.");
    }

    [ContextMenu("DEBUG: Lock Level 2")]
    private void DebugLockLevel2()
    {
        PlayerPrefs.SetInt("Level2Unlocked", 0);
        PlayerPrefs.Save();
        RefreshNodeStates();
        Debug.Log("[LevelSelectionManager] Level 2 locked via debug context menu.");
    }
#endif
}
