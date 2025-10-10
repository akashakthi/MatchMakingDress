// Assets/MMDress/Scripts/Runtime/UI/Button/UIButtonOpenPanel.cs
using UnityEngine;
using UnityEngine.UI;
using MMDress.UI.Animations;

[RequireComponent(typeof(Button))]
public sealed class UIButtonOpenPanel : MonoBehaviour
{
    [SerializeField] UIPanelZoomAnimator panel;
    [SerializeField] bool toggle; // kalau true -> Toggle(), kalau false -> Show()

    void Reset() => GetComponent<Button>().onClick.AddListener(Invoke);
    void Awake() => GetComponent<Button>().onClick.AddListener(Invoke);

    public void Invoke()
    {
        if (!panel) return;
        if (toggle) panel.Toggle();
        else panel.Show();
    }
}
