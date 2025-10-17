// Assets/MMDress/Scripts/Runtime/Services/StockService.cs
using UnityEngine;
using MMDress.Data;
using MMDress.Runtime.Inventory;

namespace MMDress.Services
{
    public sealed class StockService : MonoBehaviour
    {
        [Header("Sumber item (katalog yang sama dengan Fitting)")]
        [SerializeField] private CatalogSO catalog;

        [Header("Auto Find Catalog")]
        [SerializeField] private bool autoFindCatalog = false;

        // Materials
        int _cloth, _thread;

        // Garments
        int[] _tops;     // ukuran = catalog.TopCount
        int[] _bottoms;  // ukuran = catalog.BottomCount

        public CatalogSO Catalog => catalog;

        void Awake()
        {
            if (autoFindCatalog && !catalog)
                catalog = Resources.Load<CatalogSO>("Catalog"); // opsional
            ResizeArrays();
        }

        void ResizeArrays()
        {
            int t = catalog ? catalog.TopCount : 0;
            int b = catalog ? catalog.BottomCount : 0;
            _tops = (t > 0) ? new int[t] : new int[0];
            _bottoms = (b > 0) ? new int[b] : new int[0];
        }

        // === Materials ===
        public int GetMaterial(MaterialType t) => t == MaterialType.Cloth ? _cloth : _thread;

        public void AddMaterial(MaterialType t, int qty)
        {
            if (qty <= 0) return;
            if (t == MaterialType.Cloth) _cloth += qty;
            else _thread += qty;
        }

        // === Garments (per slot, index relatif) ===
        public int TopTypes => _tops.Length;
        public int BottomTypes => _bottoms.Length;

        public int GetGarmentCount(GarmentSlot slot, int relIndex)
        {
            return slot == GarmentSlot.Top
                ? (relIndex >= 0 && relIndex < _tops.Length ? _tops[relIndex] : 0)
                : (relIndex >= 0 && relIndex < _bottoms.Length ? _bottoms[relIndex] : 0);
        }

        // Craft = ambil 1 kain + 1 benang per item → tambahkan stok baju
        public bool TryCraft(ItemSO item, int qty)
        {
            if (!item || qty <= 0) return false;
            int need = qty;

            if (_cloth < need || _thread < need) return false;
            _cloth -= need; _thread -= need;

            if (item.slot == OutfitSlot.Top)
            {
                int rel = catalog.GetRelativeIndex(item);
                if (rel < 0 || rel >= _tops.Length) return false;
                _tops[rel] += qty;
            }
            else
            {
                int rel = catalog.GetRelativeIndex(item);
                if (rel < 0 || rel >= _bottoms.Length) return false;
                _bottoms[rel] += qty;
            }
            return true;
        }

        // Uncraft = kurangi stok baju (opsional kembalikan bahan)
        public bool TryUncraft(ItemSO item, int qty, bool refundMaterials)
        {
            if (!item || qty <= 0) return false;

            if (item.slot == OutfitSlot.Top)
            {
                int rel = catalog.GetRelativeIndex(item);
                if (rel < 0 || rel >= _tops.Length || _tops[rel] < qty) return false;
                _tops[rel] -= qty;
            }
            else
            {
                int rel = catalog.GetRelativeIndex(item);
                if (rel < 0 || rel >= _bottoms.Length || _bottoms[rel] < qty) return false;
                _bottoms[rel] -= qty;
            }

            if (refundMaterials) { _cloth += qty; _thread += qty; }
            return true;
        }

        // Helper untuk UI lama
        public ItemSO GetItem(GarmentSlot slot, int relIndex)
        {
            if (!catalog) return null;
            int count = 0;
            foreach (var it in catalog.Items)
            {
                if (!it || (slot == GarmentSlot.Top ? it.slot != OutfitSlot.Top : it.slot != OutfitSlot.Bottom))
                    continue;
                if (count == relIndex) return it;
                count++;
            }
            return null;
        }
    }
}
