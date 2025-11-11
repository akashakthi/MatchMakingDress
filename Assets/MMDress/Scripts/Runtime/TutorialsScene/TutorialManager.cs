using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class TutorialManager : MonoBehaviour, IPointerClickHandler
{
    [Header("Pages (urut sesuai sequence)")]
    [SerializeField] private TutorialPage[] pages;

    [Header("Routing")]
    [SerializeField] private string gameplaySceneName = "Gameplay";

    [Header("UX")]
    [SerializeField, Min(0f)] private float tapCooldown = 0.18f;

    private int _current;
    private float _nextTapAllowedAt;
    private bool _switching;

    void Start()
    {
        // Matikan semua, hidupkan index 0
        for (int i = 0; i < pages.Length; i++)
            if (pages[i]) pages[i].gameObject.SetActive(false);

        if (pages.Length > 0)
        {
            _current = 0;
            pages[_current].gameObject.SetActive(true);
        }
    }

    public void OnPointerClick(PointerEventData _)
    {
        if (_switching || Time.unscaledTime < _nextTapAllowedAt) return;
        _nextTapAllowedAt = Time.unscaledTime + tapCooldown;

        var p = pages[_current];
        // Tap 1: percepat ketik
        if (p.IsTyping())
        {
            p.SkipTypingIfRunning();
            return;
        }

        // Tap 2: next page
        GoNext();
    }

    private void GoNext()
    {
        _switching = true;

        pages[_current].gameObject.SetActive(false);
        _current++;

        if (_current >= pages.Length)
        {
            TutorialRouter.MarkSeen();
            SceneManager.LoadScene(gameplaySceneName);
            return;
        }

        pages[_current].gameObject.SetActive(true);
        _switching = false;
    }
}
