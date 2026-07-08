using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Pasang script ini ke tombol Run di Canvas.
/// OnPointerDown  → mulai lari (hold)
/// OnPointerUp    → kembali jalan
/// </summary>
public class MobileRunButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Tooltip("Drag PlayerTPS (rapunzel) ke sini, atau biarkan kosong untuk auto-detect.")]
    [SerializeField] private PlayerTPS player;

    private void Start()
    {
        if (player == null)
        {
            var go = GameObject.FindWithTag("Player");
            if (go != null) player = go.GetComponent<PlayerTPS>();

            if (player == null)
                Debug.LogWarning("[MobileRunButton] PlayerTPS tidak ditemukan! " +
                                 "Drag rapunzel ke field Player di Inspector.");
        }
    }

    /// <summary>Jari menyentuh → mulai sprint</summary>
    public void OnPointerDown(PointerEventData eventData)
    {
        if (player != null) player.OnMobileRunStart();
    }

    /// <summary>Jari diangkat → kembali berjalan</summary>
    public void OnPointerUp(PointerEventData eventData)
    {
        if (player != null) player.OnMobileRunStop();
    }
}
