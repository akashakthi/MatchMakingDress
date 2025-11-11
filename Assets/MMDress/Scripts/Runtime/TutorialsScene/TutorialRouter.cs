using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Menangani routing dari Main Menu ke TutorialScene atau GameplayScene.
/// </summary>
public static class TutorialRouter
{
    private const string Key = "TutorialSeen";
    private const int DefaultValue = 0;

    // isi sesuai nama scene di Build Settings
    public static string TutorialSceneName = "Tutorial";
    public static string GameplaySceneName = "SampleScene";

    /// <summary>
    /// Dipanggil oleh tombol "Play".
    /// </summary>
    public static void StartGame()
    {
        int seen = PlayerPrefs.GetInt(Key, DefaultValue);

        if (seen == 0)
        {
            // pertama kali main → jalankan tutorial
            SceneManager.LoadScene(TutorialSceneName);
        }
        else
        {
            // sudah pernah → langsung gameplay
            SceneManager.LoadScene(GameplaySceneName);
        }
    }

    /// <summary>
    /// Tandai tutorial sudah pernah dilihat (dipanggil dari TutorialManager).
    /// </summary>
    public static void MarkSeen()
    {
        PlayerPrefs.SetInt(Key, 1);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// (Opsional) reset status tutorial, buat testing di editor.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    private static void DevReset()
    {
#if UNITY_EDITOR
        // aktifin ini kalau mau ngetes ulang tutorial tiap play di editor
        // PlayerPrefs.DeleteKey(Key);
#endif
    }
}
