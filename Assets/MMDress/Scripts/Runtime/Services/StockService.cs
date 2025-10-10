using System.Collections.Generic;
using UnityEngine;
using MMDress.Runtime.Inventory;   // CatalogSO, ItemSO, OutfitSlot, GarmentSlot, MaterialType
using MMDress.Core;                // ServiceLocator
using MMDress.UI;                  // InventoryChanged

// alias untuk enum material
using InvMaterialType = MMDress.Runtime.Inventory.MaterialType;
using MMDress.Data;

namespace MMDress.Services
{
    [DisallowMultipleComponent]
    public sealed class StockService : MonoBehaviour
    {
        [Header("Sumber Item (katalog yang sama dengan Fitting)")]
        [SerializeField] private CatalogSO catalog;
        [SerializeField] private bool autoFindCatalog = true;

        // Bahan (hanya 1 enum: Inventory.MaterialType)
        private readonly Dictionary<InvMaterialType, int> _materials = new()
        {
            { InvMaterialType.Cloth,  0 },
            { InvMaterialType.Thread, 0 },
        };

        // Stok pakaian per ItemSO
        private readonly Dictionary<ItemSO, int> _garments = new();

        // Cache list per slot (urut dari CatalogSO)
        private readonly List<ItemSO> _tops = new();
        private readonly List<ItemSO> _bottoms = new();

        public int TopTypes => _tops.Count;
        public int BottomTypes => _bottoms.Count;

        public int GetMaterial(InvMaterialType t) => _materials.TryGetValue(t, out var v) ? v : 0;
        public void AddMaterial(InvMaterialType t, int delta)
        {
            _materials[t] = Mathf.Max(0, GetMaterial(t) + delta);
            ServiceLocator.Events?.Publish(new InventoryChanged());
        }

        public ItemSO GetItem(GarmentSlot slot, int typeIndex)
        {
            var list = (slot == GarmentSlot.Top) ? _tops : _bottoms;
            return (typeIndex >= 0 && typeIndex < list.Count) ? list[typeIndex] : null;
        }

        public int GetGarmentCount(GarmentSlot slot, int typeIndex)
        {
            var it = GetItem(slot, typeIndex);
            return it ? GetGarmentCount(it) : 0;
        }
        public int GetGarmentCount(ItemSO item) => _garments.TryGetValue(item, out var v) ? v : 0;
        public bool HasAny(ItemSO item) => GetGarmentCount(item) > 0;

        // ==== Crafting (1 kain + 1 benang per item) ====
        public bool TryCraft(ItemSO item, int qty)
        {
            if (!item || qty <= 0) return false;

            if (GetMaterial(InvMaterialType.Cloth) < qty) return false;
            if (GetMaterial(InvMaterialType.Thread) < qty) return false;

            AddMaterial(InvMaterialType.Cloth, -qty);
            AddMaterial(InvMaterialType.Thread, -qty);

            _garments[item] = GetGarmentCount(item) + qty;
            ServiceLocator.Events?.Publish(new InventoryChanged());
            return true;
        }

        public bool TryUncraft(ItemSO item, int qty, bool refundMaterials)
        {
            if (!item || qty <= 0) return false;

            int cur = GetGarmentCount(item);
            if (cur < qty) return false;

            _garments[item] = cur - qty;

            if (refundMaterials)
            {
                AddMaterial(InvMaterialType.Cloth, +qty);
                AddMaterial(InvMaterialType.Thread, +qty);
            }

            ServiceLocator.Events?.Publish(new InventoryChanged());
            return true;
        }

        private void Awake()
        {
            if (autoFindCatalog && !catalog)
                catalog = FindObjectOfType<CatalogSO>(true);
            RebuildFromCatalog();
        }

        private void RebuildFromCatalog()
        {
            _tops.Clear(); _bottoms.Clear();

            if (!catalog || catalog.items == null) return;
            foreach (var it in catalog.items)
            {
                if (!it) continue;
                if (!_garments.ContainsKey(it)) _garments[it] = 0;

                if (it.slot == OutfitSlot.Top) _tops.Add(it);
                else if (it.slot == OutfitSlot.Bottom) _bottoms.Add(it);
            }
        }
    }
}
