using UnityEngine;
using UnityEngine.SceneManagement;

namespace MMDress.Runtime.Systems
{
    /// <summary>
    /// Menangani tombol BACK bawaan Android (KeyCode.Escape).
    /// - Kalau ditekan di scene apa pun yang pakai script ini,
    ///   akan balik ke Main Menu.
    /// - Tidak aktif di Main Menu (biar tidak loop reload).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AndroidBackToMenu : MonoBehaviour
    {
        [Header("Target Scene")]
        [Tooltip("Nama scene Main Menu (sesuai di Build Settings).")]
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        [Header("Platform Filter")]
        [Tooltip("Kalau true, hanya jalan di Android. Di Editor masih bisa dipaksa test pakai flag di bawah.")]
        [SerializeField] private bool onlyOnAndroid = true;

        [Tooltip("Untuk debug di Editor: kalau true, Back (Esc) juga jalan di Editor Play Mode.")]
        [SerializeField] private bool enableInEditorPlay = true;

        private void Update()
        {
            // 1) Filter platform
            if (onlyOnAndroid)
            {
#if UNITY_ANDROID
                // ok, lanjut di Android build
#else
                // Bukan Android
                if (!(Application.isEditor && enableInEditorPlay))
                    return;
#endif
            }

            // 2) Cek tombol BACK (Escape di Unity)
            if (!Input.GetKeyDown(KeyCode.Escape))
                return;

            // 3) Dapatkan scene aktif sekarang
            var activeScene = SceneManager.GetActiveScene();
            string activeName = activeScene.name;

            // Kalau sudah di Main Menu, biarin (bisa nanti kamu ganti jadi Application.Quit kalau mau).
            if (!string.IsNullOrEmpty(mainMenuSceneName) &&
                activeName == mainMenuSceneName)
            {
                return;
            }

            // 4) Normalisasi timeScale (jaga-jaga kalau sebelumnya pause)
            Time.timeScale = 1f;

            // 5) Load Main Menu
            if (!string.IsNullOrEmpty(mainMenuSceneName))
            {
                SceneManager.LoadScene(mainMenuSceneName);
            }
            else
            {
                Debug.LogWarning("[AndroidBackToMenu] mainMenuSceneName belum diisi.");
            }
        }
    }
}
