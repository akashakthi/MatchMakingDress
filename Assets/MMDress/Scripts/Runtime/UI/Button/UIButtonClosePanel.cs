// Assets/MMDress/Scripts/Runtime/UI/Button/UIButtonClosePanel.cs
using UnityEngine;
using UnityEngine.UI;
using MMDress.UI.Animations;

[RequireComponent(typeof(Button))]
public sealed class UIButtonClosePanel : MonoBehaviour
{
    [SerializeField] UIPanelZoomAnimator panel;
    void Reset() => GetComponent<Button>().onClick.AddListener(() => panel?.Hide());
    void Awake() => GetComponent<Button>().onClick.AddListener(() => panel?.Hide());
}
