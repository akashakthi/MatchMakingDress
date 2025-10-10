// Assets/MMDress/Scripts/Runtime/UI/Button/UIButtonPageNext.cs
using UnityEngine;
using UnityEngine.UI;
using MMDress.UI.Animations;

[RequireComponent(typeof(Button))]
public sealed class UIButtonPageNext : MonoBehaviour
{
    [SerializeField] private UIPageZoomFader fader;

    void Reset() { GetComponent<Button>().onClick.AddListener(Invoke); }
    void Awake() { GetComponent<Button>().onClick.AddListener(Invoke); }

    public void Invoke()
    {
        if (!fader) return;
        fader.Next();
    }
}
