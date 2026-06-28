using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Otomatis me-register semua onClick button di Main Menu saat scene di-load.
/// Ini diperlukan karena listener yang di-assign via kode tidak tersimpan di .unity file
/// ketika runtime berhenti. Script ini memastikan tombol selalu berfungsi.
///
/// SETUP: Attach ke GameObject "LevelSelectionManager" di scene Main Menu.
/// LevelSelectionManager component harus ada di GameObject yang sama.
/// </summary>
[RequireComponent(typeof(LevelSelectionManager))]
public class MainMenuWirer : MonoBehaviour
{
    private void Awake()
    {
        var mgr = GetComponent<LevelSelectionManager>();
        if (mgr == null)
        {
            Debug.LogError("[MainMenuWirer] LevelSelectionManager not found on this GameObject!");
            return;
        }

        // ── PLAY Button ──────────────────────────────────────────
        var playBtn = GameObject.Find("playbtn");
        if (playBtn != null)
        {
            var btn = playBtn.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(mgr.OpenMapPanel);
                Debug.Log("[MainMenuWirer] PLAY button wired.");
            }
        }
        else
        {
            Debug.LogWarning("[MainMenuWirer] 'playbtn' tidak ditemukan di scene. Pastikan nama button adalah 'playbtn'.");
        }

        // ── Map Panel Buttons ────────────────────────────────────
        if (mgr.mapPanel != null)
        {
            var mapT = mgr.mapPanel.transform;

            // Level Node 1
            var node1T = mapT.Find("LevelNode_1");
            if (node1T != null)
            {
                var node1Btn = node1T.GetComponent<Button>();
                if (node1Btn != null)
                {
                    node1Btn.onClick.RemoveAllListeners();
                    node1Btn.onClick.AddListener(() => mgr.OnLevelNodeClicked(0));
                }
            }

            // Level Node 2
            var node2T = mapT.Find("LevelNode_2");
            if (node2T != null)
            {
                var node2Btn = node2T.GetComponent<Button>();
                if (node2Btn != null)
                {
                    node2Btn.onClick.RemoveAllListeners();
                    node2Btn.onClick.AddListener(() => mgr.OnLevelNodeClicked(1));
                }
            }

            // Close Button
            var closeT = mapT.Find("CloseButton");
            if (closeT != null)
            {
                var closeBtn = closeT.GetComponent<Button>();
                if (closeBtn != null)
                {
                    closeBtn.onClick.RemoveAllListeners();
                    closeBtn.onClick.AddListener(mgr.CloseMapPanel);
                }
            }

            Debug.Log("[MainMenuWirer] Map panel buttons wired.");
        }
        else
        {
            Debug.LogWarning("[MainMenuWirer] mapPanel tidak di-assign di LevelSelectionManager!");
        }
    }
}
