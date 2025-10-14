using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MMDress.Data;
using MMDress.Services;   // untuk angka stok

namespace MMDress.UI
{
    /// List horizontal generik. Content = HorizontalLayoutGroup + ContentSizeFitter(Horizontal=Preferred).
    [DisallowMultipleComponent]
    public sealed class ItemGridView : MonoBehaviour
    {
        [Header("Sources")]
        [SerializeField] private CatalogSO catalog;
        [SerializeField] private OutfitSlot slot = OutfitSlot.Top;
        [SerializeField] private StockService stock; // opsional: isi untuk menampilkan stok

        [Header("UI")]
        [SerializeField] private Transform content;       // parent untuk tombol
        [SerializeField] private GameObject buttonPrefab; // root harus ada ItemButtonView

        private readonly List<ItemButtonView> _buttons = new();
        private ItemSO _selected;

        public System.Action<ItemSO> OnItemSelected;

        public void SetCatalog(CatalogSO c) => catalog = c;
        public void SetSlot(OutfitSlot s) => slot = s;

        public void Refresh(ItemSO currentSelected = null)
        {
            if (!catalog) { Debug.LogWarning("[Grid] Catalog belum di-assign", this); return; }
            if (!content) { Debug.LogError("[Grid] Content belum di-assign", this); return; }
            if (!buttonPrefab) { Debug.LogError("[Grid] Button Prefab belum di-assign", this); return; }

            _selected = currentSelected;
            var items = catalog.items.Where(i => i && i.slot == slot).ToList();

            EnsurePool(items.Count);

            for (int i = 0; i < _buttons.Count; i++)
            {
                var btn = _buttons[i];
                if (!btn) continue;

                btn.Clicked -= OnClickedProxy;

                bool active = i < items.Count;
                btn.gameObject.SetActive(active);
                if (!active) continue;

                var data = items[i];
                btn.Bind(data);

                if (stock) btn.BindStock(stock.GetGarmentCount(data));

                btn.SetSelected(data == _selected);
                btn.Clicked += OnClickedProxy;
            }
        }

        private void EnsurePool(int need)
        {
            while (_buttons.Count < need)
            {
                var go = Instantiate(buttonPrefab, content);
                var btn = go.GetComponent<ItemButtonView>();
                if (!btn)
                {
                    Debug.LogError("[Grid] Button Prefab TIDAK punya ItemButtonView di root.", go);
                    Destroy(go);
                    return;
                }
                _buttons.Add(btn);
            }
        }

        private void OnClickedProxy(ItemSO item)
        {
            _selected = item;
            OnItemSelected?.Invoke(item);

            foreach (var b in _buttons)
                if (b && b.gameObject.activeSelf)
                    b.SetSelected(b.Data == _selected);
        }
    }
}
