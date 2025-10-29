using UnityEngine;
using MMDress.Core;
using MMDress.Data;

// alias event AGAR TIDAK AMBIGU dengan MMDress.UI
using SvcPurchaseSucceeded = MMDress.Services.PurchaseSucceeded;
using SvcPurchaseFailed = MMDress.Services.PurchaseFailed;
using SvcCraftSucceeded = MMDress.Services.CraftSucceeded;
using SvcCraftFailed = MMDress.Services.CraftFailed;

namespace MMDress.Services
{
    [DisallowMultipleComponent]
    public sealed class ProcurementService : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private EconomyService economy;
        [SerializeField] private StockService stock;

        [Header("Fallback Harga Material (jika SO.price=0)")]
        [SerializeField, Min(0)] private int defaultMaterialSOPrice = 100;

        [Header("Debug")]
        [SerializeField] private bool verbose = false;

        void Reset()
        {
#if UNITY_2023_1_OR_NEWER
            economy = economy ? economy : UnityEngine.Object.FindAnyObjectByType<EconomyService>(FindObjectsInactive.Include);
            stock = stock ? stock : UnityEngine.Object.FindAnyObjectByType<StockService>(FindObjectsInactive.Include);
#else
            economy = economy ? economy : FindObjectOfType<EconomyService>(true);
            stock   = stock   ? stock   : FindObjectOfType<StockService>(true);
#endif
        }

        // ==== Read helpers (untuk UI) ====
        public int GetMaterial(MaterialSO mat) => stock ? stock.GetMaterial(mat) : 0;
        public int GetGarment(ItemSO item) => stock ? stock.GetGarment(item) : 0;   // << baru
        public bool TryGetEconomy(out EconomyService eco) { eco = economy; return eco != null; }
        public int GetMoneyBalance() => economy ? economy.Balance : 0;

        // ==== Passthrough untuk persistence / admin ====
        public void SetMaterial(MaterialSO mat, int amount)
        {
            if (!stock || !mat) return;
            stock.SetMaterial(mat, Mathf.Max(0, amount));
            if (verbose) Debug.Log($"[Procure] SET {mat.displayName} = {amount}");
        }

        public void AddMaterial(MaterialSO mat, int delta)
        {
            if (!stock || !mat) return;
            stock.AddMaterial(mat, delta);
            if (verbose) Debug.Log($"[Procure] ADD {delta} {mat.displayName}");
        }

        // ===== helper: panggil persistence agar langsung tersimpan =====
        private static MMDress.Runtime.Services.Persistence.PrepPersistenceService FindPersist()
        {
#if UNITY_2023_1_OR_NEWER
            return UnityEngine.Object.FindFirstObjectByType<MMDress.Runtime.Services.Persistence.PrepPersistenceService>(FindObjectsInactive.Include);
#else
            return UnityEngine.Object.FindObjectOfType<MMDress.Runtime.Services.Persistence.PrepPersistenceService>(true);
#endif
        }

        // ==== Beli material (SO) ====
        public bool BuyMaterial(MaterialSO material, int qty)
        {
            if (!stock || !economy || !material || qty <= 0) return false;

            int unit = material.price > 0 ? material.price : defaultMaterialSOPrice;
            int total = unit * qty;

            if (!economy.Spend(total))
            {
                ServiceLocator.Events?.Publish<SvcPurchaseFailed>(
                    new SvcPurchaseFailed($"Uang tidak cukup untuk beli {material.displayName}"));
                return false;
            }

            stock.AddMaterial(material, qty);
            if (verbose) Debug.Log($"[Procure] BUY {qty}x {material.displayName} @ {unit} = {total}, sisa={economy.Balance}");
            ServiceLocator.Events?.Publish<SvcPurchaseSucceeded>(new SvcPurchaseSucceeded());
            FindPersist()?.ForceSaveNow(); // simpan instan
            return true;
        }

        // ==== Craft / Uncraft by ItemSO ====
        public bool CraftByItem(ItemSO item, int qty)
        {
            if (!stock || !item || qty <= 0) return false;

            if (!stock.TryCraft(item, qty))
            {
                ServiceLocator.Events?.Publish<SvcCraftFailed>(
                    new SvcCraftFailed($"Bahan kurang untuk {item.displayName}"));
                return false;
            }

            if (verbose) Debug.Log($"[Procure] CRAFT {qty}x {item.displayName}");
            ServiceLocator.Events?.Publish<SvcCraftSucceeded>(new SvcCraftSucceeded());
            FindPersist()?.ForceSaveNow(); // simpan stok ItemSO & material
            return true;
        }

        public bool UncraftByItem(ItemSO item, int qty, bool refundMaterials = true)
        {
            if (!stock || !item || qty <= 0) return false;

            bool ok = stock.TryUncraft(item, qty, refundMaterials);
            if (ok)
            {
                ServiceLocator.Events?.Publish<SvcCraftSucceeded>(new SvcCraftSucceeded());
                FindPersist()?.ForceSaveNow(); // simpan instan
            }
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
