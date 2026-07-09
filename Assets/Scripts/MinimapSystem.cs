using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Script dinamis untuk membuat Minimap secara otomatis saat runtime.
/// Menyiapkan Camera, RenderTexture, UI RawImage, serta indikator untuk bunga.
/// Indikator bunga hanya terlihat oleh kamera minimap.
/// </summary>
public class MinimapSystem : MonoBehaviour
{
    [Header("Minimap Settings")]
    public float minimapHeight = 100f; // Ditinggikan agar di atas semua pohon
    public float minimapOrthographicSize = 8f; // Diperkecil agar "zoom in" / tidak terlalu jauh
    public int textureResolution = 512; // Resolusi dinaikkan agar tidak blur saat dibesarkan

    private Camera minimapCamera;
    private RenderTexture renderTexture;
    private Transform playerTransform;

    private class IndicatorData {
        public Transform target;
        public Transform icon;
        public bool isFlower;
    }
    private List<IndicatorData> dynamicIndicators = new List<IndicatorData>();

    // List renderer indikator agar bisa dimatikan/dinyalakan saat rendering
    private List<Renderer> indicatorRenderers = new List<Renderer>();

    // Referensi ke objek UI agar bisa dihapus saat pindah scene
    private GameObject minimapUIObject;

    void Start()
    {
        // 1. Cari Player
        FindPlayer();

        // 2. Buat Render Texture
        renderTexture = new RenderTexture(textureResolution, textureResolution, 16, RenderTextureFormat.ARGB32);
        renderTexture.Create();

        // 3. Setup Kamera Minimap
        SetupCamera();

        // 4. Setup UI di Canvas
        SetupUI();

        // 5. Buat Indikator untuk player
        SetupPlayerIndicator();

        // 6. Buat Indikator untuk bunga
        SetupFlowerIndicators();

        // Daftarkan event camera rendering agar indikator hanya terlihat di minimap
        Camera.onPreCull += OnCameraPreCull;
        Camera.onPostRender += OnCameraPostRender;
        
        // Dukungan untuk URP (Universal Render Pipeline) jika digunakan
        UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        UnityEngine.Rendering.RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
    }

    void OnDestroy()
    {
        Camera.onPreCull -= OnCameraPreCull;
        Camera.onPostRender -= OnCameraPostRender;
        UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        UnityEngine.Rendering.RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;

        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
        }

        // Hapus UI Minimap agar tidak bocor ke scene Main Menu
        if (minimapUIObject != null)
        {
            Destroy(minimapUIObject);
        }
    }

    void Update()
    {
        if (playerTransform == null)
        {
            FindPlayer(); // Coba cari lagi jika hilang
            return;
        }

        if (minimapCamera != null)
        {
            // Ikuti pergerakan player (hanya X dan Z)
            Vector3 pos = playerTransform.position;
            minimapCamera.transform.position = new Vector3(pos.x, minimapHeight, pos.z);

            float pulseScale = 2.5f + Mathf.Sin(Time.time * 6f) * 1.2f; // Denyut ukuran dari 1.3 hingga 3.7

            // Update posisi semua indikator tepat di bawah kamera
            for (int i = 0; i < dynamicIndicators.Count; i++)
            {
                var data = dynamicIndicators[i];
                if (data.target != null && data.target.gameObject.activeInHierarchy)
                {
                    data.icon.position = new Vector3(data.target.position.x, minimapHeight - 10f, data.target.position.z);
                    
                    // Efek berkelap-kelip (denyut ukuran) khusus untuk bunga
                    if (data.isFlower)
                    {
                        data.icon.localScale = new Vector3(pulseScale, pulseScale, pulseScale);
                    }

                    data.icon.gameObject.SetActive(true);
                }
                else
                {
                    data.icon.gameObject.SetActive(false);
                }
            }
        }
    }

    private void FindPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            // Fallback cari by name
            FlowerCollectible[] fc = FindObjectsOfType<FlowerCollectible>();
            if (fc.Length > 0)
            {
                player = GameObject.Find("rapunzel_costume_basic");
            }
        }
        
        if (player != null)
        {
            playerTransform = player.transform;
        }
    }

    private void SetupCamera()
    {
        GameObject camObj = new GameObject("MinimapCamera");
        camObj.transform.SetParent(transform);
        
        minimapCamera = camObj.AddComponent<Camera>();
        minimapCamera.clearFlags = CameraClearFlags.SolidColor;
        minimapCamera.backgroundColor = new Color(0.1f, 0.2f, 0.15f, 1f); // Warna tanah default
        minimapCamera.orthographic = true;
        minimapCamera.orthographicSize = minimapOrthographicSize;
        minimapCamera.targetTexture = renderTexture;
        minimapCamera.depth = -1; // Render sebelum kamera utama atau biarkan bebas karena targetTexture dipakai
        
        // Rotasi menghadap ke bawah
        camObj.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }

    private void SetupUI()
    {
        // Cari MobileUI_Canvas secara spesifik terlebih dahulu
        GameObject canvasObj = GameObject.Find("MobileUI_Canvas");
        Canvas mainCanvas = null;
        
        if (canvasObj != null)
        {
            mainCanvas = canvasObj.GetComponent<Canvas>();
        }
        else
        {
            // Fallback cari Canvas apa saja (hati-hati dengan DontDestroyOnLoad canvas)
            mainCanvas = FindObjectOfType<Canvas>();
        }

        if (mainCanvas == null)
        {
            Debug.LogWarning("[MinimapSystem] Tidak menemukan Canvas untuk menaruh Minimap UI!");
            return;
        }

        // Buat Panel Background/Border
        GameObject bgObj = new GameObject("MinimapBorder");
        bgObj.transform.SetParent(mainCanvas.transform, false);
        bgObj.transform.SetAsFirstSibling(); // Taruh di urutan pertama agar dirender di belakang UI lain
        minimapUIObject = bgObj; // Simpan referensi untuk dihapus nanti
        
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(1f, 0.3f, 0.8f, 1f); // Ubah warna border menjadi Pink
        
        RectTransform bgRT = bgObj.GetComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0f, 1f);
        bgRT.anchorMax = new Vector2(0f, 1f);
        bgRT.pivot = new Vector2(0f, 1f);
        bgRT.anchoredPosition = new Vector2(25f, -25f); // Pojok kiri atas
        bgRT.sizeDelta = new Vector2(300f, 300f); // UI dibesarkan menjadi 300x300

        // Buat RawImage untuk texture
        GameObject rawObj = new GameObject("MinimapRawImage");
        rawObj.transform.SetParent(bgObj.transform, false);
        
        RawImage rawImg = rawObj.AddComponent<RawImage>();
        rawImg.texture = renderTexture;

        RectTransform rawRT = rawObj.GetComponent<RectTransform>();
        rawRT.anchorMin = Vector2.zero;
        rawRT.anchorMax = Vector2.one;
        rawRT.offsetMin = new Vector2(4f, 4f); // Padding untuk border
        rawRT.offsetMax = new Vector2(-4f, -4f);
    }

    private void SetupPlayerIndicator()
    {
        if (playerTransform == null) return;

        Material greenMat = new Material(Shader.Find("Unlit/Color"));
        greenMat.color = new Color(0f, 1f, 0f, 1f); // Hijau untuk player

        GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        indicator.name = "PlayerMinimapIcon";
        Destroy(indicator.GetComponent<Collider>());

        indicator.transform.SetParent(transform);
        indicator.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // Menghadap ke atas
        indicator.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f); // Ukuran kecil (lingkaran)

        Renderer rnd = indicator.GetComponent<Renderer>();
        rnd.sharedMaterial = greenMat;
        rnd.enabled = false;

        indicatorRenderers.Add(rnd);
        dynamicIndicators.Add(new IndicatorData { target = playerTransform, icon = indicator.transform, isFlower = false });
    }

    private void SetupFlowerIndicators()
    {
        // Material unlit warna pink menyala
        Material pinkMat = new Material(Shader.Find("Unlit/Color"));
        pinkMat.color = new Color(1f, 0.2f, 0.8f, 1f);

        FlowerCollectible[] flowers = FindObjectsOfType<FlowerCollectible>();
        foreach (var flower in flowers)
        {
            GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            indicator.name = "FlowerMinimapIcon";
            
            // Hapus collider bawaan Sphere
            Destroy(indicator.GetComponent<Collider>());
            
            indicator.transform.SetParent(transform); // Jadikan child dari sistem
            indicator.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // Menghadap ke atas
            indicator.transform.localScale = new Vector3(2.5f, 2.5f, 2.5f); // Ukuran dasar lebih besar

            Renderer rnd = indicator.GetComponent<Renderer>();
            rnd.sharedMaterial = pinkMat;
            rnd.enabled = false; // Matikan by default (hanya nyala saat minimap merender)

            indicatorRenderers.Add(rnd);
            dynamicIndicators.Add(new IndicatorData { target = flower.transform, icon = indicator.transform, isFlower = true });
        }
        
        Debug.Log($"[MinimapSystem] Berhasil membuat indikator untuk player dan {flowers.Length} bunga.");
    }

    // ────────────────────────────────────────────────────────
    //  Camera Hooks untuk menyembunyikan/menampilkan indikator
    // ────────────────────────────────────────────────────────

    private void OnCameraPreCull(Camera cam)
    {
        if (cam == minimapCamera) SetIndicatorsVisible(true);
    }

    private void OnCameraPostRender(Camera cam)
    {
        if (cam == minimapCamera) SetIndicatorsVisible(false);
    }

    // URP Fallbacks
    private void OnBeginCameraRendering(UnityEngine.Rendering.ScriptableRenderContext context, Camera cam)
    {
        if (cam == minimapCamera) SetIndicatorsVisible(true);
    }

    private void OnEndCameraRendering(UnityEngine.Rendering.ScriptableRenderContext context, Camera cam)
    {
        if (cam == minimapCamera) SetIndicatorsVisible(false);
    }

    private void SetIndicatorsVisible(bool visible)
    {
        for (int i = 0; i < indicatorRenderers.Count; i++)
        {
            if (indicatorRenderers[i] != null)
            {
                indicatorRenderers[i].enabled = visible;
            }
        }
    }
}
