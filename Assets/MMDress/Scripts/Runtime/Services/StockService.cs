// Assets/MMDress/Scripts/Runtime/Services/StockService.cs
using System.Collections.Generic;
using UnityEngine;
using MMDress.Data;
using MMDress.Runtime.Inventory;

namespace MMDress.Services
{
    [DefaultExecutionOrder(100)]                      // siap lebih dulu dari persistence (1000)
    [DisallowMultipleComponent]
    public sealed class StockService : MonoBehaviour
    {
        [Header("Sumber item (katalog yang sama dengan Fitting)")]
        [SerializeField] private CatalogSO catalog;

        [Header("Auto Find Catalog (Resources/Catalog.asset)")]
        [SerializeField] private bool autoFindCatalog = false;

        // ===== MATERIALS (by ID agar kebal beda instance SO) =====
        [SerializeField] private bool useNameAsIdIfEmpty = true;
        private readonly Dictionary<string, int> _materialById = new();

        // ===== GARMENTS (per-slot arrays) =====
        private int[] _tops;     // size = catalog.TopCount
        private int[] _bottoms;  // size = catalog.BottomCount

        public CatalogSO Catalog => catalog;

        void Awake()
        {
            if (autoFindCatalog && !catalog)
                catalog = Resources.Load<CatalogSO>("Catalog");

            if (!catalog)
                Debug.LogError("[Stock] CatalogSO belum di-assign. Garment tidak akan tersimpan/terapply!", this);

            ResizeArrays();
        }

        // === internal ===
        void ResizeArrays()
        {
            int t = catalog ? catalog.TopCount : 0;
            int b = catalog ? catalog.BottomCount : 0;
            _tops = t > 0 ? new int[t] : System.Array.Empty<int>();
            _bottoms = b > 0 ? new int[b] : System.Array.Empty<int>();
        }

        string MatId(MaterialSO m)
        {
            if (!m) return null;
            if (!string.IsNullOrEmpty(m.id)) return m.id;
            return useNameAsIdIfEmpty ? m.name : null;
        }

        // ===================== MATERIALS API =====================
        public int GetMaterial(MaterialSO mat)
        {
            var id = MatId(mat);
            if (string.IsNullOrEmpty(id)) return 0;
            return _materialById.TryGetValue(id, out var v) ? v : 0;
        }

        public void SetMaterial(MaterialSO mat, int amount)
        {
            var id = MatId(mat);
            if (string.IsNullOrEmpty(id)) return;
            _materialById[id] = Mathf.Max(0, amount);
        }

        public void AddMaterial(MaterialSO mat, int qty)
        {
            var id = MatId(mat);
            if (string.IsNullOrEmpty(id) || qty == 0) return;
            int cur = GetMaterial(mat);
            SetMaterial(mat, cur + qty);
        }

        // ===================== GARMENTS API =====================
        public int GetGarment(ItemSO item)
        {
            if (!catalog)
            {
                Debug.LogError("[Stock] GetGarment() dipanggil tanpa Catalog.", this);
                return 0;
            }
            if (!item) return 0;

            int rel = catalog.GetRelativeIndex(item);
            if (rel < 0)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"[Stock] Item '{item.name}' tidak ditemukan di CatalogSO.", item);
#endif
                return 0;
            }

            return item.slot == OutfitSlot.Top
                ? (rel < _tops.Length ? _tops[rel] : 0)
                : (rel < _bottoms.Length ? _bottoms[rel] : 0);
        }

        public void SetGarment(ItemSO item, int count)
        {
            if (!catalog)
            {
                Debug.LogError("[Stock] SetGarment() diabaikan karena Catalog null.", this);
                return;
            }
            if (!item) return;

            int rel = catalog.GetRelativeIndex(item);
            if (rel < 0)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"[Stock] SetGarment abaikan: '{item.name}' tidak ada di CatalogSO.", item);
#endif
                return;
            }

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

        // ===================== CRAFT / UNCRAFT =====================
        public bool TryCraft(ItemSO item, int qty)
        {
            if (!item || qty <= 0) return false;

            // 1) Validasi bahan
            if (item.requiresMaterials && item.materialCosts != null && item.materialCosts.Count > 0)
            {
                for (int i = 0; i < item.materialCosts.Count; i++)
                {
                    var c = item.materialCosts[i];
                    if (!c.material) continue;
                    int have = GetMaterial(c.material);
                    int need = c.qty * qty;
                    if (have < need)
                    {
#if UNITY_EDITOR
                        Debug.LogWarning($"[Stock] Bahan kurang: {c.material?.displayName} have={have} need={need} untuk {item.displayName} x{qty}", item);
#endif
                        return false;
                    }
                }

                // 2) Konsumsi bahan
                for (int i = 0; i < item.materialCosts.Count; i++)
                {
                    var c = item.materialCosts[i];
                    if (!c.material) continue;
                    int cur = GetMaterial(c.material);
                    SetMaterial(c.material, cur - c.qty * qty);
                }
            }

            // 3) Tambah garment
            AddGarment(item, qty);
            return true;
        }

        public bool TryUncraft(ItemSO item, int qty, bool refundMaterials)
        {
            if (!item || qty <= 0) return false;
            if (!TryConsumeGarment(item, qty)) return false;

            if (refundMaterials && item.requiresMaterials && item.materialCosts != null)
            {
                for (int i = 0; i < item.materialCosts.Count; i++)
                {
                    var c = item.materialCosts[i];
                    if (!c.material) continue;
                    AddMaterial(c.material, c.qty * qty);
                }
            }
            return true;
        }

        // ===================== LEGACY HELPER (UI lama) =====================
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
