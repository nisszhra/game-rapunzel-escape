using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class MushlightPuzzleManager : MonoBehaviour
{
    public static MushlightPuzzleManager Instance;

    private int lightsTurnedOn = 0;
    private int totalLightsNeeded = 3;

    private FlowerCollectible[] flowers;

    // UI for popup
    private GameObject popupPanel;
    private Text popupText;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // 1. Temukan semua bunga crystalbud dan sembunyikan
        flowers = FindObjectsByType<FlowerCollectible>(FindObjectsSortMode.None);
        foreach (var f in flowers)
        {
            f.gameObject.SetActive(false);
        }

        // 2. Buat UI Notifikasi Popup secara otomatis
        CreatePopupUI();
    }

    private void CreatePopupUI()
    {
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGo = new GameObject("PuzzleCanvas");
            canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();
        }

        // ── Popup Panel (tengah bawah) ──────────────────────────
        popupPanel = new GameObject("MushlightPopupPanel");
        popupPanel.transform.SetParent(canvas.transform, false);

        Image popupBG = popupPanel.AddComponent<Image>();
        popupBG.color = new Color(0.06f, 0.06f, 0.1f, 0.92f);

        RectTransform popupRect = popupPanel.GetComponent<RectTransform>();
        popupRect.anchorMin = new Vector2(0.5f, 0f);
        popupRect.anchorMax = new Vector2(0.5f, 0f);
        popupRect.pivot = new Vector2(0.5f, 0f);
        popupRect.anchoredPosition = new Vector2(0f, 80f);
        popupRect.sizeDelta = new Vector2(750f, 90f); // Lebar sedikit ditambah agar teks muat

        // Garis aksen atas popup
        GameObject accentLine = new GameObject("AccentLine");
        accentLine.transform.SetParent(popupPanel.transform, false);
        Image accentLineImg = accentLine.AddComponent<Image>();
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
        popupText.text = "Lentera telah dinyalakan semua! \n Saatnya cari flower dengan hint yang telah disediakan";
        popupText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        popupText.fontSize = 20;
        popupText.fontStyle = FontStyle.Bold;
        popupText.alignment = TextAnchor.MiddleCenter;
        popupText.color = Color.white;
        RectTransform popupTextRect = popupTextGO.GetComponent<RectTransform>();
        popupTextRect.anchorMin = Vector2.zero;
        popupTextRect.anchorMax = Vector2.one;
        popupTextRect.offsetMin = new Vector2(10f, 0f);
        popupTextRect.offsetMax = new Vector2(-10f, 0f);

        popupPanel.SetActive(false);
    }

    public void OnMushlightTurnedOn()
    {
        lightsTurnedOn++;
        Debug.Log("Mushlight dinyalakan: " + lightsTurnedOn + "/" + totalLightsNeeded);
        
        if (lightsTurnedOn >= totalLightsNeeded)
        {
            CompletePuzzle();
        }
    }

    private void CompletePuzzle()
    {
        // Munculkan semua bunga
        foreach (var f in flowers)
        {
            if (f != null) f.gameObject.SetActive(true);
        }

        // Tampilkan Popup Notifikasi
        StartCoroutine(ShowPopupRoutine());
    }

    private IEnumerator ShowPopupRoutine()
    {
        if (popupPanel != null)
        {
            popupPanel.SetActive(true);
            
            // Tampilkan selama 5 detik lalu hilangkan
            yield return new WaitForSeconds(5f); 
            
            popupPanel.SetActive(false);
        }
    }
}
