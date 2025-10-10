// Assets/MMDress/Scripts/Runtime/UI/Button/UIButtonPagePrev.cs
using UnityEngine;
using UnityEngine.UI;
using MMDress.UI.Animations;

[RequireComponent(typeof(Button))]
public sealed class UIButtonPagePrev : MonoBehaviour
{
    [SerializeField] private UIPageZoomFader fader;

    void Reset() { GetComponent<Button>().onClick.AddListener(Invoke); }
    void Awake() { GetComponent<Button>().onClick.AddListener(Invoke); }

    public void Invoke()
    {
        if (!fader) return;
        fader.Prev();
    }
}
