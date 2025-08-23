using UnityEngine;
using UnityEngine.UI;
using MMDress.Core;
using MMDress.Gameplay;
using MMDress.Data;

namespace MMDress.UI
{
    /// Panel UI Fitting: buka saat customer dipilih, populasi grid Top/Bottom,
    /// klik item -> Preview (TryOn), tombol Equip -> pasang permanen.
    public class FittingRoomUI : MonoBehaviour
    {
        [Header("Panel Root")]
        [SerializeField] private GameObject panelRoot;

        [Header("Data")]
        [SerializeField] private CatalogSO catalog;

        [Header("Grids")]
        [SerializeField] private ItemGridView topGrid;
        [SerializeField] private ItemGridView bottomGrid;

        [Header("Buttons")]
        [SerializeField] private Button tabTopButton;
        [SerializeField] private Button tabBottomButton;
        [SerializeField] private Button equipButton;
        [SerializeField] private Button closeButton;

        Customer.CustomerController _current;
        ItemSO _previewItem;

        void Awake()
        {
            if (panelRoot) panelRoot.SetActive(false);
            if (tabTopButton) tabTopButton.onClick.AddListener(() => ShowTab(OutfitSlot.Top));
            if (tabBottomButton) tabBottomButton.onClick.AddListener(() => ShowTab(OutfitSlot.Bottom));
            if (equipButton) equipButton.onClick.AddListener(EquipPreview);
            if (closeButton) closeButton.onClick.AddListener(Close);

            if (topGrid) topGrid.OnItemSelected = OnItemClicked;
            if (bottomGrid) bottomGrid.OnItemSelected = OnItemClicked;

            ServiceLocator.Events.Subscribe<CustomerSelected>(OnSelected);
        }
        void OnDestroy()
        {
            ServiceLocator.Events.Unsubscribe<CustomerSelected>(OnSelected);
        }

        void OnSelected(CustomerSelected e)
        {
            _current = e.customer;
            _previewItem = null;

            if (panelRoot) panelRoot.SetActive(true);
            ServiceLocator.Events.Publish(new FittingUIOpened());

            // isi katalog ke grid
            if (topGrid) { topGrid.SetCatalog(catalog); topGrid.Refresh(); }
            if (bottomGrid) { bottomGrid.SetCatalog(catalog); bottomGrid.Refresh(); }

            ShowTab(OutfitSlot.Top);
            Debug.Log($"[MMDress] Catalog items: {catalog?.items?.Count}");

        }

        void ShowTab(OutfitSlot slot)
        {
            bool top = slot == OutfitSlot.Top;
            if (topGrid) topGrid.gameObject.SetActive(top);
            if (bottomGrid) bottomGrid.gameObject.SetActive(!top);
        }

        void OnItemClicked(ItemSO item)
        {
            _previewItem = item;
            _current?.Outfit.TryOn(item); // preview
        }

        void EquipPreview()
        {
            if (_current == null || _previewItem == null) return;
            _current.Outfit.Equip(_previewItem);
        }

        public void Close()
        {
            if (_current != null)
            {
                _current.Outfit.RevertPreview();
                _current.FinishFitting(); // lanjut Leaving
            }
            ServiceLocator.Events.Publish(new FittingUIClosed());
            _current = null;
            _previewItem = null;
            if (panelRoot) panelRoot.SetActive(false);
        }
    }
}
