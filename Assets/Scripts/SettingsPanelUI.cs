using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// SettingsPanelUI — Mengontrol tampilan dan interaksi Settings Panel.
///
/// SETUP (Inspector):
///   1. Attach script ini ke root Settings Panel GameObject.
///   2. Assign:
///      - musicToggleButton   : Button di samping MusicLabel
///      - soundToggleButton   : Button di samping SoundLabel
///      - volumeSlider        : Slider di samping VolumeLabel
///      - toggleOnSprite      : Sprite dari Assets/UI GAME/toogle on.png
///      - toggleOffSprite     : Sprite dari Assets/UI GAME/toogle off.png
///   3. Wire Button.OnClick() ke method OnMusicToggleClicked / OnSoundToggleClicked di sini.
///   4. Wire Slider.OnValueChanged() ke OnVolumeChanged.
/// </summary>
public class SettingsPanelUI : MonoBehaviour
{
    // ──────────────────────────────────────────
    //  Inspector References
    // ──────────────────────────────────────────

    [Header("Music Toggle")]
    [Tooltip("Button yang berfungsi sebagai toggle musik (di samping MusicLabel).")]
    public Button musicToggleButton;

    [Tooltip("Image child dari musicToggleButton yang menampilkan sprite ON/OFF.")]
    public Image musicToggleImage;

    [Header("Sound Toggle")]
    [Tooltip("Button yang berfungsi sebagai toggle suara (di samping SoundLabel).")]
    public Button soundToggleButton;

    [Tooltip("Image child dari soundToggleButton yang menampilkan sprite ON/OFF.")]
    public Image soundToggleImage;

    [Header("Volume Slider")]
    [Tooltip("Slider untuk mengatur volume master (di samping VolumeLabel).")]
    public Slider volumeSlider;

    [Header("Toggle Sprites")]
    [Tooltip("Sprite 'toogle on.png' dari folder UI GAME.")]
    public Sprite toggleOnSprite;

    [Tooltip("Sprite 'toogle off.png' dari folder UI GAME.")]
    public Sprite toggleOffSprite;

    // ──────────────────────────────────────────
    //  Lifecycle
    // ──────────────────────────────────────────

    private void OnEnable()
    {
        // Inisialisasi UI state dari AudioManager setiap kali panel dibuka
        InitializeFromAudioManager();
    }

    private void Start()
    {
        // Pastikan listener volume slider terhubung
        if (volumeSlider != null)
        {
            volumeSlider.minValue = 0f;
            volumeSlider.maxValue = 1f;
            volumeSlider.onValueChanged.RemoveAllListeners();
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }

        // Pastikan listener button terhubung
        if (musicToggleButton != null)
        {
            musicToggleButton.onClick.RemoveAllListeners();
            musicToggleButton.onClick.AddListener(OnMusicToggleClicked);
        }

        if (soundToggleButton != null)
        {
            soundToggleButton.onClick.RemoveAllListeners();
            soundToggleButton.onClick.AddListener(OnSoundToggleClicked);
        }

        InitializeFromAudioManager();
    }

    // ──────────────────────────────────────────
    //  Public Callbacks (Inspector wiring)
    // ──────────────────────────────────────────

    /// <summary>Dipanggil saat music toggle button ditekan.</summary>
    public void OnMusicToggleClicked()
    {
        if (AudioManager.Instance == null) return;

        bool newState = !AudioManager.Instance.IsMusicEnabled;
        AudioManager.Instance.SetMusicEnabled(newState);
        RefreshMusicToggleVisual(newState);

        Debug.Log($"[SettingsPanelUI] Music Toggle → {(newState ? "ON" : "OFF")}");
    }

    /// <summary>Dipanggil saat sound toggle button ditekan.</summary>
    public void OnSoundToggleClicked()
    {
        if (AudioManager.Instance == null) return;

        bool newState = !AudioManager.Instance.IsSoundEnabled;
        AudioManager.Instance.SetSoundEnabled(newState);
        RefreshSoundToggleVisual(newState);

        Debug.Log($"[SettingsPanelUI] Sound Toggle → {(newState ? "ON" : "OFF")}");
    }

    /// <summary>Dipanggil saat nilai slider volume berubah.</summary>
    public void OnVolumeChanged(float value)
    {
        if (AudioManager.Instance == null) return;
        AudioManager.Instance.SetVolume(value);
    }

    // ──────────────────────────────────────────
    //  Internal
    // ──────────────────────────────────────────

    private void InitializeFromAudioManager()
    {
        if (AudioManager.Instance == null)
        {
            Debug.LogWarning("[SettingsPanelUI] AudioManager.Instance tidak ditemukan! " +
                             "Pastikan AudioManager ada di scene.");
            return;
        }

        RefreshMusicToggleVisual(AudioManager.Instance.IsMusicEnabled);
        RefreshSoundToggleVisual(AudioManager.Instance.IsSoundEnabled);

        if (volumeSlider != null)
        {
            // Matikan listener sementara agar tidak trigger callback saat set nilai
            volumeSlider.onValueChanged.RemoveAllListeners();
            volumeSlider.value = AudioManager.Instance.CurrentVolume;
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }
    }

    private void RefreshMusicToggleVisual(bool isOn)
    {
        if (musicToggleImage == null) return;
        musicToggleImage.sprite = isOn ? toggleOnSprite : toggleOffSprite;
        // Jangan gunakan SetNativeSize() — biarkan RectTransform sizeDelta mengontrol ukuran
    }

    private void RefreshSoundToggleVisual(bool isOn)
    {
        if (soundToggleImage == null) return;
        soundToggleImage.sprite = isOn ? toggleOnSprite : toggleOffSprite;
        // Jangan gunakan SetNativeSize() — biarkan RectTransform sizeDelta mengontrol ukuran
    }
}
