using System.Collections.Generic;
using UnityEngine;
using TMPro;
using MMDress.Data;
using MMDress.Services;
using InvMaterialType = MMDress.Runtime.Inventory.MaterialType;

namespace MMDress.Runtime.UI.PrepShop
{
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

        [Header("Behaviour")]
        [SerializeField] bool craftInstantly = true; // <-- nyalain ini supaya + langsung craft

        readonly Dictionary<ItemSO, int> _plan = new();

        int _snapCloth, _snapThread, _snapMoney;

        void Reset()
        {
            if (!procurement) procurement = FindObjectOfType<ProcurementService>(true);
        }

        void Awake()
        {
            BuildFromCards();
            SnapshotInventory();
            RefreshHeader();
            RecalcPreview();
        }

        void OnEnable()
        {
            // (penting kalau panel diaktif/non-aktif)
            BuildFromCards();              // pastikan subscribe ulang
            SnapshotInventory();
            RefreshHeader();
            RecalcPreview();
        }

        void BuildFromCards()
        {
            // Unsubscribe lama biar nggak dobel
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
        }

        void SnapshotInventory()
        {
            _snapCloth = procurement ? procurement.GetMaterial(InvMaterialType.Cloth) : 0;
            _snapThread = procurement ? procurement.GetMaterial(InvMaterialType.Thread) : 0;
            _snapMoney = procurement != null && procurement.TryGetEconomy(out var eco) ? eco.Balance : 0;
        }

        void RefreshHeader()
        {
            if (clothText) clothText.text = _snapCloth.ToString();
            if (threadText) threadText.text = _snapThread.ToString();
            if (moneyText) moneyText.text = $"Rp {_snapMoney:N0}";
        }

        int TotalPlanned() { int t = 0; foreach (var kv in _plan) t += kv.Value; return t; }
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

        void OnCardDelta(PrepCardView view, int delta)
        {
            if (!view || !view.Item) return;

            // MODE: langsung craft
            if (craftInstantly && procurement)
            {
                if (delta > 0)
                {
                    // cek bahan cukup?
                    if (_snapCloth <= 0 || _snapThread <= 0) return;
                    if (procurement.CraftByItem(view.Item, 1))
                    {
                        _snapCloth--; _snapThread--; // update snapshot cepat
                        view.SetPlanned(_plan.TryGetValue(view.Item, out var v) ? v + 1 : 1);
                        _plan[view.Item] = _plan.TryGetValue(view.Item, out var vv) ? vv + 1 : 1;
                        RefreshHeader(); // kalau saldo uang terpakai, disini juga bisa diupdate
                        RecalcPreview();
                    }
                    return;
                }
                else
                {
                    // kalau mau support undo (uncraft), panggil UncraftByItem(...), kalau ada
                    if (_plan.TryGetValue(view.Item, out var cur) && cur > 0)
                    {
                        _plan[view.Item] = cur - 1;
                        view.SetPlanned(cur - 1);
                        // opsional: kembalikan bahan
                        _snapCloth++; _snapThread++;
                        RefreshHeader();
                        RecalcPreview();
                    }
                    return;
                }
            }

            // MODE: planner (tidak mengurangi bahan sekarang)
            int c = _plan.TryGetValue(view.Item, out var val) ? val : 0;
            if (delta > 0)
            {
                if (TotalPlanned() >= AvailablePairs()) return; // clamp by bahan
                c += 1;
            }
            else
            {
                if (c <= 0) return;
                c -= 1;
            }
            _plan[view.Item] = c;
            view.SetPlanned(c);
            RecalcPreview();
        }

        // Planner commit (kalau craftInstantly=false pakai tombol ini)
        public void CommitCraft()
        {
            if (!procurement) return;

            foreach (var kv in _plan)
            {
                var item = kv.Key; int qty = kv.Value;
                if (qty > 0) procurement.CraftByItem(item, qty);
            }

            SnapshotInventory();
            RefreshHeader();
            foreach (var c in topCards) if (c) c.SetPlanned(0);
            foreach (var c in bottomCards) if (c) c.SetPlanned(0);
            _plan.Clear();
            BuildFromCards();
            RecalcPreview();
        }
    }
}
