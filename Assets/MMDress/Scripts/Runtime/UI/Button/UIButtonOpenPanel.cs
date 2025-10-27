// Assets/MMDress/Scripts/Runtime/UI/Button/UIButtonOpenPanel.cs
using UnityEngine;
using UnityEngine.UI;
using MMDress.UI.Animations;
using MMDress.UI.Credit;   // <-- butuh untuk CreditTypewriter

[RequireComponent(typeof(Button))]
public sealed class UIButtonOpenPanel : MonoBehaviour
{
    [Header("Target Panel")]
    [SerializeField] private UIPanelZoomAnimator panel;

    [Header("Behaviour")]
    [SerializeField] private bool toggle;              // true -> Toggle(), false -> Show()

    [Header("Credit Writer (opsional)")]
    [Tooltip("Isi hanya untuk tombol yang membuka Credit Panel. Kosongkan untuk tombol lain.")]
    [SerializeField] private CreditTypewriter creditWriter;
    [Tooltip("Kalau diisi, animasi diketik ulang saat panel tampil.")]
    [SerializeField] private bool replayWriterOnShow = true;
    [Tooltip("Delay kecil agar layout/alpha siap sebelum mulai mengetik.")]
    [SerializeField, Min(0f)] private float playDelay = 0.05f;

    void Reset()
    {
        var btn = GetComponent<Button>();
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(Invoke);
    }

    void Awake()
    {
        var btn = GetComponent<Button>();
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(Invoke);
    }

    public void Invoke()
    {
        if (!panel) return;

        if (toggle) panel.Toggle();
        else panel.Show();

        // Khusus tombol Credit: replay writer saat panel dibuka.
        if (creditWriter && replayWriterOnShow)
            StartCoroutine(PlayWriterAfterDelay());
    }

    private System.Collections.IEnumerator PlayWriterAfterDelay()
    {
        // Siapkan dari awal untuk menghindari state sisa
        creditWriter.ResetForReplay();

        // Tunggu 1 frame + delay kecil agar panel aktif & CanvasGroup sudah berubah
        if (playDelay <= 0f) yield return null;
        else yield return new WaitForSeconds(playDelay);

        creditWriter.PlayFromStart();
    }
}
