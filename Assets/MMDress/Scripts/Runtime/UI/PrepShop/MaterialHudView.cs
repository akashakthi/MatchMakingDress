using UnityEngine;
using TMPro;
using MMDress.Services;
using MMDress.Core;
using MMDress.Data;

// alias event Services (biar ga bentrok)
using SvcPurchaseSucceeded = MMDress.Services.PurchaseSucceeded;
using SvcPurchaseFailed = MMDress.Services.PurchaseFailed;
using SvcCraftSucceeded = MMDress.Services.CraftSucceeded;
using SvcCraftFailed = MMDress.Services.CraftFailed;

namespace MMDress.Runtime.UI.PrepShop
{
    [DisallowMultipleComponent]
    [AddComponentMenu("MMDress/UI/Material HUD View (SO)")]
    public sealed class MaterialHudViewSO : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private ProcurementService procurement;
        [SerializeField] private bool autoFind = true;

        [Header("Materials (SO)")]
        [SerializeField] private MaterialSO cloth;   // assign: Kain
        [SerializeField] private MaterialSO thread;  // assign: Benang

        [Header("UI")]
        [SerializeField] private TMP_Text clothText;
        [SerializeField] private TMP_Text threadText;

        System.Action<SvcPurchaseSucceeded> _onBuyOK;
        System.Action<SvcPurchaseFailed> _onBuyFail;
        System.Action<SvcCraftSucceeded> _onCraftOK;
        System.Action<SvcCraftFailed> _onCraftFail;

        void Awake()
        {
            if (autoFind)
            {
#if UNITY_2023_1_OR_NEWER
                procurement ??= Object.FindAnyObjectByType<ProcurementService>(FindObjectsInactive.Include);
#else
                procurement ??= FindObjectOfType<ProcurementService>(true);
#endif
            }
        }

        void OnEnable()
        {
            Refresh();
            _onBuyOK = _ => Refresh();
            _onBuyFail = _ => Refresh();
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
            if (!procurement) { Render(0, 0); return; }
            int c = procurement.GetMaterial(cloth);
            int t = procurement.GetMaterial(thread);
            Render(c, t);
        }

        void Render(int c, int t)
        {
            if (clothText) clothText.text = c.ToString();
            if (threadText) threadText.text = t.ToString();
        }
    }
}
