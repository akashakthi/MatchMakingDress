// Assets/MMDress/Scripts/Runtime/Services/StockService.cs
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using MMDress.Data;

namespace MMDress.Services
{
    [DisallowMultipleComponent]
    public sealed class StockService : MonoBehaviour
    {
        [Header("Sumber item (harus sama dengan Fitting)")]
        [SerializeField] private CatalogSO catalog;

        [Header("Auto Find Catalog")]
        [SerializeField] private bool autoFindCatalog = false;

        // === MATERIALS (SO-based) ===
        // key = MaterialSO (by reference), value = qty
        private readonly Dictionary<MaterialSO, int> materialStock = new();

        // === GARMENTS (per relative index) ===
        int[] _tops;     // size = catalog.TopCount
        int[] _bottoms;  // size = catalog.BottomCount

        public CatalogSO Catalog => catalog;

        const string KEY_PREFIX = "MMDress.Stock.";
        const string KEY_MAT = KEY_PREFIX + "Mat.";       // + id
        const string KEY_TOP = KEY_PREFIX + "TopCSV";
        const string KEY_BOT = KEY_PREFIX + "BotCSV";

        void Awake()
        {
            if (autoFindCatalog && !catalog)
                catalog = Resources.Load<CatalogSO>("Catalog");
            ResizeArrays();
            LoadAll();   // <- muat dari PlayerPrefs
        }

        void OnApplicationQuit() => SaveAll();

        void ResizeArrays()
        {
            int t = catalog ? catalog.TopCount : 0;
            int b = catalog ? catalog.BottomCount : 0;
            _tops = (t > 0) ? new int[t] : System.Array.Empty<int>();
            _bottoms = (b > 0) ? new int[b] : System.Array.Empty<int>();
        }

        // ========== MATERIAL SO API ==========
        public int GetMaterial(MaterialSO mat)
        {
            if (!mat) return 0;
            return materialStock.TryGetValue(mat, out var count) ? count : 0;
        }

        public void AddMaterial(MaterialSO mat, int qty)
        {
            if (!mat || qty <= 0) return;
            materialStock[mat] = GetMaterial(mat) + qty;
            SaveMaterial(mat); // persist per-material biar ringan
            PublishInventoryChanged();
        }

        public bool HasMaterials(List<MaterialCost> costs, int multiplier = 1)
        {
            if (costs == null) return true;
            foreach (var c in costs)
            {
                if (!c.material) continue;
                if (GetMaterial(c.material) < c.qty * multiplier) return false;
            }
            return true;
        }

        public void ConsumeMaterials(List<MaterialCost> costs, int multiplier = 1)
        {
            if (costs == null) return;
            foreach (var c in costs)
            {
                if (!c.material) continue;
                int cur = GetMaterial(c.material) - (c.qty * multiplier);
                if (cur < 0) cur = 0;
                materialStock[c.material] = cur;
                SaveMaterial(c.material);
            }
            PublishInventoryChanged();
        }

        // ========== GARMENT API ==========
        public int TopTypes => _tops.Length;
        public int BottomTypes => _bottoms.Length;

        public int GetTopCount(int relIndex) =>
            (relIndex >= 0 && relIndex < _tops.Length) ? _tops[relIndex] : 0;

        public int GetBottomCount(int relIndex) =>
            (relIndex >= 0 && relIndex < _bottoms.Length) ? _bottoms[relIndex] : 0;

        public bool TryCraft(ItemSO item, int qty)
        {
            if (!catalog || !item || qty <= 0) return false;

            // harus pakai material costs (SO)
            if (!(item.requiresMaterials && item.materialCosts != null && item.materialCosts.Count > 0))
                return false;

            if (!HasMaterials(item.materialCosts, qty)) return false;
            ConsumeMaterials(item.materialCosts, qty);

            if (item.slot == OutfitSlot.Top)
            {
                int rel = catalog.GetRelativeIndex(item);
                if (rel < 0 || rel >= _tops.Length) return false;
                _tops[rel] += qty;
                SaveTopBottom(); // simpan batch
            }
            else
            {
                int rel = catalog.GetRelativeIndex(item);
                if (rel < 0 || rel >= _bottoms.Length) return false;
                _bottoms[rel] += qty;
                SaveTopBottom();
            }

            PublishInventoryChanged();
            return true;
        }

        public bool TryUncraft(ItemSO item, int qty, bool refundMaterials)
        {
            if (!catalog || !item || qty <= 0) return false;

            if (item.slot == OutfitSlot.Top)
            {
                int rel = catalog.GetRelativeIndex(item);
                if (rel < 0 || rel >= _tops.Length || _tops[rel] < qty) return false;
                _tops[rel] -= qty;
                SaveTopBottom();
            }
            else
            {
                int rel = catalog.GetRelativeIndex(item);
                if (rel < 0 || rel >= _bottoms.Length || _bottoms[rel] < qty) return false;
                _bottoms[rel] -= qty;
                SaveTopBottom();
            }

            if (refundMaterials && item.materialCosts != null)
                AddBackMaterials(item.materialCosts, qty);

            PublishInventoryChanged();
            return true;
        }

        void AddBackMaterials(List<MaterialCost> costs, int multiplier)
        {
            foreach (var c in costs)
            {
                if (!c.material) continue;
                materialStock[c.material] = GetMaterial(c.material) + (c.qty * multiplier);
                SaveMaterial(c.material);
            }
        }

        // ========== UI legacy helper ==========
        public ItemSO GetItem(GarmentSlot slot, int relIndex)
        {
            if (!catalog) return null;
            int count = 0;
            foreach (var it in catalog.Items)
            {
                if (!it) continue;
                if ((slot == GarmentSlot.Top && it.slot != OutfitSlot.Top) ||
                    (slot == GarmentSlot.Bottom && it.slot != OutfitSlot.Bottom))
                    continue;

                if (count == relIndex) return it;
                count++;
            }
            return null;
        }

        // ========== Persist helpers ==========
        void SaveMaterial(MaterialSO mat)
        {
            if (!mat) return;
            PlayerPrefs.SetInt(KEY_MAT + mat.id, GetMaterial(mat));
            PlayerPrefs.Save();
        }

        void SaveTopBottom()
        {
            PlayerPrefs.SetString(KEY_TOP, ToCsv(_tops));
            PlayerPrefs.SetString(KEY_BOT, ToCsv(_bottoms));
            PlayerPrefs.Save();
        }

        public void SaveAll()
        {
            if (catalog)
            {
                SaveTopBottom();
            }
            // simpan semua material terdaftar di katalog & yang pernah tersentuh
            foreach (var kv in materialStock)
                SaveMaterial(kv.Key);
        }

        void LoadAll()
        {
            // load materials: scan semua MaterialSO yang mungkin dipakai
            // (cara simpel: dari semua item.materialCosts)
            if (catalog)
            {
                foreach (var it in catalog.Items)
                {
                    if (!it || it.materialCosts == null) continue;
                    foreach (var c in it.materialCosts)
                    {
                        if (!c.material) continue;
                        int val = PlayerPrefs.GetInt(KEY_MAT + c.material.id, 0);
                        if (val > 0) materialStock[c.material] = val;
                    }
                }
            }

            // load garment arrays
            if (catalog)
            {
                var topCsv = PlayerPrefs.GetString(KEY_TOP, "");
                var botCsv = PlayerPrefs.GetString(KEY_BOT, "");
                FromCsv(topCsv, ref _tops);
                FromCsv(botCsv, ref _bottoms);
            }
        }

        static string ToCsv(int[] arr)
        {
            if (arr == null || arr.Length == 0) return "";
            var sb = new StringBuilder();
            for (int i = 0; i < arr.Length; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(arr[i]);
            }
            return sb.ToString();
        }

        static void FromCsv(string csv, ref int[] target)
        {
            if (string.IsNullOrEmpty(csv) || target == null || target.Length == 0) return;
            var parts = csv.Split(',');
            for (int i = 0; i < target.Length && i < parts.Length; i++)
            {
                if (int.TryParse(parts[i], out var v)) target[i] = v;
            }
        }

        void PublishInventoryChanged()
        {
            // kalau kamu punya event global untuk refresh HUD
            MMDress.Core.ServiceLocator.Events?.Publish(new MMDress.UI.InventoryChanged());
        }
        public int GetGarment(ItemSO item)
        {
            if (!catalog || !item) return 0;

            int rel = catalog.GetRelativeIndex(item); // index relatif dalam slot item itu
            if (rel < 0) return 0;

            return item.slot == OutfitSlot.Top
                ? GetTopCount(rel)
                : GetBottomCount(rel);
        }
    }

    // legacy enum (cukup keep untuk UI lama yang masih refer)
    public enum GarmentSlot { Top, Bottom }
}
