using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneLoad : MonoBehaviour
{
    [Header("Settings Panel")]
    public GameObject settingsPanel;

    [Header("Music")]
    public AudioSource backgroundMusic;
    public Toggle musicToggle;

    // Warna toggle: hijau = ON, abu-abu = OFF
    private readonly Color colorOn  = new Color(0.22f, 0.78f, 0.35f, 1f);
    private readonly Color colorOff = new Color(0.40f, 0.40f, 0.40f, 1f);

    private bool isMusicOn = true;

    private void Start()
    {
        // Pastikan settings panel awalnya tertutup
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        // Baca preferensi musik
        isMusicOn = PlayerPrefs.GetInt("MusicOn", 1) == 1;

        if (backgroundMusic != null)
            backgroundMusic.mute = !isMusicOn;

        if (musicToggle != null)
        {
            musicToggle.isOn = isMusicOn;
            UpdateToggleVisual(isMusicOn);
        }
    }

    public void LoadSceneBaru(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    public void QuitGame()
    {
        Debug.Log("Game Quit!");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void ToggleSettingsPanel()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(!settingsPanel.activeSelf);
    }

    public void OnMusicToggleChanged(bool value)
    {
        isMusicOn = value;

        if (backgroundMusic != null)
            backgroundMusic.mute = !isMusicOn;

        UpdateToggleVisual(isMusicOn);

        PlayerPrefs.SetInt("MusicOn", isMusicOn ? 1 : 0);
        PlayerPrefs.Save();

        Debug.Log("Music " + (isMusicOn ? "ON" : "OFF"));
    }

    private void UpdateToggleVisual(bool isOn)
    {
        if (musicToggle == null) return;

        // Update warna background toggle
        var bgImage = musicToggle.GetComponent<Image>();
        if (bgImage != null)
            bgImage.color = isOn ? colorOn : colorOff;

        // Geser handle: kanan = ON, kiri = OFF
        var handle = musicToggle.transform.Find("Handle");
        if (handle != null)
        {
            var rect = handle.GetComponent<RectTransform>();
            if (rect != null)
                rect.anchoredPosition = isOn ? new Vector2(18, 0) : new Vector2(-18, 0);
        }
    }
}
