// Assets/MMDress/Scripts/Runtime/Services/ProcurementService.cs
using UnityEngine;
using MMDress.Core;
using MMDress.Runtime.Inventory;
using MMDress.Data;

// alias agar tidak bentrok dengan event kembar di MMDress.UI
using SvcPurchaseSucceeded = MMDress.Services.PurchaseSucceeded;
using SvcPurchaseFailed = MMDress.Services.PurchaseFailed;
using SvcCraftSucceeded = MMDress.Services.CraftSucceeded;
using SvcCraftFailed = MMDress.Services.CraftFailed;

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

        public int PriceCloth => priceCloth;
        public int PriceThread => priceThread;

        void Reset()
        {
            economy = FindObjectOfType<EconomyService>(true);
            stock = FindObjectOfType<StockService>(true);
        }

        // ==== Read helpers ====
        public int GetMaterial(InvMaterialType t) => stock ? stock.GetMaterial(t) : 0;
        public int GetTopTypes() => stock ? stock.TopTypes : 0;
        public int GetBottomTypes() => stock ? stock.BottomTypes : 0;
        public int GetGarmentCount(GarmentSlot s, int typeIndex)
            => stock ? stock.GetGarmentCount(s, typeIndex) : 0;

        // ==== Economy access (untuk UI) ====
        public bool TryGetEconomy(out EconomyService eco) { eco = economy; return eco != null; }
        public int GetMoneyBalance() => economy ? economy.Balance : 0;

        // ==== Beli bahan ====
        public bool BuyMaterial(InvMaterialType t, int qty)
        {
            if (!stock || !economy || qty <= 0) return false;

            int unit = (t == InvMaterialType.Cloth) ? priceCloth : priceThread;
            int total = unit * qty;

            if (!economy.Spend(total))
            {
                ServiceLocator.Events?.Publish<SvcPurchaseFailed>(
                    new SvcPurchaseFailed("Uang tidak cukup"));
                return false;
            }

            stock.AddMaterial(t, qty);
            ServiceLocator.Events?.Publish<SvcPurchaseSucceeded>(new SvcPurchaseSucceeded());
            return true;
        }

        // ==== Craft berdasarkan slot + typeIndex (legacy) ====
        public bool Craft(GarmentSlot slot, int typeIndex, int qty)
        {
            if (!stock || qty <= 0) return false;

            var item = stock.GetItem(slot, typeIndex);
            if (!item)
            {
                ServiceLocator.Events?.Publish<SvcCraftFailed>(new SvcCraftFailed("Item tidak ada"));
                return false;
            }

            if (!stock.TryCraft(item, qty))
            {
                ServiceLocator.Events?.Publish<SvcCraftFailed>(
                    new SvcCraftFailed("Bahan kurang (1 Kain + 1 Benang)"));
                return false;
            }

            ServiceLocator.Events?.Publish<SvcCraftSucceeded>(new SvcCraftSucceeded());
            return true;
        }

        // ==== Craft langsung berdasarkan ItemSO (dipakai PrepShopManualPanel) ====
        public bool CraftByItem(ItemSO item, int qty)
        {
            if (!stock || !item || qty <= 0) return false;

            if (!stock.TryCraft(item, qty))
            {
                ServiceLocator.Events?.Publish<SvcCraftFailed>(
                    new SvcCraftFailed("Bahan kurang (1 Kain + 1 Benang)"));
                return false;
            }

            ServiceLocator.Events?.Publish<SvcCraftSucceeded>(new SvcCraftSucceeded());
            return true;
        }

        public bool Uncraft(GarmentSlot slot, int typeIndex, int qty, bool refundMaterials = true)
        {
            if (!stock || qty <= 0) return false;
            var item = stock.GetItem(slot, typeIndex);
            if (!item) return false;

            bool ok = stock.TryUncraft(item, qty, refundMaterials);
            if (ok) ServiceLocator.Events?.Publish<SvcCraftSucceeded>(new SvcCraftSucceeded());
            else ServiceLocator.Events?.Publish<SvcCraftFailed>(new SvcCraftFailed("Stok 0"));
            return ok;
        }
    }

    // events (namespace Services)
    public struct PurchaseSucceeded { }
    public struct PurchaseFailed { public string reason; public PurchaseFailed(string r) { reason = r; } }
    public struct CraftSucceeded { }
    public struct CraftFailed { public string reason; public CraftFailed(string r) { reason = r; } }
}
