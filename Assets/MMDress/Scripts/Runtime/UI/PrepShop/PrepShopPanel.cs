using UnityEngine;
using UnityEngine.UI;
using MMDress.Services;
using MMDress.Runtime.Inventory;
using MMDress.Core;
using MMDress.UI;
using MMDress.Runtime.Timer;

// Alias untuk menghindari ambiguous types
using SvcPurchaseSucceeded = MMDress.Services.PurchaseSucceeded;
using SvcPurchaseFailed = MMDress.Services.PurchaseFailed;
using SvcCraftSucceeded = MMDress.Services.CraftSucceeded;
using SvcCraftFailed = MMDress.Services.CraftFailed;
using InvMaterialType = MMDress.Runtime.Inventory.MaterialType;

namespace MMDress.Runtime.UI.PrepShop
{
    [DisallowMultipleComponent]
    public sealed class PrepShopPanel : MonoBehaviour
    {
        [Header("Refs (drag)")]
        [SerializeField] private ProcurementService procurement;
        [SerializeField] private TimeOfDayService timeOfDay;

        [Header("UI Texts")]
        [SerializeField] private Text titleText;
        [SerializeField] private Text countdownText;
        [SerializeField] private Text clothText;
        [SerializeField] private Text threadText;
        [SerializeField] private Text moneyText;

        [Header("Top counts (optional quick view)")]
        [SerializeField] private Text topsSummary;
        [SerializeField] private Text bottomsSummary;

        [Header("Auto-find")]
        [SerializeField] private bool autoFind = true;

        System.Action<InventoryChanged> _onInv;
        System.Action<SvcPurchaseSucceeded> _onBuyOK;
        System.Action<SvcPurchaseFailed> _onBuyFail;
        System.Action<SvcCraftSucceeded> _onCraftOK;
        System.Action<SvcCraftFailed> _onCraftFail;
        System.Action<MoneyChanged> _onMoney;

        void Awake()
        {
            if (autoFind)
            {
                if (!procurement) procurement = FindObjectOfType<ProcurementService>(true);
                if (!timeOfDay) timeOfDay = FindObjectOfType<TimeOfDayService>(true);
            }
            // Biarkan Phase/Spawn gate yang mengatur SetActive
        }

        void OnEnable()
        {
            if (titleText) titleText.text = "Prep Shop (06:00–08:00)";
            RefreshInventory();

            _onInv = _ => RefreshInventory();
            _onBuyOK = _ => RefreshInventory();
            _onBuyFail = e => Debug.LogWarning($"[PrepShop] Beli gagal: {e.reason}");
            _onCraftOK = _ => RefreshInventory();
            _onCraftFail = e => Debug.LogWarning($"[PrepShop] Craft gagal: {e.reason}");

            ServiceLocator.Events.Subscribe(_onInv);
            ServiceLocator.Events.Subscribe(_onBuyOK);
            ServiceLocator.Events.Subscribe(_onBuyFail);
            ServiceLocator.Events.Subscribe(_onCraftOK);
            ServiceLocator.Events.Subscribe(_onCraftFail);

            _onMoney = e => { if (moneyText) moneyText.text = $"$ {e.balance}"; };
            ServiceLocator.Events.Subscribe(_onMoney);
        }

        void OnDisable()
        {
            if (_onInv != null) ServiceLocator.Events.Unsubscribe(_onInv);
            if (_onBuyOK != null) ServiceLocator.Events.Unsubscribe(_onBuyOK);
            if (_onBuyFail != null) ServiceLocator.Events.Unsubscribe(_onBuyFail);
            if (_onCraftOK != null) ServiceLocator.Events.Unsubscribe(_onCraftOK);
            if (_onCraftFail != null) ServiceLocator.Events.Unsubscribe(_onCraftFail);
            if (_onMoney != null) ServiceLocator.Events.Unsubscribe(_onMoney);
        }

        void Update()
        {
            if (!isActiveAndEnabled) return;
            if (countdownText && timeOfDay) countdownText.text = $"Sisa: {timeOfDay.GetClockText()}";
        }

        void RefreshInventory()
        {
            if (!procurement) return;

            if (clothText) clothText.text = $"Cloth:  {procurement.GetMaterial(InvMaterialType.Cloth)}";
            if (threadText) threadText.text = $"Thread: {procurement.GetMaterial(InvMaterialType.Thread)}";

            if (topsSummary)
            {
                int n = procurement.GetTopTypes();
                var sb = new System.Text.StringBuilder("Top: ");
                for (int i = 0; i < n; i++)
                {
                    if (i > 0) sb.Append(", ");
                    sb.Append($"T{i + 1}:{procurement.GetGarmentCount(GarmentSlot.Top, i)}");
                }
                topsSummary.text = sb.ToString();
            }

            if (bottomsSummary)
            {
                int n = procurement.GetBottomTypes();
                var sb = new System.Text.StringBuilder("Bottom: ");
                for (int i = 0; i < n; i++)
                {
                    if (i > 0) sb.Append(", ");
                    sb.Append($"B{i + 1}:{procurement.GetGarmentCount(GarmentSlot.Bottom, i)}");
                }
                bottomsSummary.text = sb.ToString();
            }
        }

        // ==== Buttons (Material) ====
        public void OnBuyCloth1() => procurement?.BuyMaterial(InvMaterialType.Cloth, 1);
        public void OnBuyCloth5() => procurement?.BuyMaterial(InvMaterialType.Cloth, 5);
        public void OnBuyCloth10() => procurement?.BuyMaterial(InvMaterialType.Cloth, 10);

        public void OnBuyThread1() => procurement?.BuyMaterial(InvMaterialType.Thread, 1);
        public void OnBuyThread5() => procurement?.BuyMaterial(InvMaterialType.Thread, 5);
        public void OnBuyThread10() => procurement?.BuyMaterial(InvMaterialType.Thread, 10);

        // ==== Buttons (Crafting) ====
        public void OnCraftTop(int typeIndex) => procurement?.Craft(GarmentSlot.Top, typeIndex, 1);
        public void OnCraftBottom(int typeIndex) => procurement?.Craft(GarmentSlot.Bottom, typeIndex, 1);
        public void OnCraftTop5(int typeIndex) => procurement?.Craft(GarmentSlot.Top, typeIndex, 5);
        public void OnCraftBottom5(int typeIndex) => procurement?.Craft(GarmentSlot.Bottom, typeIndex, 5);
    }
}
