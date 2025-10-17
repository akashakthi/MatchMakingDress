// Assets/MMDress/Scripts/Runtime/UI/ItemGridView.cs
using System.Collections.Generic;
using UnityEngine;
using MMDress.Data;
using MMDress.Services;

namespace MMDress.UI
{
    /// List horizontal generik. Content = HorizontalLayoutGroup + ContentSizeFitter(Horizontal=Preferred).
    [DisallowMultipleComponent]
    public sealed class ItemGridView : MonoBehaviour
    {
        [Header("Sources")]
        [SerializeField] private CatalogSO catalog;
        [SerializeField] private OutfitSlot slot = OutfitSlot.Top;
        [SerializeField] private StockService stock; // opsional: tampilkan stok

        [Header("UI")]
        [SerializeField] private Transform content;       // parent button
        [SerializeField] private GameObject buttonPrefab; // root harus punya ItemButtonView

        private readonly List<ItemButtonView> _buttons = new();
        private readonly List<ItemSO> _buffer = new();   // hasil filter catalog per slot
        private ItemSO _selected;

        public System.Action<ItemSO> OnItemSelected;

        public void SetCatalog(CatalogSO c) => catalog = c;
        public void SetSlot(OutfitSlot s) => slot = s;

        public void Refresh(ItemSO currentSelected = null)
        {
            if (!catalog) { Debug.LogWarning("[ItemGrid] Catalog belum di-assign", this); return; }
            if (!content) { Debug.LogError("[ItemGrid] Content belum di-assign", this); return; }
            if (!buttonPrefab) { Debug.LogError("[ItemGrid] Button Prefab belum di-assign", this); return; }

            _selected = currentSelected;

            // — filter katalog ke buffer sesuai slot —
            FilterCatalogBySlot(catalog, slot, _buffer);

            EnsurePool(_buffer.Count);

            for (int i = 0; i < _buttons.Count; i++)
            {
                var btn = _buttons[i];
                if (!btn) continue;

                // lepas listener lama biar gak double
                btn.Clicked -= OnClickedProxy;

                bool active = i < _buffer.Count;
                btn.gameObject.SetActive(active);
                if (!active) continue;

                var data = _buffer[i];
                btn.Bind(data);

                // tampilkan stok jika service ada
                if (stock) btn.BindStock(GetStockCountForItem(stock, catalog, data));

                btn.SetSelected(data == _selected);
                btn.Clicked += OnClickedProxy;
            }
        }

        // —— helpers ——

        private static void FilterCatalogBySlot(CatalogSO cat, OutfitSlot s, List<ItemSO> dst)
        {
            dst.Clear();
            var list = cat.Items;               // IReadOnlyList<ItemSO>
            for (int i = 0; i < list.Count; i++)
            {
                var it = list[i];
                if (it && it.slot == s) dst.Add(it);
            }
        }

        // Ambil stok dari StockService berdasarkan item (pakai index relatif + slot)
        private static int GetStockCountForItem(StockService stock, CatalogSO cat, ItemSO item)
        {
            if (!stock || !cat || !item) return 0;

            // map OutfitSlot → GarmentSlot
            var gslot = item.slot == OutfitSlot.Top
                ? MMDress.Runtime.Inventory.GarmentSlot.Top
                : MMDress.Runtime.Inventory.GarmentSlot.Bottom;

            int rel = cat.GetRelativeIndex(item);   // 0..N-1 untuk slot tersebut
            if (rel < 0) return 0;

            return stock.GetGarmentCount(gslot, rel);
        }

        private void EnsurePool(int need)
        {
            while (_buttons.Count < need)
            {
                var go = Instantiate(buttonPrefab, content);
                var btn = go.GetComponent<ItemButtonView>();
                if (!btn)
                {
                    Debug.LogError("[ItemGrid] Button Prefab TIDAK punya ItemButtonView di root.", go);
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

            for (int i = 0; i < _buttons.Count; i++)
            {
                var b = _buttons[i];
                if (b && b.gameObject.activeSelf)
                    b.SetSelected(b.Data == _selected);
            }
        }
    }
}
