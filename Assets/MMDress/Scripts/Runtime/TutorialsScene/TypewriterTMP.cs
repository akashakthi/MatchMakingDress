using UnityEngine;
using TMPro;
using System.Collections;

[DisallowMultipleComponent]
public sealed class TypewriterTMP : MonoBehaviour
{
    [SerializeField] private TMP_Text label;
    [SerializeField, Min(0.1f)] private float durationPerPage = 5f;

    private string _full;
    private Coroutine _co;
    private bool _isRunning;
    public bool IsRunning => _isRunning;

    void Awake()
    {
        if (!label) label = GetComponent<TMP_Text>();
    }

    /// Mulai animasi; jika 'text' kosong → fallback ke isi label saat ini.
    public void Begin(string text = null)
    {
        _full = string.IsNullOrWhiteSpace(text) ? (label ? label.text : string.Empty) : text;
        if (_co != null) StopCoroutine(_co);
        _co = StartCoroutine(CoType());
    }

    /// Percepat/akhiri animasi ketik → langsung tampilkan full text.
    public void Skip()
    {
        if (!_isRunning) return;
        if (_co != null) StopCoroutine(_co);
        if (label) label.text = _full;
        _isRunning = false;
    }

    private IEnumerator CoType()
    {
        _isRunning = true;

        if (!label || string.IsNullOrEmpty(_full))
        {
            if (label) label.text = _full;
            _isRunning = false;
            yield break;
        }

        label.text = string.Empty;

        int total = _full.Length;
        float cps = total / durationPerPage; // karakter per detik
        float shown = 0f;

        while (shown < total)
        {
            shown += cps * Time.unscaledDeltaTime; // <-- ini yang benar
            int c = Mathf.Clamp(Mathf.FloorToInt(shown), 0, total);
            label.text = _full.Substring(0, c);
            yield return null;
        }

        label.text = _full;
        _isRunning = false;
    }
}
