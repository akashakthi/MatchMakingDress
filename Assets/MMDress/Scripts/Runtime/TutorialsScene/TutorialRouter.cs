using UnityEngine;
using UnityEngine.SceneManagement;

public static class TutorialRouter
{
    private const string Key = "TutorialSeen";
    public static string TutorialSceneName = "Tutorial";
    public static string GameplaySceneName = "Gameplay"; // isi dari Inspector via static setter kalau mau

    public static void StartGame()
    {
        if (PlayerPrefs.GetInt(Key, 0) == 0)
            SceneManager.LoadScene(TutorialSceneName);
        else
            SceneManager.LoadScene(GameplaySceneName);
    }

    public static void MarkSeen()
    {
        PlayerPrefs.SetInt(Key, 1);
        PlayerPrefs.Save();
    }
}
