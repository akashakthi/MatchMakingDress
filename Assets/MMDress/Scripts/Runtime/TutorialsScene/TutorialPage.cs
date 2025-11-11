using UnityEngine;

[DisallowMultipleComponent]
public sealed class TutorialPage : MonoBehaviour
{
    [TextArea] public string text;               // opsional; kosong → fallback ke label
    [SerializeField] private TypewriterTMP typewriter;

    void Reset()
    {
        typewriter ??= GetComponentInChildren<TypewriterTMP>(true);
    }

    void Awake()
    {
        if (!typewriter) typewriter = GetComponentInChildren<TypewriterTMP>(true);
    }

    // Dipanggil saat halaman diaktifkan
    void OnEnable()
    {
        if (typewriter) typewriter.Begin(text);
    }

    public bool IsTyping() => typewriter && typewriter.IsRunning;
    public void SkipTypingIfRunning()
    {
        if (typewriter && typewriter.IsRunning) typewriter.Skip();
    }
}
