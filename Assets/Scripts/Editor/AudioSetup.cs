#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Editor tool untuk setup AudioManager di scene yang sedang aktif.
///
/// CARA PAKAI:
///   Menu bar Unity → Tools → Setup Audio Manager
///
/// Yang dilakukan:
///   1. Membuat GameObject "AudioManager" dengan 3 AudioSource
///   2. Attach script AudioManager
///   3. Assign audio clips dari Assets/Sound/
///   4. Membuat Settings Panel UI elements (toggle buttons + volume slider)
///      di Settings Panel yang sudah ada
/// </summary>
public class AudioSetup : EditorWindow
{
    // ──────────────────────────────────────────
    //  Menu: Setup AudioManager
    // ──────────────────────────────────────────

    [MenuItem("Tools/Setup Audio Manager")]
    public static void SetupAudioManager()
    {
        // Hapus AudioManager lama jika sudah ada
        var existing = GameObject.Find("AudioManager");
        if (existing != null)
        {
            DestroyImmediate(existing);
            Debug.Log("[AudioSetup] AudioManager lama dihapus.");
        }

        // ── Buat GameObject AudioManager ──────────────────────────
        var go = new GameObject("AudioManager");
        var mgr = go.AddComponent<AudioManager>();

        // ── Buat 3 AudioSource ────────────────────────────────────
        // 1. Main Menu Music
        var mainMenuSrc = go.AddComponent<AudioSource>();
        mainMenuSrc.playOnAwake = false;
        mainMenuSrc.loop        = true;
        mainMenuSrc.volume      = 1f;
        mgr.mainMenuMusicSource = mainMenuSrc;

        // 2. In-Game Music
        var inGameSrc = go.AddComponent<AudioSource>();
        inGameSrc.playOnAwake = false;
        inGameSrc.loop        = true;
        inGameSrc.volume      = 1f;
        mgr.inGameMusicSource = inGameSrc;

        // 3. Walk Sound
        var walkSrc = go.AddComponent<AudioSource>();
        walkSrc.playOnAwake = false;
        walkSrc.loop        = true;
        walkSrc.volume      = 0.8f;
        mgr.walkSoundSource = walkSrc;

        // ── Assign Audio Clips ────────────────────────────────────
        var mainMenuClip = AssetDatabase.LoadAssetAtPath<AudioClip>(
            "Assets/Sound/main menu music.mp3");
        if (mainMenuClip != null)
            mgr.mainMenuMusicClip = mainMenuClip;
        else
            Debug.LogWarning("[AudioSetup] 'Assets/Sound/main menu music.mp3' tidak ditemukan!");

        var inGameClip = AssetDatabase.LoadAssetAtPath<AudioClip>(
            "Assets/Sound/in game music.mp3");
        if (inGameClip != null)
            mgr.inGameMusicClip = inGameClip;
        else
            Debug.LogWarning("[AudioSetup] 'Assets/Sound/in game music.mp3' tidak ditemukan!");

        var walkClip = AssetDatabase.LoadAssetAtPath<AudioClip>(
            "Assets/Sound/walk sound.mp3");
        if (walkClip != null)
            mgr.walkSoundSource.clip = walkClip;
        if (walkClip != null)
            mgr.walkSoundClip = walkClip;
        else
            Debug.LogWarning("[AudioSetup] 'Assets/Sound/walk sound.mp3' tidak ditemukan!");

        EditorUtility.SetDirty(go);
        Debug.Log("[AudioSetup] ✅ AudioManager GameObject berhasil dibuat!");

        // Mark scene dirty
        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[AudioSetup] Simpan scene (Ctrl+S) dan lanjutkan ke Setup Settings Panel UI.");
    }

    // ──────────────────────────────────────────
    //  Menu: Setup Settings Panel UI
    // ──────────────────────────────────────────

    [MenuItem("Tools/Setup Settings Panel UI")]
    public static void SetupSettingsPanelUI()
    {
        // Load sprites dari UI GAME
        var toggleOnSprite  = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI GAME/toogle on.png");
        var toggleOffSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI GAME/toogle off.png");
        var volumeBarSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI GAME/volume bar.png");

        if (toggleOnSprite == null)
            Debug.LogWarning("[AudioSetup] 'Assets/UI GAME/toogle on.png' tidak ditemukan!");
        if (toggleOffSprite == null)
            Debug.LogWarning("[AudioSetup] 'Assets/UI GAME/toogle off.png' tidak ditemukan!");
        if (volumeBarSprite == null)
            Debug.LogWarning("[AudioSetup] 'Assets/UI GAME/volume bar.png' tidak ditemukan!");

        // Cari Settings Panel di scene
        var settingsPanel = FindSettingsPanel();
        if (settingsPanel == null)
        {
            Debug.LogError("[AudioSetup] Settings Panel tidak ditemukan! " +
                           "Pastikan ada GameObject bernama 'SettingsPanel' atau 'Panel Setting' di scene.");
            return;
        }

        Debug.Log($"[AudioSetup] Settings Panel ditemukan: {settingsPanel.name}");

        // Attach SettingsPanelUI script
        var ui = settingsPanel.GetComponent<SettingsPanelUI>();
        if (ui == null)
            ui = settingsPanel.AddComponent<SettingsPanelUI>();

        // Assign sprites
        ui.toggleOnSprite  = toggleOnSprite;
        ui.toggleOffSprite = toggleOffSprite;

        // Cari atau buat MusicRow
        SetupToggleRow(settingsPanel.transform, "MusicRow", "MusicLabel",
            toggleOnSprite, toggleOffSprite,
            ref ui.musicToggleButton, ref ui.musicToggleImage, "Music");

        // Cari atau buat SoundRow
        SetupToggleRow(settingsPanel.transform, "SoundRow", "SoundLabel",
            toggleOnSprite, toggleOffSprite,
            ref ui.soundToggleButton, ref ui.soundToggleImage, "Sound");

        // Cari atau buat VolumeRow
        SetupVolumeRow(settingsPanel.transform, "VolumeRow", "VolumeLabel",
            volumeBarSprite, ref ui.volumeSlider);

        EditorUtility.SetDirty(settingsPanel);
        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[AudioSetup] ✅ Settings Panel UI berhasil di-setup! Simpan scene (Ctrl+S).");
    }

    // ──────────────────────────────────────────
    //  Helpers
    // ──────────────────────────────────────────

    static GameObject FindSettingsPanel()
    {
        // Coba beberapa kemungkinan nama
        string[] candidates = { "SettingsPanel", "Panel Setting", "PanelSetting",
                                 "SettingPanel", "Settings Panel", "settings" };
        foreach (var name in candidates)
        {
            var go = GameObject.Find(name);
            if (go != null) return go;
        }

        // Cari semua GameObject dan periksa yang mengandung "setting" (case-insensitive)
        var allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
        foreach (var obj in allObjects)
        {
            if (obj.name.ToLower().Contains("setting"))
                return obj;
        }

        return null;
    }

    static void SetupToggleRow(
        Transform panelRoot,
        string rowName,
        string labelName,
        Sprite onSprite,
        Sprite offSprite,
        ref Button outButton,
        ref Image outImage,
        string displayName)
    {
        // Coba temukan row yang sudah ada berdasarkan nama label
        Transform existingLabel = FindChildRecursive(panelRoot, labelName);
        Transform row = null;

        if (existingLabel != null)
        {
            row = existingLabel.parent;
            Debug.Log($"[AudioSetup] Found existing row for {displayName} via label '{labelName}'");
        }
        else
        {
            // Buat row baru
            var rowGO = new GameObject(rowName);
            rowGO.transform.SetParent(panelRoot, false);

            var hLayout = rowGO.AddComponent<HorizontalLayoutGroup>();
            hLayout.spacing = 20f;
            hLayout.childAlignment = TextAnchor.MiddleLeft;
            hLayout.childForceExpandWidth = false;
            hLayout.childForceExpandHeight = false;

            var rowRect = rowGO.GetComponent<RectTransform>();
            rowRect.sizeDelta = new Vector2(400f, 60f);

            // Label
            var labelGO = new GameObject(labelName);
            labelGO.transform.SetParent(rowGO.transform, false);
            var labelText = labelGO.AddComponent<Text>();
            labelText.text = displayName;
            labelText.fontSize = 28;
            labelText.fontStyle = FontStyle.Bold;
            labelText.color = Color.white;
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var labelRect = labelGO.GetComponent<RectTransform>();
            labelRect.sizeDelta = new Vector2(200f, 50f);

            row = rowGO.transform;
            Debug.Log($"[AudioSetup] Created new row for {displayName}");
        }

        // Buat toggle button di row
        var btnName = displayName + "ToggleBtn";

        // Hapus button lama jika ada
        var existingBtn = FindChildRecursive(row, btnName);
        if (existingBtn != null)
            DestroyImmediate(existingBtn.gameObject);

        var buttonGO = new GameObject(btnName);
        buttonGO.transform.SetParent(row, false);

        // Image background toggle
        var btnImage = buttonGO.AddComponent<Image>();
        btnImage.sprite = onSprite; // Default: ON
        btnImage.preserveAspect = true;

        var btnRect = buttonGO.GetComponent<RectTransform>();
        if (onSprite != null)
            btnRect.sizeDelta = new Vector2(onSprite.rect.width * 0.5f, onSprite.rect.height * 0.5f);
        else
            btnRect.sizeDelta = new Vector2(100f, 50f);

        var btn = buttonGO.AddComponent<Button>();

        // Nonaktifkan warna highlight bawaan Button (kita kontrol via sprite)
        var colors = btn.colors;
        colors.normalColor      = Color.white;
        colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        colors.pressedColor     = new Color(0.75f, 0.75f, 0.75f, 1f);
        colors.selectedColor    = Color.white;
        colors.fadeDuration     = 0.05f;
        btn.colors = colors;

        outButton = btn;
        outImage  = btnImage;
    }

    static void SetupVolumeRow(
        Transform panelRoot,
        string rowName,
        string labelName,
        Sprite volumeBarSprite,
        ref Slider outSlider)
    {
        Transform existingLabel = FindChildRecursive(panelRoot, labelName);
        Transform row = null;

        if (existingLabel != null)
        {
            row = existingLabel.parent;
        }
        else
        {
            var rowGO = new GameObject(rowName);
            rowGO.transform.SetParent(panelRoot, false);

            var hLayout = rowGO.AddComponent<HorizontalLayoutGroup>();
            hLayout.spacing = 20f;
            hLayout.childAlignment = TextAnchor.MiddleLeft;
            hLayout.childForceExpandWidth = false;
            hLayout.childForceExpandHeight = false;

            var rowRect = rowGO.GetComponent<RectTransform>();
            rowRect.sizeDelta = new Vector2(500f, 60f);

            var labelGO = new GameObject(labelName);
            labelGO.transform.SetParent(rowGO.transform, false);
            var labelText = labelGO.AddComponent<Text>();
            labelText.text = "Volume";
            labelText.fontSize = 28;
            labelText.fontStyle = FontStyle.Bold;
            labelText.color = Color.white;
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var labelRect = labelGO.GetComponent<RectTransform>();
            labelRect.sizeDelta = new Vector2(160f, 50f);

            row = rowGO.transform;
        }

        // Hapus slider lama jika ada
        var existingSlider = FindChildRecursive(row, "VolumeSlider");
        if (existingSlider != null)
            DestroyImmediate(existingSlider.gameObject);

        // ── Buat Slider ──────────────────────────────────────────
        var sliderGO = new GameObject("VolumeSlider");
        sliderGO.transform.SetParent(row, false);

        var sliderRect = sliderGO.AddComponent<RectTransform>();
        sliderRect.sizeDelta = new Vector2(300f, 40f);

        // Background image (volume bar sprite)
        var bgImage = sliderGO.AddComponent<Image>();
        if (volumeBarSprite != null)
        {
            bgImage.sprite = volumeBarSprite;
            bgImage.type   = Image.Type.Sliced;
        }
        else
        {
            bgImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);
        }

        // Fill Area
        var fillAreaGO = new GameObject("Fill Area");
        fillAreaGO.transform.SetParent(sliderGO.transform, false);
        var fillAreaRect = fillAreaGO.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = new Vector2(5f, 5f);
        fillAreaRect.offsetMax = new Vector2(-15f, -5f);

        // Fill Image
        var fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(fillAreaGO.transform, false);
        var fillRect = fillGO.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;
        var fillImage = fillGO.AddComponent<Image>();
        fillImage.color = new Color(0.95f, 0.75f, 0.2f, 1f); // Warna emas / game-like

        // Handle Slide Area
        var handleAreaGO = new GameObject("Handle Slide Area");
        handleAreaGO.transform.SetParent(sliderGO.transform, false);
        var handleAreaRect = handleAreaGO.AddComponent<RectTransform>();
        handleAreaRect.anchorMin = Vector2.zero;
        handleAreaRect.anchorMax = Vector2.one;
        handleAreaRect.offsetMin = new Vector2(10f, 0f);
        handleAreaRect.offsetMax = new Vector2(-10f, 0f);

        // Handle
        var handleGO = new GameObject("Handle");
        handleGO.transform.SetParent(handleAreaGO.transform, false);
        var handleRect = handleGO.AddComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(30f, 30f);
        var handleImage = handleGO.AddComponent<Image>();
        handleImage.color = Color.white;

        // Slider component
        var slider = sliderGO.AddComponent<Slider>();
        slider.fillRect        = fillRect;
        slider.handleRect      = handleRect;
        slider.minValue        = 0f;
        slider.maxValue        = 1f;
        slider.value           = 1f;
        slider.direction       = Slider.Direction.LeftToRight;

        outSlider = slider;
    }

    static Transform FindChildRecursive(Transform parent, string name)
    {
        if (parent == null) return null;
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            var found = FindChildRecursive(child, name);
            if (found != null) return found;
        }
        return null;
    }
}
#endif
