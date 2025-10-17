// Assets/MMDress/Scripts/Runtime/UI/PrepShop/MaterialHudView.cs
using UnityEngine;
using TMPro;
using MMDress.Services;
using MMDress.Runtime.Inventory;
using MMDress.Core;

// alias biar pendek
using InvMaterialType = MMDress.Runtime.Inventory.MaterialType;

namespace MMDress.Runtime.UI.PrepShop
{
    [DisallowMultipleComponent]
    [AddComponentMenu("MMDress/UI/Material HUD View (Kain & Benang)")]
    public sealed class MaterialHudView : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private ProcurementService procurement;   // drag dari [_Services]
        [SerializeField] private bool autoFind = true;

        [Header("UI")]
        [SerializeField] private TMP_Text clothText;   // label angka Kain
        [SerializeField] private TMP_Text threadText;  // label angka Benang

        // simpan delegate agar bisa unsubscribe
        System.Action<PurchaseSucceeded> _onBuyOK;
        System.Action<PurchaseFailed> _onBuyFail;
        System.Action<CraftSucceeded> _onCraftOK;
        System.Action<CraftFailed> _onCraftFail;

        void Awake()
        {
            if (autoFind)
            {
                procurement ??= FindObjectOfType<ProcurementService>(true);
                if (!clothText || !threadText)
                    GetComponentInChildrenSafe();
            }
        }

        void OnEnable()
        {
            Refresh();

            // apa pun yang mengubah stok → refresh
            _onBuyOK = _ => Refresh();
            _onBuyFail = _ => Refresh();     // optional, jaga-jaga UI tetap sinkron
            _onCraftOK = _ => Refresh();
            _onCraftFail = _ => Refresh();

            ServiceLocator.Events.Subscribe(_onBuyOK);
            ServiceLocator.Events.Subscribe(_onBuyFail);
            ServiceLocator.Events.Subscribe(_onCraftOK);
            ServiceLocator.Events.Subscribe(_onCraftFail);
        }

        void OnDisable()
        {
            if (_onBuyOK != null) ServiceLocator.Events.Unsubscribe(_onBuyOK);
            if (_onBuyFail != null) ServiceLocator.Events.Unsubscribe(_onBuyFail);
            if (_onCraftOK != null) ServiceLocator.Events.Unsubscribe(_onCraftOK);
            if (_onCraftFail != null) ServiceLocator.Events.Unsubscribe(_onCraftFail);
        }

        public void Refresh()
        {
            if (!procurement)
            {
                if (autoFind) procurement = FindObjectOfType<ProcurementService>(true);
                if (!procurement) { Render(0, 0); return; }
            }

            int cloth = procurement.GetMaterial(InvMaterialType.Cloth);
            int thread = procurement.GetMaterial(InvMaterialType.Thread);
            Render(cloth, thread);
        }

        void Render(int cloth, int thread)
        {
            if (clothText) clothText.text = cloth.ToString();
            if (threadText) threadText.text = thread.ToString();
        }

        // cari TMP_Text di anak2 jika belum di-assign
        void GetComponentInChildrenSafe()
        {
            var texts = GetComponentsInChildren<TMP_Text>(true);
            if (texts != null && texts.Length >= 2)
            {
                // biar gampang: ambil dua pertama kalau kosong
                if (!clothText) clothText = texts[0];
                if (!threadText) threadText = texts[1];
            }
        }
    }
}
