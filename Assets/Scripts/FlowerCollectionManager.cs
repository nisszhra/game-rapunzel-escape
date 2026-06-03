using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Singleton manager yang melacak progress pengumpulan plant_flower_crystalbud.
/// Menampilkan popup notifikasi setiap kali bunga berhasil diambil.
/// Ketika semua bunga terkumpul, membuka Portal01.
/// </summary>
public class FlowerCollectionManager : MonoBehaviour
{
    public static FlowerCollectionManager Instance { get; private set; }

    [Header("Collection Settings")]
    public int totalFlowers = 3;
    private int collectedCount = 0;

    [Header("Portal Reference")]
    [Tooltip("Drag Portal01 GameObject ke sini, atau dibiarkan kosong (auto-find)")]
    public PortalController portalController;

    // UI References (dibuat secara dinamis)
    private Canvas uiCanvas;
    private GameObject popupPanel;
    private Text popupText;
    private Image popupBG;
    private Image accentLineImg;
    private Text counterText;
    private GameObject counterPanel;

    // Portal-enter popup (overlay gelap + teks)
    private GameObject portalPopupPanel;
    private Text portalPopupText;

    private Coroutine hidePopupCoroutine;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        CreateUI();
    }

    void Start()
    {
        // Auto-find portal jika belum di-assign via inspector
        if (portalController == null)
        {
            portalController = FindObjectOfType<PortalController>();
            if (portalController != null)
                Debug.Log("[FlowerCollectionManager] Portal ditemukan: " + portalController.name);
            else
                Debug.LogWarning("[FlowerCollectionManager] PortalController tidak ditemukan di scene!");
        }
    }

    void CreateUI()
    {
        // ── Canvas ──────────────────────────────────────────────
        GameObject canvasGO = new GameObject("FlowerCollectionCanvas");
        uiCanvas = canvasGO.AddComponent<Canvas>();
        uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        uiCanvas.sortingOrder = 100;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        // ── Counter Panel (pojok kanan atas) ───────────────────
        counterPanel = new GameObject("CounterPanel");
        counterPanel.transform.SetParent(canvasGO.transform, false);

        Image counterBG = counterPanel.AddComponent<Image>();
        counterBG.color = new Color(0f, 0f, 0f, 0.6f);

        RectTransform counterRect = counterPanel.GetComponent<RectTransform>();
        counterRect.anchorMin = new Vector2(1f, 1f);
        counterRect.anchorMax = new Vector2(1f, 1f);
        counterRect.pivot = new Vector2(1f, 1f);
        counterRect.anchoredPosition = new Vector2(-30f, -30f);
        counterRect.sizeDelta = new Vector2(220f, 70f);

        // Icon bunga di counter
        GameObject iconGO = new GameObject("FlowerIcon");
        iconGO.transform.SetParent(counterPanel.transform, false);
        Text iconText = iconGO.AddComponent<Text>();
        iconText.text = "🌸";
        iconText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        iconText.fontSize = 30;
        iconText.alignment = TextAnchor.MiddleLeft;
        iconText.color = Color.white;
        RectTransform iconRect = iconGO.GetComponent<RectTransform>();
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.offsetMin = new Vector2(10f, 0f);
        iconRect.offsetMax = new Vector2(-120f, 0f);

        // Teks counter
        GameObject counterTextGO = new GameObject("CounterText");
        counterTextGO.transform.SetParent(counterPanel.transform, false);
        counterText = counterTextGO.AddComponent<Text>();
        counterText.text = $"0 / {totalFlowers}";
        counterText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        counterText.fontSize = 28;
        counterText.fontStyle = FontStyle.Bold;
        counterText.alignment = TextAnchor.MiddleRight;
        counterText.color = Color.white;
        RectTransform counterTextRect = counterTextGO.GetComponent<RectTransform>();
        counterTextRect.anchorMin = Vector2.zero;
        counterTextRect.anchorMax = Vector2.one;
        counterTextRect.offsetMin = new Vector2(50f, 0f);
        counterTextRect.offsetMax = new Vector2(-15f, 0f);

        // ── Popup Panel (tengah bawah) ──────────────────────────
        popupPanel = new GameObject("PopupPanel");
        popupPanel.transform.SetParent(canvasGO.transform, false);

        popupBG = popupPanel.AddComponent<Image>();
        popupBG.color = new Color(0.06f, 0.06f, 0.1f, 0.92f);

        RectTransform popupRect = popupPanel.GetComponent<RectTransform>();
        popupRect.anchorMin = new Vector2(0.5f, 0f);
        popupRect.anchorMax = new Vector2(0.5f, 0f);
        popupRect.pivot = new Vector2(0.5f, 0f);
        popupRect.anchoredPosition = new Vector2(0f, 80f);
        popupRect.sizeDelta = new Vector2(560f, 90f);

        // Garis aksen atas popup
        GameObject accentLine = new GameObject("AccentLine");
        accentLine.transform.SetParent(popupPanel.transform, false);
        accentLineImg = accentLine.AddComponent<Image>();
        accentLineImg.color = new Color(0.85f, 0.3f, 0.85f, 1f); // pink default
        RectTransform accentRect = accentLine.GetComponent<RectTransform>();
        accentRect.anchorMin = new Vector2(0f, 1f);
        accentRect.anchorMax = new Vector2(1f, 1f);
        accentRect.pivot = new Vector2(0.5f, 1f);
        accentRect.anchoredPosition = Vector2.zero;
        accentRect.sizeDelta = new Vector2(0f, 5f);

        // Teks popup
        GameObject popupTextGO = new GameObject("PopupText");
        popupTextGO.transform.SetParent(popupPanel.transform, false);
        popupText = popupTextGO.AddComponent<Text>();
        popupText.text = "Bunga berhasil diambil!";
        popupText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        popupText.fontSize = 26;
        popupText.fontStyle = FontStyle.Bold;
        popupText.alignment = TextAnchor.MiddleCenter;
        popupText.color = Color.white;
        RectTransform popupTextRect = popupTextGO.GetComponent<RectTransform>();
        popupTextRect.anchorMin = Vector2.zero;
        popupTextRect.anchorMax = Vector2.one;
        popupTextRect.offsetMin = new Vector2(10f, 0f);
        popupTextRect.offsetMax = new Vector2(-10f, 0f);

        popupPanel.SetActive(false);

        // ── Portal Enter Popup (overlay tengah layar) ───────────
        CreatePortalEnterPopup(canvasGO.transform);
    }

    void CreatePortalEnterPopup(Transform parent)
    {
        portalPopupPanel = new GameObject("PortalEnterPopup");
        portalPopupPanel.transform.SetParent(parent, false);

        Image bg = portalPopupPanel.AddComponent<Image>();
        bg.color = new Color(0f, 0.05f, 0.1f, 0.88f);

        RectTransform rt = portalPopupPanel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(680f, 180f);

        // Garis cyan atas
        GameObject topLine = new GameObject("TopLine");
        topLine.transform.SetParent(portalPopupPanel.transform, false);
        Image topLineImg = topLine.AddComponent<Image>();
        topLineImg.color = new Color(0f, 1f, 0.95f, 1f);
        RectTransform topLineRT = topLine.GetComponent<RectTransform>();
        topLineRT.anchorMin = new Vector2(0f, 1f);
        topLineRT.anchorMax = new Vector2(1f, 1f);
        topLineRT.pivot = new Vector2(0.5f, 1f);
        topLineRT.anchoredPosition = Vector2.zero;
        topLineRT.sizeDelta = new Vector2(0f, 5f);

        // Garis cyan bawah
        GameObject botLine = new GameObject("BotLine");
        botLine.transform.SetParent(portalPopupPanel.transform, false);
        Image botLineImg = botLine.AddComponent<Image>();
        botLineImg.color = new Color(0f, 1f, 0.95f, 1f);
        RectTransform botLineRT = botLine.GetComponent<RectTransform>();
        botLineRT.anchorMin = new Vector2(0f, 0f);
        botLineRT.anchorMax = new Vector2(1f, 0f);
        botLineRT.pivot = new Vector2(0.5f, 0f);
        botLineRT.anchoredPosition = Vector2.zero;
        botLineRT.sizeDelta = new Vector2(0f, 5f);

        // Teks utama
        GameObject titleGO = new GameObject("PortalTitle");
        titleGO.transform.SetParent(portalPopupPanel.transform, false);
        Text titleText = titleGO.AddComponent<Text>();
        titleText.text = "🌀  PORTAL TERBUKA!";
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 34;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = new Color(0f, 1f, 0.95f, 1f);
        RectTransform titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0f, 0.55f);
        titleRT.anchorMax = new Vector2(1f, 1f);
        titleRT.offsetMin = new Vector2(10f, 0f);
        titleRT.offsetMax = new Vector2(-10f, -5f);

        // Sub teks
        GameObject subGO = new GameObject("PortalSub");
        subGO.transform.SetParent(portalPopupPanel.transform, false);
        portalPopupText = subGO.AddComponent<Text>();
        portalPopupText.text = "Semua bunga terkumpul — Menuju Level Berikutnya...";
        portalPopupText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        portalPopupText.fontSize = 22;
        portalPopupText.fontStyle = FontStyle.Italic;
        portalPopupText.alignment = TextAnchor.MiddleCenter;
        portalPopupText.color = new Color(0.8f, 0.95f, 1f, 0.9f);
        RectTransform subRT = subGO.GetComponent<RectTransform>();
        subRT.anchorMin = new Vector2(0f, 0f);
        subRT.anchorMax = new Vector2(1f, 0.5f);
        subRT.offsetMin = new Vector2(10f, 5f);
        subRT.offsetMax = new Vector2(-10f, 0f);

        portalPopupPanel.SetActive(false);
    }

    // ─────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────

    /// <summary>Dipanggil oleh FlowerCollectible ketika bunga berhasil diambil.</summary>
    public void OnFlowerCollected(string flowerName)
    {
        collectedCount++;
        collectedCount = Mathf.Clamp(collectedCount, 0, totalFlowers);

        counterText.text = $"{collectedCount} / {totalFlowers}";

        bool isComplete = collectedCount >= totalFlowers;

        string message;
        if (isComplete)
        {
            message = $"✨  Semua bunga terkumpul!  {collectedCount}/{totalFlowers}  — Portal Terbuka!";
            counterText.color = new Color(0f, 1f, 0.95f);
            if (accentLineImg != null) accentLineImg.color = new Color(0f, 1f, 0.95f, 1f);
            OpenPortalIfReady();
        }
        else
        {
            message = $"🌸  Bunga diambil!  {collectedCount}/{totalFlowers}";
            counterText.color = Color.white;
        }

        ShowPopup(message, isComplete);
        Debug.Log($"[FlowerCollection] {flowerName} diambil. Progress: {collectedCount}/{totalFlowers}");
    }

    /// <summary>Dipanggil oleh PortalController ketika player masuk portal.</summary>
    public void ShowPortalEnterPopup()
    {
        if (hidePopupCoroutine != null) StopCoroutine(hidePopupCoroutine);
        popupPanel.SetActive(false);
        portalPopupPanel.SetActive(true);
        Debug.Log("[FlowerCollectionManager] Menuju level berikutnya...");
    }

    public int GetCollectedCount() => collectedCount;
    public int GetTotalFlowers() => totalFlowers;

    // ─────────────────────────────────────────────
    //  Internal
    // ─────────────────────────────────────────────

    void OpenPortalIfReady()
    {
        if (portalController != null)
        {
            portalController.OpenPortal();
        }
        else
        {
            portalController = FindObjectOfType<PortalController>();
            if (portalController != null)
                portalController.OpenPortal();
            else
                Debug.LogWarning("[FlowerCollectionManager] PortalController tidak ditemukan!");
        }
    }

    void ShowPopup(string message, bool isComplete)
    {
        if (hidePopupCoroutine != null)
            StopCoroutine(hidePopupCoroutine);

        popupText.text = message;

        if (isComplete)
        {
            popupBG.color = new Color(0f, 0.08f, 0.12f, 0.95f);
            popupText.color = new Color(0f, 1f, 0.95f, 1f);
        }
        else
        {
            popupBG.color = new Color(0.06f, 0.06f, 0.1f, 0.92f);
            popupText.color = Color.white;
        }

        popupPanel.SetActive(true);
        hidePopupCoroutine = StartCoroutine(HidePopupAfterDelay(isComplete ? 4f : 2.5f));
    }

    IEnumerator HidePopupAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        popupPanel.SetActive(false);
    }
}
