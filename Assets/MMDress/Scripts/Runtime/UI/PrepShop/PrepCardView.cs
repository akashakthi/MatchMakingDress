// Assets/MMDress/Scripts/Runtime/UI/PrepShop/PrepCardView.cs
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
        [Header("UI (boleh kosong, akan di-auto-wire)")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text qtyText;
        [SerializeField] private Button plusButton;
        [SerializeField] private Button minusButton;

        [Header("Manual Mapping (optional)")]
        [SerializeField] private ItemSO manualItem;   // taruh ItemSO di Inspector kalau mau
        public ItemSO Item => manualItem;

        [Header("Debug")]
        [SerializeField] private bool logClicks = false;

        int _planned;
        public event Action<PrepCardView, int> OnDelta; // +1 / -1

        void Reset() => AutoWireChildren();

        void Awake()
        {
            AutoWireChildren();
            MakeNonBlocking(iconImage);          // biar klik nggak ketahan icon
            if (qtyText) qtyText.raycastTarget = false;

            // pasang listener aman (hapus dulu supaya nggak double kalau domain reload)
            if (plusButton)
            {
                plusButton.onClick.RemoveAllListeners();
                plusButton.onClick.AddListener(() =>
                {
                    if (logClicks) Debug.Log($"[PrepCardView] PLUS clicked ({GetItemLabel()})", this);
                    OnDelta?.Invoke(this, +1);
                });
            }

            if (minusButton)
            {
                minusButton.onClick.RemoveAllListeners();
                minusButton.onClick.AddListener(() =>
                {
                    if (_planned <= 0) return;
                    if (logClicks) Debug.Log($"[PrepCardView] MINUS clicked ({GetItemLabel()})", this);
                    OnDelta?.Invoke(this, -1);
                });
            }

            SetPlanned(0);
        }

        // --- API ---

        public void BindItem(ItemSO item, Sprite icon = null)
        {
            manualItem = item;
            if (icon != null) SetIcon(icon);
        }

        public void SetIcon(Sprite icon)
        {
            if (!iconImage) return;
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
            iconImage.type = Image.Type.Simple;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
        }

        public void SetPlanned(int qty)
        {
            _planned = Mathf.Max(0, qty);
            if (qtyText) qtyText.text = _planned.ToString();
            if (minusButton) minusButton.interactable = _planned > 0;
        }

        // --- helpers ---

        void AutoWireChildren()
        {
            // Cari icon & qty jika belum di-assign
            if (!iconImage) iconImage = GetComponentInChildren<Image>(true);
            if (!qtyText) qtyText = GetComponentInChildren<TMP_Text>(true);

            // Cari plus/minus berdasarkan nama child (mengandung "plus"/"minus")
            if (!plusButton || !minusButton)
            {
                var buttons = GetComponentsInChildren<Button>(true);
                foreach (var b in buttons)
                {
                    var nm = b.name.ToLowerInvariant();
                    if (!plusButton && nm.Contains("plus")) plusButton = b;
                    if (!minusButton && nm.Contains("minus")) minusButton = b;
                }
            }
        }

        static void MakeNonBlocking(Graphic g)
        {
            if (!g) return;
            g.raycastTarget = false;
        }

        string GetItemLabel()
        {
            if (Item != null)
                return string.IsNullOrEmpty(Item.displayName) ? Item.name : Item.displayName;
            return name;
        }
    }
}
