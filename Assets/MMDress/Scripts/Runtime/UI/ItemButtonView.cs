using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MMDress.Data;

namespace MMDress.UI
{
    /// Kartu item pada list horizontal: stok 0 tetap bisa preview (hanya gelap).
    [DisallowMultipleComponent]
    public sealed class ItemButtonView : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private Button button;       // WAJIB: Button di root prefab
        [SerializeField] private Image icon;         // child image untuk sprite item
        [SerializeField] private TMP_Text label;      // opsional
        [SerializeField] private TMP_Text stockText;  // angka stok (opsional)

        [Header("Visual States")]
        [SerializeField, Range(0.2f, 1f)] private float enabledAlpha = 1f;
        [SerializeField, Range(0.1f, 0.9f)] private float disabledAlpha = 0.45f;

        private ItemSO _data;
        private int _stock = 0;
        public ItemSO Data => _data;

        public event Action<ItemSO> Clicked;

        private void Reset()
        {
            if (!button) button = GetComponent<Button>();
            if (!icon) icon = transform.GetComponentInChildren<Image>(true);
        }

        private void Awake()
        {
            if (!button) button = GetComponent<Button>();

            if (button)
            {
                // Hindari dobel listener jika prefab pernah diisi lewat Inspector
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() =>
                {
                    if (_data != null)
                        Clicked?.Invoke(_data); // stok 0 tetap boleh preview
                });
            }

            // Pastikan child grafis tidak memblokir klik ke Button induk
            if (icon) icon.raycastTarget = false;
            if (label) label.raycastTarget = false;
            if (stockText) stockText.raycastTarget = false;
        }

        public void Bind(ItemSO data)
        {
            _data = data;

            if (icon)
            {
                icon.sprite = data ? data.sprite : null;
                icon.enabled = icon.sprite != null;
                icon.type = Image.Type.Simple;
                // Tidak perlu SetNativeSize kalau ingin seragam
            }

            if (label) label.text = data ? data.displayName : "-";

            ApplyStockVisual(_stock);
        }

        public void BindStock(int stock)
        {
            _stock = Mathf.Max(0, stock);
            ApplyStockVisual(_stock);
        }

        private void ApplyStockVisual(int stock)
        {
            if (stockText) stockText.text = stock.ToString();

            float a = (stock > 0) ? enabledAlpha : disabledAlpha;

            if (icon) { var c = icon.color; c.a = a; icon.color = c; }
            if (label) { var c = label.color; c.a = a; label.color = c; }
            if (stockText) { var c = stockText.color; c.a = a; stockText.color = c; }

            // Penting: JANGAN menonaktifkan button.interactable (preview tetap boleh)
        }

        public void SetSelected(bool on)
        {
            if (!icon) return;
            var c = icon.color;
            c.a = on ? 1f : enabledAlpha;
            icon.color = c;
        }
    }
}
