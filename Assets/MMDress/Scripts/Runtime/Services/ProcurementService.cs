using UnityEngine;
using MMDress.Runtime.Inventory;
using MMDress.Runtime.Timer;
using MMDress.Core;
using MMDress.UI;


namespace MMDress.Services
{
    [DisallowMultipleComponent]
    public sealed class ProcurementService : MonoBehaviour
    {
        [Header("Refs (drag)")]
        [SerializeField] private ProcurementConfigSO config;
        [SerializeField] private TimeOfDayService timeOfDay;
        [SerializeField] private EconomyService economy;
        [SerializeField] private StockService stock;

        [Header("State")]
        [SerializeField] private bool setStartingMoneyOnFirstPrep = true;
        private bool _startMoneyApplied = false;

        public bool CanShop => timeOfDay && timeOfDay.CurrentPhase == DayPhase.Prep;

        private void Awake()
        {
            if (!timeOfDay) timeOfDay = FindObjectOfType<TimeOfDayService>(true);
            if (!economy) economy = FindObjectOfType<EconomyService>(true);
            if (!stock) stock = FindObjectOfType<StockService>(true);
        }

        private void OnEnable()
        {
            if (timeOfDay) timeOfDay.DayPhaseChanged += OnPhaseChanged;
        }

        private void OnDisable()
        {
            if (timeOfDay) timeOfDay.DayPhaseChanged -= OnPhaseChanged;
        }

        private void OnPhaseChanged(DayPhase phase)
        {
            if (phase == DayPhase.Prep)
            {
                // Terapkan uang awal saat Prep pertama (sekali saja)
                if (setStartingMoneyOnFirstPrep && !_startMoneyApplied && config)
                {
                    economy?.SetBalance(config.startingMoney);
                    _startMoneyApplied = true;
                }

                // Opsional: sinkronkan durasi prep ke TimeOfDay kalau mau
                if (config && timeOfDay)
                {
                    // kalau TimeOfDayService kamu expose setter ke prepRealSeconds, pakai di sini.
                    // otherwise, set via Inspector.
                }
            }

            if (phase == DayPhase.Closed)
            {
                // bisa publish event khusus ringkasan stok kalau perlu
                ServiceLocator.Events?.Publish(new EndOfDayArrived());
            }
        }

        // ======= PUBLIC API untuk UI =======

        public bool BuyMaterial(MaterialType type, int qty)
        {
            if (!CanShop || qty <= 0 || config == null || economy == null || stock == null)
            {
                ServiceLocator.Events?.Publish(new PurchaseFailed("Tidak bisa belanja saat ini."));
                return false;
            }

            int price = (type == MaterialType.Cloth) ? config.clothPrice : config.threadPrice;
            int cost = price * qty;

            if (!economy.TrySpend(cost))
            {
                ServiceLocator.Events?.Publish(new PurchaseFailed("Uang tidak cukup."));
                return false;
            }

            stock.AddMaterial(type, qty);
            ServiceLocator.Events?.Publish(new PurchaseSucceeded(type, qty, cost));
            return true;
        }

        /// <summary>Craft baju: butuh 1 Cloth + 1 Thread per 1 item.</summary>
        public bool Craft(GarmentSlot slot, int typeIndex, int qty)
        {
            if (!CanShop || qty <= 0 || stock == null)
            {
                ServiceLocator.Events?.Publish(new CraftFailed("Tidak bisa craft saat ini."));
                return false;
            }

            // Cek material cukup?
            if (stock.Cloth < qty || stock.Thread < qty)
            {
                ServiceLocator.Events?.Publish(new CraftFailed("Material tidak cukup (butuh 1 Cloth + 1 Thread per item)."));
                return false;
            }

            // Konsumsi material & tambahkan garment
            if (!stock.TryConsumeMaterial(MaterialType.Cloth, qty)) { ServiceLocator.Events?.Publish(new CraftFailed("Cloth kurang.")); return false; }
            if (!stock.TryConsumeMaterial(MaterialType.Thread, qty)) { ServiceLocator.Events?.Publish(new CraftFailed("Thread kurang.")); return false; }

            stock.AddGarment(slot, typeIndex, qty);
            ServiceLocator.Events?.Publish(new CraftSucceeded(slot, typeIndex, qty));
            return true;
        }

        // Getter util untuk UI
        public int GetGarmentCount(GarmentSlot slot, int typeIndex) => stock?.GetGarmentCount(slot, typeIndex) ?? 0;
        public int GetTopTypes() => stock?.TopTypes ?? (config ? config.topTypes : 0);
        public int GetBottomTypes() => stock?.BottomTypes ?? (config ? config.bottomTypes : 0);
        public int GetMaterial(MaterialType t) => (t == MaterialType.Cloth) ? (stock?.Cloth ?? 0) : (stock?.Thread ?? 0);
        public int GetPrice(MaterialType t) => (t == MaterialType.Cloth) ? (config ? config.clothPrice : 0) : (config ? config.threadPrice : 0);
    }
}
