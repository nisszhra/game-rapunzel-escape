#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Editor utility untuk men-wire semua tombol di Main Menu scene secara persistent
/// (tersimpan di scene file, tidak hilang saat Play berhenti).
///
/// CARA PAKAI:
///   Di Unity menu bar: Tools -> Wire Main Menu Buttons
///   Pastikan scene Main Menu sedang terbuka.
/// </summary>
public class MainMenuButtonWirer : EditorWindow
{
    [MenuItem("Tools/Wire Main Menu Buttons")]
    public static void WireAllButtons()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null) { Debug.LogError("[Wire] Canvas not found!"); return; }

        var mgrGO = GameObject.Find("LevelSelectionManager");
        if (mgrGO == null) { Debug.LogError("[Wire] LevelSelectionManager not found!"); return; }
        var mgr = mgrGO.GetComponent<LevelSelectionManager>();
        if (mgr == null) { Debug.LogError("[Wire] LevelSelectionManager component not found!"); return; }

        // ── PLAY Button ──────────────────────────────────────────────
        FixPlayButton(canvas, mgr);

        // ── Map Panel Buttons ────────────────────────────────────────
        var mapPanel = canvas.transform.Find("MapPanel");
        if (mapPanel == null) { Debug.LogError("[Wire] MapPanel not found!"); return; }

        FixNodeButton(mapPanel, "LevelNode_1", mgr, 0);
        FixNodeButton(mapPanel, "LevelNode_2", mgr, 1);
        FixCloseButton(mapPanel, mgr);

        // Save the scene
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log("[Wire] ✅ All Main Menu buttons wired successfully! Save the scene (Ctrl+S).");
    }

    // ─────────────────────────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────────────────────────

    static void FixPlayButton(GameObject canvas, LevelSelectionManager mgr)
    {
        var playbtnT = canvas.transform.Find("playbtn");
        if (playbtnT == null) { Debug.LogWarning("[Wire] 'playbtn' not found!"); return; }
        var btn = playbtnT.GetComponent<Button>();
        if (btn == null) return;

        // Remove all persistent listeners
        ClearPersistentListeners(btn);

        // Add OpenMapPanel as persistent listener
        UnityEventTools.AddVoidPersistentListener(btn.onClick, mgr.OpenMapPanel);
        EditorUtility.SetDirty(btn);
        Debug.Log("[Wire] playbtn -> OpenMapPanel ✅");
    }

    static void FixNodeButton(Transform mapPanel, string nodeName, LevelSelectionManager mgr, int levelIndex)
    {
        var nodeT = mapPanel.Find(nodeName);
        if (nodeT == null) { Debug.LogWarning($"[Wire] {nodeName} not found!"); return; }
        var btn = nodeT.GetComponent<Button>();
        if (btn == null) return;

        ClearPersistentListeners(btn);

        // Wire: OnLevelNodeClicked(int) — use SerializedObject to pass int arg
        var so = new SerializedObject(btn);
        var calls = so.FindProperty("m_OnClick.m_PersistentCalls.m_Calls");
        calls.InsertArrayElementAtIndex(0);
        var c = calls.GetArrayElementAtIndex(0);
        c.FindPropertyRelative("m_Target").objectReferenceValue = mgr;
        c.FindPropertyRelative("m_TargetAssemblyTypeName").stringValue = mgr.GetType().AssemblyQualifiedName;
        c.FindPropertyRelative("m_MethodName").stringValue = "OnLevelNodeClicked";
        c.FindPropertyRelative("m_Mode").enumValueIndex = 4; // Int
        c.FindPropertyRelative("m_Arguments.m_IntArgument").intValue = levelIndex;
        c.FindPropertyRelative("m_CallState").enumValueIndex = 2; // RuntimeOnly
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(btn);
        Debug.Log($"[Wire] {nodeName} -> OnLevelNodeClicked({levelIndex}) ✅");
    }

    static void FixCloseButton(Transform mapPanel, LevelSelectionManager mgr)
    {
        var closeT = mapPanel.Find("CloseButton");
        if (closeT == null) { Debug.LogWarning("[Wire] CloseButton not found!"); return; }
        var btn = closeT.GetComponent<Button>();
        if (btn == null) return;

        ClearPersistentListeners(btn);
        UnityEventTools.AddVoidPersistentListener(btn.onClick, mgr.CloseMapPanel);
        EditorUtility.SetDirty(btn);
        Debug.Log("[Wire] CloseButton -> CloseMapPanel ✅");
    }

    /// <summary>Removes all persistent listeners from a button's onClick.</summary>
    static void ClearPersistentListeners(Button btn)
    {
        var so = new SerializedObject(btn);
        var calls = so.FindProperty("m_OnClick.m_PersistentCalls.m_Calls");
        calls.ClearArray();
        so.ApplyModifiedProperties();
    }
}
#endif
