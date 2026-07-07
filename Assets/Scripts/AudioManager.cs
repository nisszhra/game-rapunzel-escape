using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Singleton AudioManager — mengelola semua audio game:
///   - Main Menu Music  : loop di scene Main Menu
///   - In-Game Music    : loop di scene game (non-Main-Menu)
///   - Walk Sound       : diputar saat karakter bergerak, dihentikan saat diam
///
/// SETUP:
///   1. Jalankan Tools → Setup Audio Manager di Unity Editor.
///   2. Assign clip AudioSource (Main Menu Music, In-Game Music, Walk Sound) di Inspector.
///   3. Attach SettingsPanelUI ke Settings Panel di setiap scene.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    // ──────────────────────────────────────────
    //  Inspector
    // ──────────────────────────────────────────

    [Header("Audio Sources")]
    [Tooltip("AudioSource untuk musik main menu. Loop = true.")]
    public AudioSource mainMenuMusicSource;

    [Tooltip("AudioSource untuk musik in-game. Loop = true.")]
    public AudioSource inGameMusicSource;

    [Tooltip("AudioSource untuk efek suara langkah. Loop = true.")]
    public AudioSource walkSoundSource;

    [Header("Audio Clips")]
    [Tooltip("Clip musik main menu (main menu music.mp3).")]
    public AudioClip mainMenuMusicClip;

    [Tooltip("Clip musik in-game (in game music.mp3).")]
    public AudioClip inGameMusicClip;

    [Tooltip("Clip efek suara langkah (walk sound.mp3).")]
    public AudioClip walkSoundClip;

    [Header("Default Settings")]
    [Range(0f, 1f)]
    [Tooltip("Volume default saat pertama kali game dijalankan.")]
    public float defaultVolume = 1f;

    // ──────────────────────────────────────────
    //  Runtime State
    // ──────────────────────────────────────────

    private bool _musicEnabled = true;
    private bool _soundEnabled = true;
    private float _volume = 1f;

    // Nama scene main menu — sesuaikan jika berbeda
    private const string MAIN_MENU_SCENE = "Main Menu";

    // PlayerPrefs keys
    private const string KEY_MUSIC  = "AudioMusicOn";
    private const string KEY_SOUND  = "AudioSoundOn";
    private const string KEY_VOLUME = "AudioVolume";

    // ──────────────────────────────────────────
    //  Lifecycle
    // ──────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadPreferences();
        ApplyAllSettings();

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        // Mulai musik sesuai scene pertama
        PlayMusicForCurrentScene(SceneManager.GetActiveScene().name);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StopAllMusic();
        PlayMusicForCurrentScene(scene.name);
    }

    // ──────────────────────────────────────────
    //  Music Routing
    // ──────────────────────────────────────────

    private void PlayMusicForCurrentScene(string sceneName)
    {
        if (sceneName == MAIN_MENU_SCENE)
        {
            PlayMainMenuMusic();
        }
        else
        {
            PlayInGameMusic();
        }
    }

    private void PlayMainMenuMusic()
    {
        if (mainMenuMusicSource == null || mainMenuMusicClip == null) return;

        mainMenuMusicSource.clip  = mainMenuMusicClip;
        mainMenuMusicSource.loop  = true;
        mainMenuMusicSource.mute  = !_musicEnabled;

        if (!mainMenuMusicSource.isPlaying)
            mainMenuMusicSource.Play();
    }

    private void PlayInGameMusic()
    {
        if (inGameMusicSource == null || inGameMusicClip == null) return;

        inGameMusicSource.clip  = inGameMusicClip;
        inGameMusicSource.loop  = true;
        inGameMusicSource.mute  = !_musicEnabled;

        if (!inGameMusicSource.isPlaying)
            inGameMusicSource.Play();
    }

    private void StopAllMusic()
    {
        if (mainMenuMusicSource != null && mainMenuMusicSource.isPlaying)
            mainMenuMusicSource.Stop();

        if (inGameMusicSource != null && inGameMusicSource.isPlaying)
            inGameMusicSource.Stop();
    }

    // ──────────────────────────────────────────
    //  Walk Sound Control (dipanggil oleh PlayerTPS)
    // ──────────────────────────────────────────

    /// <summary>
    /// Dipanggil oleh PlayerTPS setiap frame — true saat karakter bergerak & grounded.
    /// </summary>
    public void SetWalkSoundActive(bool isWalking)
    {
        if (walkSoundSource == null) return;

        if (isWalking && _soundEnabled)
        {
            if (!walkSoundSource.isPlaying)
            {
                walkSoundSource.clip  = walkSoundClip;
                walkSoundSource.loop  = true;
                walkSoundSource.mute  = false;
                walkSoundSource.Play();
            }
        }
        else
        {
            if (walkSoundSource.isPlaying)
                walkSoundSource.Stop();
        }
    }

    // ──────────────────────────────────────────
    //  Public Settings API (dipanggil SettingsPanelUI)
    // ──────────────────────────────────────────

    /// <summary>Aktifkan / matikan musik background (main menu + in-game).</summary>
    public void SetMusicEnabled(bool enabled)
    {
        _musicEnabled = enabled;

        if (mainMenuMusicSource != null)
            mainMenuMusicSource.mute = !enabled;

        if (inGameMusicSource != null)
            inGameMusicSource.mute = !enabled;

        PlayerPrefs.SetInt(KEY_MUSIC, enabled ? 1 : 0);
        PlayerPrefs.Save();

        Debug.Log($"[AudioManager] Music: {(enabled ? "ON" : "OFF")}");
    }

    /// <summary>Aktifkan / matikan efek suara (walk sound).</summary>
    public void SetSoundEnabled(bool enabled)
    {
        _soundEnabled = enabled;

        if (walkSoundSource != null)
        {
            if (!enabled && walkSoundSource.isPlaying)
                walkSoundSource.Stop();

            walkSoundSource.mute = !enabled;
        }

        PlayerPrefs.SetInt(KEY_SOUND, enabled ? 1 : 0);
        PlayerPrefs.Save();

        Debug.Log($"[AudioManager] Sound: {(enabled ? "ON" : "OFF")}");
    }

    /// <summary>Set volume master (0–1). Mengubah AudioListener.volume.</summary>
    public void SetVolume(float volume)
    {
        _volume = Mathf.Clamp01(volume);
        AudioListener.volume = _volume;

        PlayerPrefs.SetFloat(KEY_VOLUME, _volume);
        PlayerPrefs.Save();

        Debug.Log($"[AudioManager] Volume: {_volume:F2}");
    }

    // ──────────────────────────────────────────
    //  Getters (untuk inisialisasi UI)
    // ──────────────────────────────────────────

    public bool IsMusicEnabled  => _musicEnabled;
    public bool IsSoundEnabled  => _soundEnabled;
    public float CurrentVolume  => _volume;

    // ──────────────────────────────────────────
    //  Internal
    // ──────────────────────────────────────────

    private void LoadPreferences()
    {
        _musicEnabled = PlayerPrefs.GetInt(KEY_MUSIC,  1) == 1;
        _soundEnabled = PlayerPrefs.GetInt(KEY_SOUND,  1) == 1;
        _volume       = PlayerPrefs.GetFloat(KEY_VOLUME, defaultVolume);
    }

    private void ApplyAllSettings()
    {
        AudioListener.volume = _volume;

        if (mainMenuMusicSource != null)
            mainMenuMusicSource.mute = !_musicEnabled;

        if (inGameMusicSource != null)
            inGameMusicSource.mute = !_musicEnabled;

        if (walkSoundSource != null)
            walkSoundSource.mute = !_soundEnabled;
    }
}
