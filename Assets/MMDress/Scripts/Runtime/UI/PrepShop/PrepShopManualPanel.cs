using System;
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
    /// <summary>
    /// Panel manual untuk perencanaan/crafting material & item.
    /// Aman-null untuk semua referensi UI (boleh kosong), dan material SO opsional.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PrepShopManualPanel : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Refs")]
        [SerializeField] private ProcurementService procurement;

        [Header("Top Cards (maks 5)")]
        [SerializeField] private PrepCardView[] topCards = new PrepCardView[5];

        [Header("Bottom Cards (maks 5)")]
        [SerializeField] private PrepCardView[] bottomCards = new PrepCardView[5];

        [Header("Header UI (opsional)")]
        [SerializeField] private TMP_Text clothText;
        [SerializeField] private TMP_Text threadText;
        [SerializeField] private TMP_Text moneyText;

        [Header("Footer Preview (opsional)")]
        [SerializeField] private TMP_Text needClothText;
        [SerializeField] private TMP_Text needThreadText;
        [SerializeField] private TMP_Text warningText;

        [Header("Material SO Refs (opsional)")]
        [SerializeField] private MaterialSO clothMaterial;
        [SerializeField] private MaterialSO threadMaterial;

        [Header("Behaviour")]
        [SerializeField] private bool craftInstantly = true;

        [Header("Debug")]
        [SerializeField] private bool verboseLog = false;
        #endregion

        #region State
        private readonly Dictionary<ItemSO, int> _plan = new();

        private int _snapCloth;
        private int _snapThread;
        private int _snapMoney;

        private Action<SvcPurchaseSucceeded> _onBuyOK;
        private Action<SvcPurchaseFailed> _onBuyFail;
        private Action<SvcCraftSucceeded> _onCraftOK;
        private Action<SvcCraftFailed> _onCraftFail;
        private Action<MoneyChanged> _onMoney;
        #endregion

        #region Unity Lifecycle
        private void Reset()
        {
#if UNITY_2023_1_OR_NEWER
            procurement ??= FindAnyObjectByType<ProcurementService>(FindObjectsInactive.Include);
#else
            procurement ??= FindObjectOfType<ProcurementService>(true);
#endif
        }

        private void Awake()
        {
            // Tidak ada inisialisasi berat di sini, biarkan OnEnable yang urus.
        }

        private void Start()
        {
            BuildFromCards();
            SnapAndRefresh();
        }

        private void OnEnable()
        {
            BuildFromCards();
            HookEvents();
            SnapAndRefresh();
        }

        private void OnDisable()
        {
            UnhookEvents();
        }
        #endregion

        #region Event Wiring
        private void HookEvents()
        {
            // Hindari double-subscribe
            UnhookEvents();

            _onBuyOK = _ => { VLog("[Prep] PurchaseSucceeded"); SnapAndRefresh(); };
            _onBuyFail = e => { VWarn($"[Prep] PurchaseFailed: {e.reason}"); };
            _onCraftOK = _ => { VLog("[Prep] CraftSucceeded"); SnapAndRefresh(); };
            _onCraftFail = e => { VWarn($"[Prep] CraftFailed: {e.reason}"); };

            ServiceLocator.Events.Subscribe(_onBuyOK);
            ServiceLocator.Events.Subscribe(_onBuyFail);
            ServiceLocator.Events.Subscribe(_onCraftOK);
            ServiceLocator.Events.Subscribe(_onCraftFail);

            _onMoney = e =>
            {
                SafeSetText(moneyText, $"Rp {e.balance:N0}");
                VLog($"[Prep] MoneyChanged amount={e.amount} balance={e.balance}");
            };
            ServiceLocator.Events.Subscribe(_onMoney);
        }

        private void UnhookEvents()
        {
            if (_onBuyOK != null) ServiceLocator.Events.Unsubscribe(_onBuyOK);
            if (_onBuyFail != null) ServiceLocator.Events.Unsubscribe(_onBuyFail);
            if (_onCraftOK != null) ServiceLocator.Events.Unsubscribe(_onCraftOK);
            if (_onCraftFail != null) ServiceLocator.Events.Unsubscribe(_onCraftFail);
            if (_onMoney != null) ServiceLocator.Events.Unsubscribe(_onMoney);

            _onBuyOK = null; _onBuyFail = null; _onCraftOK = null; _onCraftFail = null; _onMoney = null;
        }
        #endregion

        #region Build & Snapshot
        private void BuildFromCards()
        {
            // Lepas handler lama jika ada
            ForEachCard(c => c.OnDelta -= OnCardDelta);

            _plan.Clear();

            // Wire kartu aktif + siapkan plan 0
            ForEachCard(WireCard);

            // Tampilkan stok aktual di kartu saat awal
            RefreshCardStocks();
        }

        private void WireCard(PrepCardView c)
        {
            if (!c || !c.Item) return;

            c.SetPlanned(0);
            c.OnDelta += OnCardDelta;

            if (!_plan.ContainsKey(c.Item))
                _plan.Add(c.Item, 0);
        }

        private void SnapshotInventory()
        {
            if (procurement)
            {
                _snapCloth = clothMaterial ? procurement.GetMaterial(clothMaterial) : 0;
                _snapThread = threadMaterial ? procurement.GetMaterial(threadMaterial) : 0;
                _snapMoney = procurement.TryGetEconomy(out var eco) ? eco.Balance : 0;
            }
            else
            {
                _snapCloth = _snapThread = _snapMoney = 0;
            }

            VLog($"[Prep] Snapshot cloth={_snapCloth} thread={_snapThread} money={_snapMoney}");
        }
        #endregion

        #region UI Refresh
        private void RefreshHeader()
        {
            SafeSetText(clothText, _snapCloth.ToString());
            SafeSetText(threadText, _snapThread.ToString());
            SafeSetText(moneyText, $"Rp {_snapMoney:N0}");
        }

        /// <summary>Perbarui angka stok ItemSO aktual pada tiap kartu.</summary>
        private void RefreshCardStocks()
        {
            if (!procurement) return;

            ForEachCard(c =>
            {
                if (!c || !c.Item) return;
                int stock = procurement.GetGarment(c.Item);
                c.SetPlanned(stock);
            });
        }

        private void RecalcPreview()
        {
            int planned = TotalPlanned();
            int needCloth = Mathf.Max(0, planned - _snapCloth);
            int needThread = Mathf.Max(0, planned - _snapThread);

            SafeSetText(needClothText, needCloth.ToString());
            SafeSetText(needThreadText, needThread.ToString());

            bool ok = planned <= AvailablePairs();
            if (warningText)
            {
                warningText.text = ok ? string.Empty : "Bahan tidak cukup (1 kain + 1 benang / item).";
                warningText.color = ok ? new Color(1, 1, 1, 0) : new Color(1f, 0.3f, 0.3f, 1f);
            }
        }

        private void SnapAndRefresh()
        {
            SnapshotInventory();
            RefreshHeader();
            RefreshCardStocks();
            RecalcPreview();
        }
        #endregion

        #region Card Handlers
        private void OnCardDelta(PrepCardView view, int delta)
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
                    else
                    {
                        VWarn("[Prep] Craft gagal (bahan kurang?)");
                    }
                    return;
                }
                else // delta <= 0
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
                        else VWarn("[Prep] Uncraft gagal (stok item 0?)");
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

        /// <summary>Commit untuk mode planner (craftInstantly=false).</summary>
        public void CommitCraft()
        {
            if (!procurement) return;

            foreach (var kv in _plan)
            {
                var item = kv.Key; int qty = kv.Value;
                if (qty > 0) procurement.CraftByItem(item, qty);
            }

            // Reset tampilan kartu & plan
            ForEachCard(c => { if (c) c.SetPlanned(0); });
            _plan.Clear();

            SnapAndRefresh();
            BuildFromCards();
        }
        #endregion

        #region Helpers
        private static void SafeSetText(TMP_Text label, string value)
        {
            if (!label) return;                  // label opsional (boleh kosong)
            label.text = value ?? string.Empty;  // null → string.Empty
        }

        private void ForEachCard(Action<PrepCardView> action)
        {
            if (action == null) return;

            if (topCards != null)
                for (int i = 0; i < topCards.Length; i++)
                    action(topCards[i]);

            if (bottomCards != null)
                for (int i = 0; i < bottomCards.Length; i++)
                    action(bottomCards[i]);
        }

        private int TotalPlanned()
        {
            int t = 0;
            foreach (var kv in _plan) t += kv.Value;
            return t;
        }

        private int AvailablePairs() => Mathf.Min(_snapCloth, _snapThread);

        private void VLog(string msg) { if (verboseLog) Debug.Log(msg); }
        private void VWarn(string msg) { if (verboseLog) Debug.LogWarning(msg); }
        #endregion
    }
}
