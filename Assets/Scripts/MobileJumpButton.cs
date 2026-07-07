using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Pasang script ini ke GameObject tombol Jump di Canvas.
/// Gunakan IPointerDownHandler agar jump langsung terpicu saat
/// jari menyentuh tombol — lebih responsif daripada Button.onClick
/// yang baru fire saat PointerUp.
/// </summary>
public class MobileJumpButton : MonoBehaviour, IPointerDownHandler
{
    [Tooltip("Drag PlayerTPS (rapunzel_costume_basic) ke sini.")]
    [SerializeField] private PlayerTPS player;

    private void Start()
    {
        // Auto-cari player jika belum di-assign di Inspector
        if (player == null)
        {
            var playerGO = GameObject.FindWithTag("Player");
            if (playerGO != null)
                player = playerGO.GetComponent<PlayerTPS>();

            if (player == null)
                Debug.LogWarning("[MobileJumpButton] PlayerTPS tidak ditemukan! " +
                                 "Drag rapunzel_costume_basic ke field Player di Inspector.");
        }
    }

    /// <summary>
    /// Dipanggil LANGSUNG saat jari menyentuh tombol (PointerDown).
    /// </summary>
    public void OnPointerDown(PointerEventData eventData)
    {
        if (player != null)
            player.OnMobileJumpPressed();
    }
}
