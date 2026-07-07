using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

public class MobileUISetup : EditorWindow
{
    [MenuItem("Tools/Setup Mobile UI TPS")]
    public static void SetupMobileUI()
    {
        // ── EventSystem ──────────────────────────────────────────────────────
        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            Debug.Log("[MobileUI] EventSystem created");
        }

        // Hapus canvas lama
        var existing = GameObject.Find("MobileUI_Canvas");
        if (existing != null)
        {
            Object.DestroyImmediate(existing);
            Debug.Log("[MobileUI] Removed old MobileUI_Canvas");
        }

        // ── CANVAS ───────────────────────────────────────────────────────────
        var canvasGO = new GameObject("MobileUI_Canvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // ── LOAD SPRITES dari Joystick Pack ──────────────────────────────────
        var bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/Joystick Pack/Sprites/All Axis Backgrounds/AllAxis_Plain.png");
        var handleSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/Joystick Pack/Sprites/Handles/Handle_Plain.png");

        if (bgSprite == null)
            Debug.LogWarning("[MobileUI] AllAxis_Plain.png tidak ditemukan!");
        if (handleSprite == null)
            Debug.LogWarning("[MobileUI] Handle_Plain.png tidak ditemukan!");

        // ── JOYSTICK (kiri bawah) ─────────────────────────────────────────────
        var joystickGO = new GameObject("Fixed Joystick");
        joystickGO.transform.SetParent(canvasGO.transform, false);
        var joystickRect = joystickGO.AddComponent<RectTransform>();
        joystickRect.anchorMin = Vector2.zero;
        joystickRect.anchorMax = Vector2.zero;
        joystickRect.pivot = new Vector2(0.5f, 0.5f);
        joystickRect.anchoredPosition = new Vector2(200f, 200f);
        joystickRect.sizeDelta = new Vector2(256f, 256f);

        var joystickBgImg = joystickGO.AddComponent<Image>();
        joystickBgImg.sprite = bgSprite;
        joystickBgImg.color = new Color(1f, 1f, 1f, 0.5f);
        joystickBgImg.raycastTarget = true;

        // Handle child
        var handleGO = new GameObject("Handle");
        handleGO.transform.SetParent(joystickGO.transform, false);
        var handleRect = handleGO.AddComponent<RectTransform>();
        handleRect.anchorMin = new Vector2(0.5f, 0.5f);
        handleRect.anchorMax = new Vector2(0.5f, 0.5f);
        handleRect.pivot = new Vector2(0.5f, 0.5f);
        handleRect.anchoredPosition = Vector2.zero;
        handleRect.sizeDelta = new Vector2(128f, 128f);

        var handleImg = handleGO.AddComponent<Image>();
        handleImg.sprite = handleSprite;
        handleImg.color = new Color(1f, 1f, 1f, 0.8f);
        handleImg.raycastTarget = false;

        // Attach FixedJoystick
        var joystickType = System.Type.GetType("FixedJoystick, Assembly-CSharp");
        MonoBehaviour joystickComp = null;
        if (joystickType != null)
        {
            joystickComp = (MonoBehaviour)joystickGO.AddComponent(joystickType);
            SetPrivateField(joystickComp, joystickType, "background", joystickRect);
            SetPrivateField(joystickComp, joystickType, "handle", handleRect);
            Debug.Log("[MobileUI] FixedJoystick attached ✓");
        }
        else
        {
            Debug.LogWarning("[MobileUI] FixedJoystick type not found!");
        }

        // ── JUMP BUTTON (kanan bawah) ─────────────────────────────────────────
        // Gunakan MobileJumpButton (IPointerDownHandler) bukan Button.onClick
        // supaya jump langsung terpicu saat PointerDown, bukan PointerUp.
        var jumpGO = new GameObject("JumpButton");
        jumpGO.transform.SetParent(canvasGO.transform, false);
        var jumpRect = jumpGO.AddComponent<RectTransform>();
        jumpRect.anchorMin = new Vector2(1f, 0f);
        jumpRect.anchorMax = new Vector2(1f, 0f);
        jumpRect.pivot = new Vector2(0.5f, 0.5f);
        jumpRect.anchoredPosition = new Vector2(-200f, 200f);
        jumpRect.sizeDelta = new Vector2(150f, 150f);

        var jumpImg = jumpGO.AddComponent<Image>();
        jumpImg.sprite = handleSprite != null ? handleSprite : bgSprite;
        jumpImg.color = new Color(0.3f, 0.7f, 1f, 0.85f);
        jumpImg.raycastTarget = true;  // WAJIB true agar IPointerDownHandler berfungsi

        // ── MobileJumpButton script (IPointerDownHandler) ──
        var jumpBtnScript = jumpGO.AddComponent<MobileJumpButton>();

        // Label JUMP
        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(jumpGO.transform, false);
        var labelRect = labelGO.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        var txt = labelGO.AddComponent<Text>();
        txt.text = "JUMP";
        txt.alignment = TextAnchor.MiddleCenter;
        txt.fontSize = 28;
        txt.fontStyle = FontStyle.Bold;
        txt.color = Color.white;
        txt.raycastTarget = false;

        // ── WIRE KE PlayerTPS ─────────────────────────────────────────────────
        var player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            var tps = player.GetComponent<PlayerTPS>();
            if (tps != null)
            {
                // Wire MobileJumpButton → PlayerTPS (tersimpan di scene, tidak hilang saat play)
                var playerField = typeof(MobileJumpButton).GetField(
                    "player",
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance);
                if (playerField != null)
                {
                    playerField.SetValue(jumpBtnScript, tps);
                    Debug.Log("[MobileUI] MobileJumpButton.player → PlayerTPS ✓");
                }

                // Wire joystick ke PlayerTPS.mobileJoystick
                if (joystickComp != null)
                {
                    var joyField = typeof(PlayerTPS).GetField(
                        "mobileJoystick",
                        System.Reflection.BindingFlags.NonPublic |
                        System.Reflection.BindingFlags.Instance);
                    if (joyField != null)
                    {
                        joyField.SetValue(tps, joystickComp);
                        Debug.Log("[MobileUI] PlayerTPS.mobileJoystick → FixedJoystick ✓");
                    }
                }

                Debug.Log("[MobileUI] Wiring complete ✓");
            }
            else Debug.LogWarning("[MobileUI] PlayerTPS tidak ada di Player!");
        }
        else Debug.LogWarning("[MobileUI] Tidak ada GameObject bertag 'Player'!");

        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[MobileUI] ✅ Setup complete! Simpan scene lalu Play.");
    }

    static void SetPrivateField(object target, System.Type startType, string fieldName, object value)
    {
        var t = startType;
        while (t != null && t != typeof(MonoBehaviour))
        {
            var f = t.GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public);
            if (f != null) { f.SetValue(target, value); return; }
            t = t.BaseType;
        }
        Debug.LogWarning("[MobileUI] Field '" + fieldName + "' tidak ditemukan di " + startType.Name);
    }
}
#endif
