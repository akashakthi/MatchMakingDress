using UnityEngine;
using MMDress.Core;                  // ServiceLocator
using MMDress.Runtime.Inventory;     // ItemSO, GarmentSlot, MaterialType
using System;

// alias untuk kejelasan
using InvMaterialType = MMDress.Runtime.Inventory.MaterialType;

namespace MMDress.Services
{
    [DisallowMultipleComponent]
    public sealed class ProcurementService : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private EconomyService economy;
        [SerializeField] private StockService stock;

        [Header("Harga bahan (per unit)")]
        [SerializeField, Min(0)] private int priceCloth = 100;
        [SerializeField, Min(0)] private int priceThread = 100;

        private void Reset()
        {
            economy = FindObjectOfType<EconomyService>(true);
            stock = FindObjectOfType<StockService>(true);
        }

        // === DIPAKAI PREP SHOP PANEL (wraps) ===
        public int GetMaterial(InvMaterialType t) => stock ? stock.GetMaterial(t) : 0;
        public int GetTopTypes() => stock ? stock.TopTypes : 0;
        public int GetBottomTypes() => stock ? stock.BottomTypes : 0;
        public int GetGarmentCount(GarmentSlot slot, int typeIndex)
            => stock ? stock.GetGarmentCount(slot, typeIndex) : 0;

        // === BAHAN ===
        public bool BuyMaterial(InvMaterialType t, int qty)
        {
            if (!stock || !economy || qty <= 0) return false;

            int unit = (t == InvMaterialType.Cloth) ? priceCloth : priceThread;
            int total = unit * qty;

            // gunakan Spend/CanSpend sesuai EconomyService
            if (!economy.Spend(total))
            {
                ServiceLocator.Events?.Publish(new PurchaseFailed("Uang tidak cukup"));
                return false;
            }

            stock.AddMaterial(t, qty);
            ServiceLocator.Events?.Publish(new PurchaseSucceeded());
            return true;
        }

        // === PAKAIAN (CRAFT 1 kain + 1 benang per item) ===
        public bool Craft(GarmentSlot slot, int typeIndex, int qty)
        {
            if (!stock || qty <= 0) return false;

            var item = stock.GetItem(slot, typeIndex);
            if (!item)
            {
                ServiceLocator.Events?.Publish(new CraftFailed("Item tidak ada"));
                return false;
            }

            if (!stock.TryCraft(item, qty))
            {
                ServiceLocator.Events?.Publish(new CraftFailed("Bahan kurang (butuh 1 Kain + 1 Benang)"));
                return false;
            }

            ServiceLocator.Events?.Publish(new CraftSucceeded());
            return true;
        }

        public bool Uncraft(GarmentSlot slot, int typeIndex, int qty, bool refundMaterials = true)
        {
            if (!stock || qty <= 0) return false;

            var item = stock.GetItem(slot, typeIndex);
            if (!item) return false;

            bool ok = stock.TryUncraft(item, qty, refundMaterials);
            if (ok) ServiceLocator.Events?.Publish(new CraftSucceeded());
            else ServiceLocator.Events?.Publish(new CraftFailed("Stok 0"));
            return ok;
        }
    }

    // Event payload (tinggal di namespace Services saja agar tidak bentrok)
    public struct PurchaseSucceeded { }
    public struct PurchaseFailed { public string reason; public PurchaseFailed(string r) { reason = r; } }
    public struct CraftSucceeded { }
    public struct CraftFailed { public string reason; public CraftFailed(string r) { reason = r; } }
}
