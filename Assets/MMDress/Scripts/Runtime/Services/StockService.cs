// Assets/MMDress/Scripts/Runtime/Services/StockService.cs
using System.Collections.Generic;
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

        // Materials (SO)
        private readonly Dictionary<MaterialSO, int> materialStock = new();

        // Garments (backend lama: per-slot arrays → tetap dipakai)
        int[] _tops;     // size = catalog.TopCount
        int[] _bottoms;  // size = catalog.BottomCount

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
            _tops = t > 0 ? new int[t] : new int[0];
            _bottoms = b > 0 ? new int[b] : new int[0];
        }

        // ===================== MATERIALS (SO) =====================
        public int GetMaterial(MaterialSO mat)
            => (mat && materialStock.TryGetValue(mat, out var v)) ? v : 0;

        public void AddMaterial(MaterialSO mat, int qty)
        {
            if (!mat || qty <= 0) return;
            materialStock[mat] = GetMaterial(mat) + qty;
        }

        // ===================== GARMENTS (SO facade) =====================
        // Map ItemSO → slot array via relative index dari CatalogSO.
        public int GetGarment(ItemSO item)
        {
            if (!catalog || !item) return 0;
            int rel = catalog.GetRelativeIndex(item); // index relatif dalam slot item.slot
            if (rel < 0) return 0;
            if (item.slot == OutfitSlot.Top)
                return (rel < _tops.Length) ? _tops[rel] : 0;
            else
                return (rel < _bottoms.Length) ? _bottoms[rel] : 0;
        }

        public void SetGarment(ItemSO item, int count)
        {
            if (!catalog || !item) return;
            int rel = catalog.GetRelativeIndex(item);
            if (rel < 0) return;

            count = Mathf.Max(0, count);
            if (item.slot == OutfitSlot.Top)
            {
                if (rel < _tops.Length) _tops[rel] = count;
            }
            else
            {
                if (rel < _bottoms.Length) _bottoms[rel] = count;
            }
        }

        public void AddGarment(ItemSO item, int delta)
        {
            if (!item || delta == 0) return;
            int cur = GetGarment(item);
            SetGarment(item, cur + delta);
        }

        public bool TryConsumeGarment(ItemSO item, int qty)
        {
            if (!item || qty <= 0) return false;
            int cur = GetGarment(item);
            if (cur < qty) return false;
            SetGarment(item, cur - qty);
            return true;
        }

        // ===================== CRAFT (pakai MaterialSO costs) =====================
        public bool TryCraft(ItemSO item, int qty)
        {
            if (!item || qty <= 0) return false;

            // Cek bahan
            if (item.requiresMaterials && item.materialCosts != null && item.materialCosts.Count > 0)
            {
                for (int i = 0; i < item.materialCosts.Count; i++)
                {
                    var c = item.materialCosts[i];
                    if (!c.material) continue;
                    if (GetMaterial(c.material) < c.qty * qty) return false;
                }
                // Konsumsi bahan
                foreach (var c in item.materialCosts)
                {
                    if (!c.material) continue;
                    materialStock[c.material] = GetMaterial(c.material) - c.qty * qty;
                }
            }
            else
            {
                // Tidak ada requirement → tetap izinkan craft
            }

            AddGarment(item, qty);
            return true;
        }

        public bool TryUncraft(ItemSO item, int qty, bool refundMaterials)
        {
            if (!item || qty <= 0) return false;
            if (!TryConsumeGarment(item, qty)) return false;

            if (refundMaterials && item.requiresMaterials && item.materialCosts != null)
            {
                foreach (var c in item.materialCosts)
                {
                    if (!c.material) continue;
                    AddMaterial(c.material, c.qty * qty);
                }
            }
            return true;
        }

        // ===================== LEGACY HELPER (UI lama masih pakai) =====================
        public ItemSO GetItem(GarmentSlot slot, int relIndex)
        {
            if (!catalog) return null;
            int count = 0;
            foreach (var it in catalog.Items)
            {
                if (!it) continue;
                if (slot == GarmentSlot.Top && it.slot != OutfitSlot.Top) continue;
                if (slot == GarmentSlot.Bottom && it.slot != OutfitSlot.Bottom) continue;
                if (count == relIndex) return it;
                count++;
            }
            return null;
        }
    }
}
