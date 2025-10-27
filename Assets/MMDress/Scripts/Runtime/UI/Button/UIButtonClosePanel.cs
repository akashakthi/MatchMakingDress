// Assets/MMDress/Scripts/Runtime/UI/Button/UIButtonClosePanel.cs
using UnityEngine;
using UnityEngine.UI;
using MMDress.UI.Animations;
using MMDress.UI.Credit;

[RequireComponent(typeof(Button))]
public sealed class UIButtonClosePanel : MonoBehaviour
{
    [SerializeField] UIPanelZoomAnimator panel;
    [Header("Opsional: reset writer saat panel ditutup")]
    [SerializeField] CreditTypewriter creditWriter;

    void Awake()
    {
        var btn = GetComponent<Button>();
        btn.onClick.RemoveAllListeners();          // hindari double add
        btn.onClick.AddListener(() =>
        {
            creditWriter?.ResetForReplay();        // siapkan supaya nanti buka -> ketik dari awal
            panel?.Hide();
        });
    }
}
