#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Menu kecil tambahan untuk operasi cepat yang tidak duplikat dengan Dev Hub.
/// </summary>
public static class MMDressToolsMenu
{
    // Tidak ada menu "Dev Hub" di sini agar tidak bentrok.
    // Dev Hub sudah disediakan oleh MMDressDevHubWindow (Tools/MMDress/Dev Hub).

    [MenuItem("Tools/MMDress/Reset PlayerPrefs", false, 50)]
    public static void ResetPlayerPrefsAll()
    {
        if (EditorUtility.DisplayDialog("Reset PlayerPrefs",
            "Yakin menghapus SEMUA PlayerPrefs untuk project ini? (tidak bisa di-undo)",
            "Reset", "Batal"))
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            Debug.Log("[MMDress] PlayerPrefs di-reset.");
        }
    }

    // (Opsional) Shortcut cepat membuka Dev Hub tab Generate Items
    [MenuItem("Tools/MMDress/Generate Items…", false, 10)]
    public static void OpenDevHubGenerateItems()
    {
        var w = MMDressDevHubWindow.OpenWindow();
        w.SetTabGenerateItems();
    }
}
#endif
