using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MMDress.Data;

namespace MMDress.UI
{
    [DisallowMultipleComponent]
    public sealed class ItemButtonView : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private Button button;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text label;
        [SerializeField] private TMP_Text stockText;

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
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() =>
                {
                    if (_data == null) return;
                    if (_stock <= 0) return;
                    Clicked?.Invoke(_data);
                });
            }

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

            float a = stock > 0 ? enabledAlpha : disabledAlpha;

            if (icon) { var c = icon.color; c.a = a; icon.color = c; }
            if (label) { var c = label.color; c.a = a; label.color = c; }
            if (stockText) { var c = stockText.color; c.a = a; stockText.color = c; }

            if (button)
                button.interactable = stock > 0;
        }

        public void SetSelected(bool on)
        {
            if (!icon) return;

            var c = icon.color;
            c.a = on ? 1f : (_stock > 0 ? enabledAlpha : disabledAlpha);
            icon.color = c;
        }
    }
}