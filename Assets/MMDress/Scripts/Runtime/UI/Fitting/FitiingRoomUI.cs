using UnityEngine;
using UnityEngine.UI;
using MMDress.Core;
using MMDress.Gameplay;          // FittingUIOpened, FittingUIClosed
using MMDress.Data;
using MMDress.UI;                // CharacterOutfitController
using MMDress.Services;
using MMDress.Runtime.Inventory;
using MMDress.Customer;          // CustomerController, CustomerOrder

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
        [SerializeField] private StockService stock;

        [Header("List (Horizontal)")]
        [SerializeField] private ItemGridView listView;

        [Header("Preview (UI)")]
        [SerializeField] private CharacterOutfitController preview;

        [Header("Buttons")]
        [SerializeField] private Button tabTopButton;
        [SerializeField] private Button tabBottomButton;
        [SerializeField] private Button equipButton;
        [SerializeField] private Button closeButton;

        [Header("Options")]
        [SerializeField] private bool autoFindInChildren = true;

        #endregion

        #region Runtime state

        private CustomerController _current;
        private OutfitSlot _activeTab = OutfitSlot.Top;

        private ItemSO _equippedTop;
        private ItemSO _equippedBottom;

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

        private void OnDisable()
        {
            DetachCurrentCustomer();
            _current = null;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Dipanggil ketika customer diklik (event CustomerSelected → flow kamu).
        /// </summary>
        public void Open(CustomerController target)
        {
            // lepas dulu kalau sebelumnya sudah terhubung ke customer lain
            DetachCurrentCustomer();

            _current = target;
            AttachToCurrentCustomer();

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

        #region Customer event wiring

        private void AttachToCurrentCustomer()
        {
            if (_current == null) return;
            _current.OnTimedOut += OnCurrentTimedOut;
        }

        private void DetachCurrentCustomer()
        {
            if (_current == null) return;
            _current.OnTimedOut -= OnCurrentTimedOut;
        }

        private void OnCurrentTimedOut(CustomerController c)
        {
            // ini pasti customer yang sama, tapi cek lagi biar aman
            if (c != _current) return;

            // Tutup paksa panel TANPA FinishFitting (checkout sudah dihandle di CustomerController)
            ForceCloseOnTimeout();
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
                panelRoot.SetActive(on);
        }

        private void RefreshPreviewFromState()
        {
            if (!preview) return;

            var visualTop = _previewTop ?? _equippedTop;
            var visualBottom = _previewBottom ?? _equippedBottom;

            preview.ApplyEquipped(visualTop, visualBottom);
        }

        private bool TryConsumeEquippedFromStock()
        {
            if (!stock)
                return true;

            bool hasTop = _equippedTop != null;
            bool hasBottom = _equippedBottom != null;

            bool topTaken = false;

            if (hasTop)
            {
                topTaken = stock.TryUncraft(_equippedTop, 1, refundMaterials: false);
                if (!topTaken) return false;
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

        private bool IsCorrectOrder()
        {
            if (_current == null)
                return false;

            var orderHolder = _current.GetComponent<CustomerOrder>();

            ItemSO reqTop = null;
            ItemSO reqBottom = null;

            if (orderHolder != null && orderHolder.HasOrder)
            {
                reqTop = orderHolder.RequiredTop;
                reqBottom = orderHolder.RequiredBottom;
            }
            else
            {
                reqTop = _current.RequestedTop;
                reqBottom = _current.RequestedBottom;
            }

            bool topOk = (reqTop == null) || (_equippedTop == reqTop);
            bool bottomOk = (reqBottom == null) || (_equippedBottom == reqBottom);
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

        #region Callbacks (klik item & konfirmasi)

        private void OnItemClicked(ItemSO item)
        {
            if (item == null) return;

            if (item.slot == OutfitSlot.Top)
                _previewTop = item;
            else if (item.slot == OutfitSlot.Bottom)
                _previewBottom = item;

            OutfitPreviewChanged.Publish(ServiceLocator.Events, _current, item.slot, item);

            RefreshPreviewFromState();
            UpdateEquipButton();
        }

        private void EquipPreview()
        {
            if (_previewTop != null && _previewTop != _equippedTop)
            {
                _equippedTop = _previewTop;

                if (_current != null)
                {
                    OutfitEquippedCommitted.Publish(
                        ServiceLocator.Events, _current, OutfitSlot.Top, _equippedTop);
                }
            }

            if (_previewBottom != null && _previewBottom != _equippedBottom)
            {
                _equippedBottom = _previewBottom;

                if (_current != null)
                {
                    OutfitEquippedCommitted.Publish(
                        ServiceLocator.Events, _current, OutfitSlot.Bottom, _equippedBottom);
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
                success = TryConsumeEquippedFromStock();

            bool isCorrectOrder = false;

            if (success && equippedCount > 0)
                isCorrectOrder = IsCorrectOrder();

            if (_current != null)
            {
                int finalCount = success ? equippedCount : 0;
                bool finalCorrect = success && isCorrectOrder;

                _current.FinishFitting(finalCount, finalCorrect);
            }

            ServiceLocator.Events.Publish(new FittingUIClosed());

            DetachCurrentCustomer();
            _current = null;

            _previewTop = _previewBottom = null;
            _equippedTop = _equippedBottom = null;

            SetVisible(false);
        }

        public void InternalClose() => CloseInternalLogic();

        public void Close() => InternalClose();

        #endregion

        #region Auto close on timeout (from current customer)

        private void ForceCloseOnTimeout()
        {
            // Di sini *tidak* panggil FinishFitting.
            // CustomerController sudah publish Checkout(0,false) + CustomerTimedOut.
            ServiceLocator.Events.Publish(new FittingUIClosed());

            DetachCurrentCustomer();
            _current = null;

            _previewTop = _previewBottom = null;
            _equippedTop = _equippedBottom = null;

            SetVisible(false);
        }

        #endregion
    }
}
