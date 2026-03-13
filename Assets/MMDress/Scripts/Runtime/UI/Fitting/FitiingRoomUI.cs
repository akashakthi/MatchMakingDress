using UnityEngine;
using UnityEngine.UI;
using MMDress.Core;
using MMDress.Gameplay;
using MMDress.Data;
using MMDress.Services;
using MMDress.Customer;

namespace MMDress.UI
{
    [DisallowMultipleComponent]
    [AddComponentMenu("MMDress/UI/Fitting Room UI")]
    public sealed class FittingRoomUI : MonoBehaviour
    {
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
        [SerializeField] private bool forbidOutOfStockSelection = true;

        private CustomerController _current;
        private ItemSO _equippedTop;
        private ItemSO _equippedBottom;
        private ItemSO _previewTop;
        private ItemSO _previewBottom;
        private CanvasGroup _cg;

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

        public void Open(CustomerController target)
        {
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

            ServiceLocator.Events?.Publish(new FittingUIOpened());
            UpdateEquipButton();
        }

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
            if (c != _current) return;
            ForceCloseOnTimeout();
        }

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

        private bool HasStock(ItemSO item)
        {
            if (!item) return false;
            if (!stock) return true;
            return stock.GetGarment(item) > 0;
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
                if (!topTaken)
                    return false;
            }

            if (hasBottom)
            {
                bool bottomTaken = stock.TryUncraft(_equippedBottom, 1, refundMaterials: false);
                if (!bottomTaken)
                {
                    if (topTaken)
                        stock.TryCraft(_equippedTop, 1); // rollback top
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

            bool topOk = reqTop != null && _equippedTop == reqTop;
            bool bottomOk = reqBottom != null && _equippedBottom == reqBottom;

            return topOk && bottomOk;
        }

        private void ShowTab(OutfitSlot slot)
        {
            if (listView)
            {
                listView.SetCatalog(catalog);
                listView.SetSlot(slot);

                var selected =
                    slot == OutfitSlot.Top
                        ? (_previewTop ?? _equippedTop)
                        : (_previewBottom ?? _equippedBottom);

                listView.Refresh(selected);
            }

            RefreshPreviewFromState();

            if (tabTopButton) tabTopButton.interactable = slot != OutfitSlot.Top;
            if (tabBottomButton) tabBottomButton.interactable = slot != OutfitSlot.Bottom;

            UpdateEquipButton();
        }

        private void UpdateEquipButton()
        {
            if (!equipButton) return;

            bool topChanged = _previewTop != null && _previewTop != _equippedTop;
            bool bottomChanged = _previewBottom != null && _previewBottom != _equippedBottom;

            bool topStockOk = _previewTop == null || !forbidOutOfStockSelection || HasStock(_previewTop);
            bool bottomStockOk = _previewBottom == null || !forbidOutOfStockSelection || HasStock(_previewBottom);

            equipButton.interactable = (topChanged || bottomChanged) && topStockOk && bottomStockOk;
        }

        private void OnItemClicked(ItemSO item)
        {
            if (item == null) return;

            if (forbidOutOfStockSelection && !HasStock(item))
            {
                UpdateEquipButton();
                return;
            }

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
                if (forbidOutOfStockSelection && !HasStock(_previewTop))
                {
                    UpdateEquipButton();
                    return;
                }

                _equippedTop = _previewTop;

                if (_current != null)
                    OutfitEquippedCommitted.Publish(ServiceLocator.Events, _current, OutfitSlot.Top, _equippedTop);
            }

            if (_previewBottom != null && _previewBottom != _equippedBottom)
            {
                if (forbidOutOfStockSelection && !HasStock(_previewBottom))
                {
                    UpdateEquipButton();
                    return;
                }

                _equippedBottom = _previewBottom;

                if (_current != null)
                    OutfitEquippedCommitted.Publish(ServiceLocator.Events, _current, OutfitSlot.Bottom, _equippedBottom);
            }

            _previewTop = null;
            _previewBottom = null;

            RefreshPreviewFromState();
            UpdateEquipButton();

            InternalClose();
        }

        private void CloseInternalLogic()
        {
            int equippedCount = (_equippedTop ? 1 : 0) + (_equippedBottom ? 1 : 0);

            bool success = false;
            if (equippedCount == 2)
                success = TryConsumeEquippedFromStock();

            bool isCorrectOrder = success && IsCorrectOrder();

            if (_current != null)
            {
                int finalCount = success ? equippedCount : 0;
                bool finalCorrect = success && isCorrectOrder;
                _current.FinishFitting(finalCount, finalCorrect);
            }

            ServiceLocator.Events?.Publish(new FittingUIClosed());

            DetachCurrentCustomer();
            _current = null;
            _previewTop = null;
            _previewBottom = null;
            _equippedTop = null;
            _equippedBottom = null;
            SetVisible(false);
        }

        public void InternalClose() => CloseInternalLogic();
        public void Close() => InternalClose();

        private void ForceCloseOnTimeout()
        {
            ServiceLocator.Events?.Publish(new FittingUIClosed());

            DetachCurrentCustomer();
            _current = null;
            _previewTop = null;
            _previewBottom = null;
            _equippedTop = null;
            _equippedBottom = null;
            SetVisible(false);
        }
    }
}