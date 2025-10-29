using System.Collections.Generic;
using UnityEngine;
using TMPro;
using MMDress.Data;
using MMDress.Services;
using MMDress.Core;
using MMDress.UI;

// alias Service events (hindari tabrakan dgn UI/*)
using SvcPurchaseSucceeded = MMDress.Services.PurchaseSucceeded;
using SvcPurchaseFailed = MMDress.Services.PurchaseFailed;
using SvcCraftSucceeded = MMDress.Services.CraftSucceeded;
using SvcCraftFailed = MMDress.Services.CraftFailed;

namespace MMDress.Runtime.UI.PrepShop
{
    [DisallowMultipleComponent]
    public sealed class PrepShopManualPanel : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] ProcurementService procurement;

        [Header("Top Cards (maks 5)")]
        [SerializeField] PrepCardView[] topCards = new PrepCardView[5];

        [Header("Bottom Cards (maks 5)")]
        [SerializeField] PrepCardView[] bottomCards = new PrepCardView[5];

        [Header("Header UI")]
        [SerializeField] TMP_Text clothText;
        [SerializeField] TMP_Text threadText;
        [SerializeField] TMP_Text moneyText;

        [Header("Footer Preview")]
        [SerializeField] TMP_Text needClothText;
        [SerializeField] TMP_Text needThreadText;
        [SerializeField] TMP_Text warningText;

        [Header("Material SO Refs")]
        [SerializeField] private MaterialSO clothMaterial;
        [SerializeField] private MaterialSO threadMaterial;

        [Header("Behaviour")]
        [SerializeField] bool craftInstantly = true;

        [Header("Debug")][SerializeField] bool verboseLog = false;

        readonly Dictionary<ItemSO, int> _plan = new();

        int _snapCloth, _snapThread, _snapMoney;

        System.Action<SvcPurchaseSucceeded> _onBuyOK;
        System.Action<SvcPurchaseFailed> _onBuyFail;
        System.Action<SvcCraftSucceeded> _onCraftOK;
        System.Action<SvcCraftFailed> _onCraftFail;
        System.Action<MoneyChanged> _onMoney;

        void Reset()
        {
            if (!procurement) procurement = FindObjectOfType<ProcurementService>(true);
        }

        void Start()
        {
            BuildFromCards();
            SnapAndRefresh();
        }

        void OnEnable()
        {
            BuildFromCards();
            HookEvents();
            SnapAndRefresh();
        }

        void OnDisable() => UnhookEvents();

        void HookEvents()
        {
            _onBuyOK = _ => { if (verboseLog) Debug.Log("[Prep] PurchaseSucceeded"); SnapAndRefresh(); };
            _onBuyFail = e => { if (verboseLog) Debug.LogWarning($"[Prep] PurchaseFailed: {e.reason}"); };
            _onCraftOK = _ => { if (verboseLog) Debug.Log("[Prep] CraftSucceeded"); SnapAndRefresh(); };
            _onCraftFail = e => { if (verboseLog) Debug.LogWarning($"[Prep] CraftFailed: {e.reason}"); };

            ServiceLocator.Events.Subscribe(_onBuyOK);
            ServiceLocator.Events.Subscribe(_onBuyFail);
            ServiceLocator.Events.Subscribe(_onCraftOK);
            ServiceLocator.Events.Subscribe(_onCraftFail);

            _onMoney = e =>
            {
                if (moneyText) moneyText.text = $"Rp {e.balance:N0}";
                if (verboseLog) Debug.Log($"[Prep] MoneyChanged amount={e.amount} balance={e.balance}");
            };
            ServiceLocator.Events.Subscribe(_onMoney);
        }

        void UnhookEvents()
        {
            if (_onBuyOK != null) ServiceLocator.Events.Unsubscribe(_onBuyOK);
            if (_onBuyFail != null) ServiceLocator.Events.Unsubscribe(_onBuyFail);
            if (_onCraftOK != null) ServiceLocator.Events.Unsubscribe(_onCraftOK);
            if (_onCraftFail != null) ServiceLocator.Events.Unsubscribe(_onCraftFail);
            if (_onMoney != null) ServiceLocator.Events.Unsubscribe(_onMoney);
        }

        void BuildFromCards()
        {
            foreach (var c in topCards) if (c) c.OnDelta -= OnCardDelta;
            foreach (var c in bottomCards) if (c) c.OnDelta -= OnCardDelta;

            _plan.Clear();

            void Wire(PrepCardView c)
            {
                if (!c || !c.Item) return;
                c.SetPlanned(0);
                c.OnDelta += OnCardDelta;
                if (!_plan.ContainsKey(c.Item)) _plan.Add(c.Item, 0);
            }

            foreach (var c in topCards) Wire(c);
            foreach (var c in bottomCards) Wire(c);

            RefreshCardStocks(); // << tampilkan stok aktual di awal
        }

        // === Snapshot + UI ===
        void SnapshotInventory()
        {
            _snapCloth = procurement ? procurement.GetMaterial(clothMaterial) : 0;
            _snapThread = procurement ? procurement.GetMaterial(threadMaterial) : 0;
            _snapMoney = procurement != null && procurement.TryGetEconomy(out var eco) ? eco.Balance : 0;

            if (verboseLog) Debug.Log($"[Prep] Snapshot cloth={_snapCloth} thread={_snapThread} money={_snapMoney}");
        }

        void RefreshHeader()
        {
            if (clothText) clothText.text = _snapCloth.ToString();
            if (threadText) threadText.text = _snapThread.ToString();
            if (moneyText) moneyText.text = $"Rp {_snapMoney:N0}";
        }

        // tampilkan stok ItemSO aktual pada kartu
        void RefreshCardStocks()
        {
            if (!procurement) return;

            void Set(PrepCardView c)
            {
                if (!c || !c.Item) return;
                int stockCount = procurement.GetGarment(c.Item);
                c.SetPlanned(stockCount);
            }

            foreach (var c in topCards) Set(c);
            foreach (var c in bottomCards) Set(c);
        }

        int TotalPlanned()
        {
            int t = 0; foreach (var kv in _plan) t += kv.Value; return t;
        }
        int AvailablePairs() => Mathf.Min(_snapCloth, _snapThread);

        void RecalcPreview()
        {
            int planned = TotalPlanned();
            int needCloth = Mathf.Max(0, planned - _snapCloth);
            int needThread = Mathf.Max(0, planned - _snapThread);

            if (needClothText) needClothText.text = needCloth.ToString();
            if (needThreadText) needThreadText.text = needThread.ToString();

            bool ok = planned <= AvailablePairs();
            if (warningText)
            {
                warningText.text = ok ? "" : "Bahan tidak cukup (1 kain + 1 benang / item).";
                warningText.color = ok ? new Color(1, 1, 1, 0) : new Color(1f, .3f, .3f, 1f);
            }
        }

        void SnapAndRefresh()
        {
            SnapshotInventory();
            RefreshHeader();
            RefreshCardStocks(); // << update angka kartu setiap refresh
            RecalcPreview();
        }

        // === Handler tombol card ===
        void OnCardDelta(PrepCardView view, int delta)
        {
            if (!view || !view.Item) return;

            // MODE: langsung craft / uncraft
            if (craftInstantly && procurement)
            {
                if (delta > 0)
                {
                    if (procurement.CraftByItem(view.Item, 1))
                    {
                        int cur = _plan.TryGetValue(view.Item, out var v) ? v : 0;
                        _plan[view.Item] = cur + 1;
                        view.SetPlanned(cur + 1);
                        SnapAndRefresh();
                    }
                    else if (verboseLog) Debug.LogWarning("[Prep] Craft gagal (bahan kurang?)");
                    return;
                }
                else
                {
                    if (_plan.TryGetValue(view.Item, out var cur) && cur > 0)
                    {
                        bool ok = procurement.UncraftByItem(view.Item, 1, refundMaterials: true);
                        if (ok)
                        {
                            _plan[view.Item] = cur - 1;
                            view.SetPlanned(cur - 1);
                            SnapAndRefresh();
                        }
                        else if (verboseLog) Debug.LogWarning("[Prep] Uncraft gagal (stok item 0?)");
                    }
                    else
                    {
                        // walau plan 0, tetap coba uncraft → Snap akan sync jumlah
                        if (procurement.UncraftByItem(view.Item, 1, refundMaterials: true))
                            SnapAndRefresh();
                    }
                    return;
                }
            }

            // MODE: planner (tidak memodifikasi stok sekarang)
            int c = _plan.TryGetValue(view.Item, out var val) ? val : 0;
            c = Mathf.Max(0, c + (delta > 0 ? +1 : -1));
            _plan[view.Item] = c;
            view.SetPlanned(c);
            RecalcPreview();
        }

        // Commit untuk mode planner (craftInstantly=false)
        public void CommitCraft()
        {
            if (!procurement) return;

            foreach (var kv in _plan)
            {
                var item = kv.Key; int qty = kv.Value;
                if (qty > 0) procurement.CraftByItem(item, qty);
            }

            foreach (var c in topCards) if (c) c.SetPlanned(0);
            foreach (var c in bottomCards) if (c) c.SetPlanned(0);
            _plan.Clear();

            SnapAndRefresh();
            BuildFromCards();
        }
    }
}
