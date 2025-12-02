using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class GameExitManager : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Waktu tunggu sebelum aplikasi benar-benar tutup (agar suara klik terdengar dulu).")]
    [SerializeField] private float delayBeforeQuit = 0.4f;

    private Button _exitButton;

    private void Awake()
    {
        // Otomatis cari tombol di object ini
        _exitButton = GetComponent<Button>();

        // Jika script ini ditempel di tombol, otomatis tambahkan listener
        if (_exitButton != null)
        {
            _exitButton.onClick.AddListener(QuitGame);
        }
    }

    // Fungsi ini bisa dipanggil via Inspector (OnClick) atau otomatis via script di atas
    public void QuitGame()
    {
        StartCoroutine(QuitSequence());
    }

    private IEnumerator QuitSequence()
    {
        // 1. Tunggu sebentar (agar animasi/suara klik selesai)
        // Pakai Realtime supaya tetap jalan meski game sedang di-Pause (Time.timeScale = 0)
        yield return new WaitForSecondsRealtime(delayBeforeQuit);

        // 2. Logika Keluar
#if UNITY_EDITOR
        // Jika sedang di Unity Editor, stop play mode
        Debug.Log("Aplikasi minta Quit (Hanya bekerja di Build, di Editor cuma stop play).");
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // Jika sudah di-build (Android/PC/iOS), tutup aplikasi
        Application.Quit();
#endif
    }
}