using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// SceneLoad — Mengatur navigasi scene dan tombol di Main Menu / UI Game.
///
/// Catatan Audio:
///   Semua logik audio (musik, toggle, volume) kini dikelola oleh AudioManager.
///   Script ini tidak lagi mengelola AudioSource atau Toggle musik secara langsung.
/// </summary>
public class SceneLoad : MonoBehaviour
{
    [Header("Settings Panel")]
    [Tooltip("Root GameObject dari Settings Panel.")]
    public GameObject settingsPanel;

    private void Start()
    {
        // Pastikan settings panel awalnya tertutup
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    // ──────────────────────────────────────────
    //  Navigation
    // ──────────────────────────────────────────

    /// <summary>Load scene berdasarkan nama.</summary>
    public void LoadSceneBaru(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>Keluar dari game.</summary>
    public void QuitGame()
    {
        Debug.Log("Game Quit!");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ──────────────────────────────────────────
    //  Settings Panel
    // ──────────────────────────────────────────

    /// <summary>Buka / tutup Settings Panel.</summary>
    public void ToggleSettingsPanel()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(!settingsPanel.activeSelf);
    }

    /// <summary>Tutup Settings Panel.</summary>
    public void CloseSettingsPanel()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }
}
