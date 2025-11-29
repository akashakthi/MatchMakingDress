// Assets/MMDress/Scripts/Runtime/UI/Fitting/FittingRoomUI.cs
using UnityEngine;
using UnityEngine.UI;
using MMDress.Core;
using MMDress.Gameplay;
using MMDress.Data;
using MMDress.UI; // CharacterOutfitController
using MMDress.Services;
using MMDress.Runtime.Inventory;

namespace MMDress.UI
{
    [DisallowMultipleComponent]
    [AddComponentMenu("MMDress/UI/Fitting Room UI")]
    public sealed class FittingRoomUI : MonoBehaviour
    {
        #region Inspector

        [Header("Panel Root")]
        [SerializeField] private GameObject panelRoot;

        [Header("Data")]
        [SerializeField] private CatalogSO catalog;
        [SerializeField] private StockService stock;   // sinkron ke stok baju

        [Header("List (Horizontal)")]
        [SerializeField] private ItemGridView listView;

        [Header("Preview (UI)")]
        [SerializeField] private CharacterOutfitController preview;

        [Header("Buttons")]
        [SerializeField] private Button tabTopButton;
        [SerializeField] private Button tabBottomButton;
        [SerializeField] private Button equipButton;
        [SerializeField] private Button closeButton;   // opsional

        [Header("Options")]
        [SerializeField] private bool autoFindInChildren = true;

        #endregion

        #region Runtime state

        // pakai namespace lengkap biar nggak butuh alias
        private MMDress.Customer.CustomerController _current;
        private OutfitSlot _activeTab = OutfitSlot.Top;

        // State equip final (sudah dikonfirmasi)
        private ItemSO _equippedTop;
        private ItemSO _equippedBottom;

        // State preview sementara (belum dikonfirmasi)
        private ItemSO _previewTop;
        private ItemSO _previewBottom;

        private CanvasGroup _cg;

        #endregion

        #region Unity

        private void Awake()
        {
            _cg = GetComponent<CanvasGroup>();
            if (!_cg) _cg = gameObject.AddComponent<CanvasGroup>();

            if (autoFindInChildren && !listView)
                listView = GetComponentInChildren<ItemGridView>(true);

            if (tabTopButton) tabTopButton.onClick.AddListener(() => ShowTab(OutfitSlot.Top));
            if (tabBottomButton) tabBottomButton.onClick.AddListener(() => ShowTab(OutfitSlot.Bottom));

            if (equipButton) equipButton.onClick.AddListener(EquipPreview);

            if (closeButton) closeButton.onClick.AddListener(InternalClose);

            if (listView != null)
                listView.OnItemSelected = OnItemClicked;

            SetVisible(false);
        }

        #endregion

        #region Public API

        public void Open(MMDress.Customer.CustomerController target)
        {
            _current = target;

            _equippedTop = null;
            _equippedBottom = null;
            _previewTop = null;
            _previewBottom = null;

            if (preview) preview.Clear();

            SetVisible(true);
            ShowTab(OutfitSlot.Top);

            ServiceLocator.Events.Publish(new FittingUIOpened());
            UpdateEquipButton();
        }

        #endregion

        #region UI Helpers

        private void SetVisible(bool on)
        {
            if (_cg)
            {
                _cg.alpha = on ? 1f : 0f;
                _cg.interactable = on;
                _cg.blocksRaycasts = on;
            }

            if (panelRoot)
                panelRoot.SetActive(true); // panel tetap hidup, cuma di-alpha
        }

        /// <summary>
        /// Kombinasi visual: preview kalau ada, kalau tidak pakai equipped.
        /// </summary>
        private void RefreshPreviewFromState()
        {
            if (!preview) return;

            var visualTop = _previewTop ?? _equippedTop;
            var visualBottom = _previewBottom ?? _equippedBottom;

            preview.ApplyEquipped(visualTop, visualBottom);
        }

        /// <summary>
        /// Cek stok dan konsumsi baju yang sudah di-equip (final).
        /// </summary>
        private bool TryConsumeEquippedFromStock()
        {
            if (!stock)
                return true; // kalau StockService belum di-set, jangan blokir

            bool hasTop = _equippedTop != null;
            bool hasBottom = _equippedBottom != null;

            bool topTaken = false;

            if (hasTop)
            {
                topTaken = stock.TryUncraft(_equippedTop, 1, refundMaterials: false);
                if (!topTaken)
                    return false;
            }

            if (hasBottom)
            {
                bool bottomTaken = stock.TryUncraft(_equippedBottom, 1, refundMaterials: false);
                if (!bottomTaken)
                {
                    if (topTaken)
                        stock.TryCraft(_equippedTop, 1); // rollback

                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Cek apakah outfit final sesuai pesanan.
        /// Prioritas:
        /// 1) CustomerOrder (OrderSO)
        /// 2) fallback ke RequestedTop/Bottom di CustomerController (kalau ada).
        /// </summary>
        private bool IsCorrectOrder()
        {
            if (_current == null)
                return false;

            // 1) Cari holder order
            var orderHolder = _current.GetComponent<MMDress.Customer.CustomerOrder>();

            ItemSO reqTop = null;
            ItemSO reqBottom = null;

            if (orderHolder != null && orderHolder.HasOrder)
            {
                reqTop = orderHolder.RequiredTop;
                reqBottom = orderHolder.RequiredBottom;
            }
            else
            {
                // fallback ke field lama (kalau masih dipakai)
                reqTop = _current.RequestedTop;
                reqBottom = _current.RequestedBottom;
            }

            bool topOk =
                (reqTop == null) || (_equippedTop == reqTop);

            bool bottomOk =
                (reqBottom == null) || (_equippedBottom == reqBottom);

            bool hasBoth = (_equippedTop != null && _equippedBottom != null);

            return hasBoth && topOk && bottomOk;
        }

        private void ShowTab(OutfitSlot slot)
        {
            _activeTab = slot;

            if (listView)
            {
                listView.SetCatalog(catalog);
                listView.SetSlot(slot);

                var selected =
                    (slot == OutfitSlot.Top)
                        ? (_previewTop ?? _equippedTop)
                        : (_previewBottom ?? _equippedBottom);

                listView.Refresh(selected);
            }

            RefreshPreviewFromState();

            if (tabTopButton) tabTopButton.interactable = (slot != OutfitSlot.Top);
            if (tabBottomButton) tabBottomButton.interactable = (slot != OutfitSlot.Bottom);

            UpdateEquipButton();
        }

        private void UpdateEquipButton()
        {
            if (!equipButton) return;

            bool topChanged = _previewTop != null && _previewTop != _equippedTop;
            bool bottomChanged = _previewBottom != null && _previewBottom != _equippedBottom;

            equipButton.interactable = topChanged || bottomChanged;
        }

        #endregion

        #region Callbacks

        private void OnItemClicked(ItemSO item)
        {
            if (item == null) return;

            if (item.slot == OutfitSlot.Top)
            {
                _previewTop = item;
            }
            else if (item.slot == OutfitSlot.Bottom)
            {
                _previewBottom = item;
            }

            OutfitPreviewChanged.Publish(ServiceLocator.Events, _current, item.slot, item);

            RefreshPreviewFromState();
            UpdateEquipButton();
        }

        /// <summary>
        /// Equip = commit semua preview ke equipped + kirim ke customer + close.
        /// </summary>
        private void EquipPreview()
        {
            if (_previewTop != null && _previewTop != _equippedTop)
            {
                _equippedTop = _previewTop;

                if (_current != null)
                {
                    OutfitEquippedCommitted.Publish(
                        ServiceLocator.Events, _current, OutfitSlot.Top, _equippedTop
                    );
                }
            }

            if (_previewBottom != null && _previewBottom != _equippedBottom)
            {
                _equippedBottom = _previewBottom;

                if (_current != null)
                {
                    OutfitEquippedCommitted.Publish(
                        ServiceLocator.Events, _current, OutfitSlot.Bottom, _equippedBottom
                    );
                }
            }

            _previewTop = null;
            _previewBottom = null;

            RefreshPreviewFromState();
            UpdateEquipButton();

            InternalClose();
        }

        private void CloseInternalLogic()
        {
            int equippedCount =
                (_equippedTop ? 1 : 0) +
                (_equippedBottom ? 1 : 0);

            bool success = true;
            if (equippedCount > 0)
            {
                success = TryConsumeEquippedFromStock();
            }

            bool isCorrectOrder = false;

            if (success && equippedCount > 0)
            {
                isCorrectOrder = IsCorrectOrder();
            }

            if (_current != null)
            {
                int finalCount = success ? equippedCount : 0;
                bool finalCorrect = success && isCorrectOrder;

                _current.FinishFitting(finalCount, finalCorrect);
            }

            ServiceLocator.Events.Publish(new FittingUIClosed());

            _current = null;
            _previewTop = null;
            _previewBottom = null;

            SetVisible(false);
        }

        public void InternalClose()
        {
            CloseInternalLogic();
        }

        public void Close()
        {
            InternalClose();
        }

        #endregion
    }
}
