using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

/// <summary>
/// Mengontrol Portal01 — membuka portal setelah semua bunga dikumpulkan.
/// Menampilkan efek cahaya cyan berputar, popup "Portal Terbuka!",
/// dan meload scene berikutnya saat player masuk.
/// </summary>
[RequireComponent(typeof(Collider))]
public class PortalController : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  Inspector Settings
    // ─────────────────────────────────────────────
    [Header("Portal Settings")]
    [Tooltip("Nama scene level berikutnya (harus sudah ada di Build Settings)")]
    public string nextSceneName = "Level2";
    [Tooltip("Fallback: load scene by build index jika string kosong")]
    public int nextSceneIndex = 1;

    [Header("Trigger Zone")]
    [Tooltip("Radius area trigger player masuk portal")]
    public float portalTriggerRadius = 3.0f;

    [Header("Vortex Light Settings")]
    public Color vortexColor = new Color(0f, 1f, 0.95f, 1f); // Cyan
    public float lightIntensity = 3f;
    public float lightRange = 12f;
    public float rotationSpeed = 90f;   // derajat per detik
    public float pulseSpeed = 2f;
    public float pulseAmount = 0.4f;

    [Header("Load Settings")]
    [Tooltip("Delay sebelum level berikutnya di-load (detik)")]
    public float loadDelay = 2.5f;

    // ─────────────────────────────────────────────
    //  Private State
    // ─────────────────────────────────────────────
    private bool isOpen = false;
    private bool isLoading = false;

    // Visual objects
    private GameObject vortexRoot;
    private Light portalLight;
    private ParticleSystem vortexParticles;
    private GameObject vortexRingA;
    private GameObject vortexRingB;
    private GameObject vortexRingC;

    // Collider for trigger zone
    private SphereCollider triggerZone;

    // ─────────────────────────────────────────────
    //  Mono
    // ─────────────────────────────────────────────
    void Awake()
    {
        // Buat trigger sphere untuk mendeteksi player masuk portal
        triggerZone = gameObject.AddComponent<SphereCollider>();
        triggerZone.isTrigger = true;
        triggerZone.radius = portalTriggerRadius;
        triggerZone.center = new Vector3(0f, 1.5f, 0f); // sedikit ke atas dari pivot

        // Siapkan visual efek (masih disembunyikan)
        BuildVortexVisual();
        SetVortexActive(false);
    }

    void Update()
    {
        if (!isOpen) return;

        // Rotasi ring-ring vortex
        float dt = Time.deltaTime;
        if (vortexRingA != null) vortexRingA.transform.Rotate(0f, rotationSpeed * dt, 0f, Space.World);
        if (vortexRingB != null) vortexRingB.transform.Rotate(0f, -rotationSpeed * 0.7f * dt, 0f, Space.World);
        if (vortexRingC != null) vortexRingC.transform.Rotate(rotationSpeed * 0.5f * dt, 0f, 0f, Space.World);

        // Pulse cahaya
        if (portalLight != null)
        {
            float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
            portalLight.intensity = lightIntensity * pulse;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!isOpen || isLoading) return;
        if (other.CompareTag("Player") || other.name.Contains("rapunzel"))
        {
            StartCoroutine(LoadNextLevel());
        }
    }

    // ─────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────

    /// <summary>Dipanggil oleh FlowerCollectionManager ketika semua bunga terkumpul.</summary>
    public void OpenPortal()
    {
        if (isOpen) return;
        isOpen = true;
        SetVortexActive(true);
        StartCoroutine(OpenAnimation());
        Debug.Log("[PortalController] Portal01 terbuka!");
    }

    // ─────────────────────────────────────────────
    //  Internal
    // ─────────────────────────────────────────────

    void BuildVortexVisual()
    {
        // Root semua visual efek portal
        vortexRoot = new GameObject("VortexRoot");
        vortexRoot.transform.SetParent(transform, false);
        vortexRoot.transform.localPosition = new Vector3(0f, 3f, 0f); // Di tengah-tengah portal

        // ── Point Light ────────────────────────────
        GameObject lightGO = new GameObject("PortalLight");
        lightGO.transform.SetParent(vortexRoot.transform, false);
        portalLight = lightGO.AddComponent<Light>();
        portalLight.type = LightType.Point;
        portalLight.color = vortexColor;
        portalLight.intensity = lightIntensity;
        portalLight.range = lightRange;
        portalLight.shadows = LightShadows.None;

        // ── Ring A (horizontal besar) ───────────────
        vortexRingA = CreateRing("RingA", 2.0f, 0.25f, vortexColor, vortexRoot.transform, Vector3.zero);

        // ── Ring B (miring 45°) ─────────────────────
        vortexRingB = CreateRing("RingB", 1.5f, 0.18f, new Color(0.2f, 1f, 1f, 1f), vortexRoot.transform, new Vector3(45f, 0f, 0f));

        // ── Ring C (vertikal) ───────────────────────
        vortexRingC = CreateRing("RingC", 1.1f, 0.12f, new Color(0f, 0.8f, 1f, 1f), vortexRoot.transform, new Vector3(90f, 0f, 0f));

        // ── Particle System (partikel berputar) ─────
        BuildParticleSystem();
    }

    GameObject CreateRing(string name, float radius, float thickness, Color color, Transform parent, Vector3 eulerOffset)
    {
        GameObject ring = new GameObject(name);
        ring.transform.SetParent(parent, false);
        ring.transform.localEulerAngles = eulerOffset;

        int segments = 32;
        MeshFilter mf = ring.AddComponent<MeshFilter>();
        MeshRenderer mr = ring.AddComponent<MeshRenderer>();

        // Buat material emissive
        Material mat = new Material(Shader.Find("Standard"));
        mat.SetFloat("_Mode", 1f); // Transparent
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", color * 2.5f);
        mat.color = new Color(color.r, color.g, color.b, 0.85f);
        mr.material = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;

        // Buat mesh torus sederhana (poligon ring)
        Mesh mesh = new Mesh();
        int tubeSegs = 8;
        Vector3[] verts = new Vector3[segments * tubeSegs];
        int[] tris = new int[segments * tubeSegs * 6];
        Vector2[] uvs = new Vector2[verts.Length];

        for (int s = 0; s < segments; s++)
        {
            float angleS = (float)s / segments * Mathf.PI * 2f;
            Vector3 center = new Vector3(Mathf.Cos(angleS) * radius, Mathf.Sin(angleS) * radius, 0f);
            for (int t = 0; t < tubeSegs; t++)
            {
                float angleT = (float)t / tubeSegs * Mathf.PI * 2f;
                Vector3 tube = center + new Vector3(
                    Mathf.Cos(angleT) * thickness * Mathf.Cos(angleS),
                    Mathf.Cos(angleT) * thickness * Mathf.Sin(angleS),
                    Mathf.Sin(angleT) * thickness
                );
                verts[s * tubeSegs + t] = tube;
                uvs[s * tubeSegs + t] = new Vector2((float)s / segments, (float)t / tubeSegs);
            }
        }
        // Triangles
        int idx = 0;
        for (int s = 0; s < segments; s++)
        {
            for (int t = 0; t < tubeSegs; t++)
            {
                int curr = s * tubeSegs + t;
                int next = ((s + 1) % segments) * tubeSegs + t;
                int currN = s * tubeSegs + (t + 1) % tubeSegs;
                int nextN = ((s + 1) % segments) * tubeSegs + (t + 1) % tubeSegs;
                tris[idx++] = curr; tris[idx++] = next; tris[idx++] = currN;
                tris[idx++] = next; tris[idx++] = nextN; tris[idx++] = currN;
            }
        }
        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        mf.mesh = mesh;

        return ring;
    }

    void BuildParticleSystem()
    {
        GameObject psGO = new GameObject("VortexParticles");
        psGO.transform.SetParent(vortexRoot.transform, false);
        psGO.transform.localPosition = Vector3.zero;

        vortexParticles = psGO.AddComponent<ParticleSystem>();
        var main = vortexParticles.main;
        main.loop = true;
        main.startLifetime = 1.8f;
        main.startSpeed = 0f;
        main.startSize = 0.18f;
        main.maxParticles = 150;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0f, 1f, 1f, 0.9f),
            new Color(0.3f, 1f, 0.8f, 0.6f)
        );

        var emission = vortexParticles.emission;
        emission.rateOverTime = 60f;

        var shape = vortexParticles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 1.8f;

        var vel = vortexParticles.velocityOverLifetime;
        vel.enabled = true;
        vel.orbitalY = new ParticleSystem.MinMaxCurve(3f, 5f);
        vel.radial = new ParticleSystem.MinMaxCurve(-0.5f, -1.5f);

        var col = vortexParticles.colorOverLifetime;
        col.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(0f,1f,0.95f), 0f),
                new GradientColorKey(new Color(0.2f,1f,1f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.15f),
                new GradientAlphaKey(1f, 0.8f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        col.color = new ParticleSystem.MinMaxGradient(grad);

        var size = vortexParticles.sizeOverLifetime;
        size.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.2f, 1f),
            new Keyframe(0.8f, 0.8f),
            new Keyframe(1f, 0f)
        );
        size.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        var psRenderer = psGO.GetComponent<ParticleSystemRenderer>();
        psRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        psRenderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
        psRenderer.material.color = new Color(0f, 1f, 0.95f, 1f);
    }

    void SetVortexActive(bool active)
    {
        if (vortexRoot != null) vortexRoot.SetActive(active);
    }

    IEnumerator OpenAnimation()
    {
        // Fade in cahaya dari 0
        if (portalLight != null)
        {
            portalLight.intensity = 0f;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * 0.8f;
                portalLight.intensity = Mathf.Lerp(0f, lightIntensity, t);
                yield return null;
            }
        }
    }

    IEnumerator LoadNextLevel()
    {
        if (isLoading) yield break;
        isLoading = true;

        Debug.Log("[PortalController] Player masuk portal! Memuat level berikutnya...");

        // Beritahu FlowerCollectionManager untuk tampilkan popup loading
        if (FlowerCollectionManager.Instance != null)
            FlowerCollectionManager.Instance.ShowPortalEnterPopup();

        yield return new WaitForSeconds(loadDelay);

        // Load scene
        if (!string.IsNullOrEmpty(nextSceneName))
            SceneManager.LoadScene(nextSceneName);
        else
            SceneManager.LoadScene(nextSceneIndex);
    }

    // ─────────────────────────────────────────────
    //  Gizmos
    // ─────────────────────────────────────────────
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0.95f, 0.3f);
        Gizmos.DrawSphere(transform.position + Vector3.up * 1.5f, portalTriggerRadius);
        Gizmos.color = new Color(0f, 1f, 0.95f, 1f);
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 1.5f, portalTriggerRadius);
    }
}
