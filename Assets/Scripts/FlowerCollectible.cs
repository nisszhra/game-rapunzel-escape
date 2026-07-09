using UnityEngine;

/// <summary>
/// Ditempelkan ke setiap plant_flower_crystalbud.
/// Mendeteksi ketika player (rapunzel_costume_basic) melewatinya via trigger,
/// lalu bunga "diambil" (dinonaktifkan) dan melaporkan ke FlowerCollectionManager.
/// </summary>
[RequireComponent(typeof(SphereCollider))]
public class FlowerCollectible : MonoBehaviour
{
    [Header("Collectible Settings")]
    [Tooltip("Tag player yang dapat mengambil bunga ini")]
    public string playerTag = "Player";

    [Tooltip("Radius trigger untuk mendeteksi player")]
    public float collectRadius = 1.2f;

    [Header("Visual Feedback")]
    [Tooltip("Aktifkan efek rotasi/hover pada bunga")]
    public bool enableHoverEffect = true;
    public float rotationSpeed = 60f;
    public float bobSpeed = 1.5f;
    public float bobHeight = 0.15f;

    private SphereCollider triggerCollider;
    private Light glowLight;
    private bool isCollected = false;
    private Vector3 startPosition;

    void Awake()
    {
        // Setup SphereCollider sebagai trigger
        triggerCollider = GetComponent<SphereCollider>();
        triggerCollider.isTrigger = true;
        triggerCollider.radius = collectRadius;
        
        // Pastikan ada Rigidbody (Kinematic) agar OnTriggerEnter selalu terpanggil
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        // Tambahkan efek cahaya (Point Light)
        glowLight = gameObject.AddComponent<Light>();
        glowLight.type = LightType.Point;
        glowLight.color = new Color(0.2f, 1f, 0.95f, 1f); // Cyan muda bercahaya
        glowLight.intensity = 3f;
        glowLight.range = 4f;

        startPosition = transform.position;
    }

    void Update()
    {
        if (isCollected) return;

        // Efek hover: rotasi + naik-turun
        if (enableHoverEffect)
        {
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
            float newY = startPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (isCollected) return;

        // Cek apakah yang masuk adalah player (case-insensitive)
        if (other.CompareTag(playerTag) || other.name.ToLower().Contains("rapunzel") || other.name.ToLower().Contains("player"))
        {
            Collect();
        }
    }

    void Collect()
    {
        isCollected = true;

        // Lapor ke manager
        if (FlowerCollectionManager.Instance != null)
        {
            FlowerCollectionManager.Instance.OnFlowerCollected(gameObject.name);
        }
        else
        {
            Debug.LogWarning("[FlowerCollectible] FlowerCollectionManager tidak ditemukan di scene!");
        }

        // Nonaktifkan bunga (efek diambil)
        gameObject.SetActive(false);
    }

    // Visualisasi radius trigger di Scene View
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.4f, 1f, 0.4f);
        Gizmos.DrawSphere(transform.position, collectRadius);
        Gizmos.color = new Color(1f, 0.4f, 1f, 1f);
        Gizmos.DrawWireSphere(transform.position, collectRadius);
    }
}
