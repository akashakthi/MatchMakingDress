using UnityEngine;
using UnityEngine.UI;
using MMDress.Core;
using MMDress.Gameplay;
using MMDress.Data;

namespace MMDress.UI
{
    /// UI Fitting: buka saat Customer dipilih, pilih item -> Preview,
    /// tekan Equip -> pasang permanen, Close -> revert preview & lanjut leave.
    [DisallowMultipleComponent]
    [AddComponentMenu("MMDress/UI/Fitting Room UI")]
    public class FittingRoomUI : MonoBehaviour
    {
        #region Inspector

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

        [Header("Options")]
        [SerializeField] private bool autoFindInChildren = true;

        #endregion

        #region Runtime state

        Customer.CustomerController _current;
        ItemSO _previewItem;
        OutfitSlot _activeTab = OutfitSlot.Top;

        #endregion

        #region Unity

        void Awake()
        {
            if (autoFindInChildren)
            {
                if (!panelRoot) panelRoot = gameObject;
                if (!topGrid) topGrid = transform.Find("TopGrid")?.GetComponent<ItemGridView>();
                if (!bottomGrid) bottomGrid = transform.Find("BottomGrid")?.GetComponent<ItemGridView>();
                if (!equipButton) equipButton = transform.Find("EquipButton")?.GetComponent<Button>();
                if (!closeButton) closeButton = transform.Find("CloseButton")?.GetComponent<Button>();
                if (!tabTopButton) tabTopButton = transform.Find("TabTopButton")?.GetComponent<Button>();
                if (!tabBottomButton) tabBottomButton = transform.Find("TabBottomButton")?.GetComponent<Button>();
            }

            if (panelRoot) panelRoot.SetActive(false);

            if (tabTopButton) tabTopButton.onClick.AddListener(() => ShowTab(OutfitSlot.Top));
            if (tabBottomButton) tabBottomButton.onClick.AddListener(() => ShowTab(OutfitSlot.Bottom));
            if (equipButton) equipButton.onClick.AddListener(EquipPreview);
            if (closeButton) closeButton.onClick.AddListener(Close);

            if (topGrid) topGrid.OnItemSelected = OnItemClicked;
            if (bottomGrid) bottomGrid.OnItemSelected = OnItemClicked;

            ServiceLocator.Events.Subscribe<CustomerSelected>(OnSelected);
            UpdateEquipButton();
        }

        void OnDestroy()
        {
            ServiceLocator.Events.Unsubscribe<CustomerSelected>(OnSelected);
        }

        #endregion

        #region Event handlers

        void OnSelected(CustomerSelected e)
        {
            _current = e.customer;
            _previewItem = null;

            if (panelRoot) panelRoot.SetActive(true);
            ServiceLocator.Events.Publish(new FittingUIOpened());

            if (topGrid)
            {
                topGrid.SetCatalog(catalog);
                topGrid.Refresh();
            }
            if (bottomGrid)
            {
                bottomGrid.SetCatalog(catalog);
                bottomGrid.Refresh();
            }

            ShowTab(_activeTab);
            UpdateEquipButton();
        }

        void OnItemClicked(ItemSO item)
        {
            _previewItem = item;
            _current?.Outfit.TryOn(item); // preview di karakter
            UpdateEquipButton();
        }

        #endregion

        #region UI logic

        void ShowTab(OutfitSlot slot)
        {
            _activeTab = slot;
            bool isTop = slot == OutfitSlot.Top;

            if (topGrid) topGrid.gameObject.SetActive(isTop);
            if (bottomGrid) bottomGrid.gameObject.SetActive(!isTop);

            if (tabTopButton) tabTopButton.interactable = !isTop;
            if (tabBottomButton) tabBottomButton.interactable = isTop;

            UpdateEquipButton();
        }

        void UpdateEquipButton()
        {
            if (!equipButton) return;
            equipButton.interactable = (_previewItem != null);
        }

        void EquipPreview()
        {
            if (_current == null || _previewItem == null) return;

            _current.Outfit.Equip(_previewItem);

            // Publish event agar komponen lain dapat merespon (FX / badge / paket)
            ServiceLocator.Events.Publish(
                new ItemEquipped(_current, _previewItem.slot, _previewItem)
            );

            // Setelah equip, anggap tidak ada preview aktif
            _previewItem = null;
            UpdateEquipButton();
        }

        public void Close()
        {
            if (_current != null)
            {
                // GANTI: commit semua preview jadi equip
                _current.Outfit.EquipAllPreview();

                // lanjut flow pergi
                _current.FinishFitting();
            }

            ServiceLocator.Events.Publish(new FittingUIClosed());
            _current = null;
            _previewItem = null;
            if (panelRoot) panelRoot.SetActive(false);
        }


        #endregion
    }
}
