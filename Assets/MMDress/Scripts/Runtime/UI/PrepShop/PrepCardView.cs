using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MMDress.Data;

namespace MMDress.Runtime.UI.PrepShop
{
    [DisallowMultipleComponent]
    public sealed class PrepCardView : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] Image iconImage;
        [SerializeField] TMP_Text qtyText;
        [SerializeField] Button plusButton;
        [SerializeField] Button minusButton;

        [Header("Manual Mapping (optional)")]
        [SerializeField] ItemSO manualItem;              // <- taruh ItemSO-mu di sini lewat Inspector
        public ItemSO Item => manualItem;

        int _planned;
        public event Action<PrepCardView, int> OnDelta;   // +1 / -1

        void Awake()
        {
            if (plusButton) plusButton.onClick.AddListener(() => OnDelta?.Invoke(this, +1));
            if (minusButton) minusButton.onClick.AddListener(() => { if (_planned > 0) OnDelta?.Invoke(this, -1); });
            SetPlanned(0);
        }

        public void SetIcon(Sprite icon)
        {
            if (!iconImage) return;
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
            iconImage.type = Image.Type.Simple;
            iconImage.preserveAspect = true;
        }

        public void SetPlanned(int qty)
        {
            _planned = Mathf.Max(0, qty);
            if (qtyText) qtyText.text = _planned.ToString();
            if (minusButton) minusButton.interactable = _planned > 0;
        }
    }
}
