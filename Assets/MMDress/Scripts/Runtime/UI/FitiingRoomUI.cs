using UnityEngine;
using UnityEngine.UI;
using MMDress.Core;
using MMDress.Gameplay;
using MMDress.Data;
using MMDress.UI; // <-- penting untuk CharacterOutfitController

namespace MMDress.UI
{
    /// UI Fitting (1 list horizontal + filter kategori):
    /// - Klik Baju/Rok -> list memuat item slot terkait
    /// - Klik item -> preview di karakter (UI Image)
    /// - Equip -> commit 1 item preview aktif
    /// - Close -> kirim jumlah item ter-equip ke CustomerController
    [DisallowMultipleComponent]
    [AddComponentMenu("MMDress/UI/Fitting Room UI")]
    public sealed class FittingRoomUI : MonoBehaviour
    {
        #region Inspector

        [Header("Panel Root")]
        [SerializeField] private GameObject panelRoot; // Biarkan aktif; sembunyikan via CanvasGroup

        [Header("Data")]
        [SerializeField] private CatalogSO catalog;

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

        private Customer.CustomerController _current;
        private ItemSO _previewItem;
        private OutfitSlot _activeTab = OutfitSlot.Top;

        // State commit (hasil Equip)
        private ItemSO _equippedTop;
        private ItemSO _equippedBottom;

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
            if (closeButton) closeButton.onClick.AddListener(Close);

            if (listView != null)
                listView.OnItemSelected = OnItemClicked;

            SetVisible(false); // sembunyikan lewat alpha
        }

        #endregion

        #region Public API

        // Dipanggil dari CustomerController.OnClick()
        public void Open(Customer.CustomerController target)
        {
            _current = target;
            _previewItem = null;
            _equippedTop = null;
            _equippedBottom = null;

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
            if (panelRoot) panelRoot.SetActive(true); // keep alive
        }

        private void RefreshPreviewFromState()
        {
            if (!preview) return;
            preview.ApplyEquipped(_equippedTop, _equippedBottom);
        }

        private void ShowTab(OutfitSlot slot)
        {
            _activeTab = slot;

            if (listView)
            {
                listView.SetCatalog(catalog);
                listView.SetSlot(slot);
                var equipped = (slot == OutfitSlot.Top) ? _equippedTop : _equippedBottom;
                listView.Refresh(equipped);
            }

            // Pastikan preview menampilkan kombinasi equip terkini
            RefreshPreviewFromState();

            if (tabTopButton) tabTopButton.interactable = (slot != OutfitSlot.Top);
            if (tabBottomButton) tabBottomButton.interactable = (slot != OutfitSlot.Bottom);

            UpdateEquipButton();
        }

        private void UpdateEquipButton()
        {
            if (equipButton) equipButton.interactable = (_previewItem != null);
        }

        #endregion

        #region Callbacks

        private void OnItemClicked(ItemSO item)
        {
            _previewItem = item;

            // Preview langsung di UI (Image)
            if (preview) preview.TryOn(item);

            ServiceLocator.Events.Publish(new ItemPreviewed(item));
            UpdateEquipButton();
        }

        private void EquipPreview()
        {
            if (_previewItem == null) return;

            // Commit ke state lokal
            if (_previewItem.slot == OutfitSlot.Top) _equippedTop = _previewItem;
            if (_previewItem.slot == OutfitSlot.Bottom) _equippedBottom = _previewItem;

            // Broadcast sesuai signature event di project-mu
            if (_current != null)
                ServiceLocator.Events.Publish(
                    new ItemEquipped(_current, _previewItem.slot, _previewItem)
                );

            _previewItem = null;

            // Tampilkan hasil commit
            RefreshPreviewFromState();
            UpdateEquipButton();
        }

        public void Close()
        {
            int equippedCount = (_equippedTop ? 1 : 0) + (_equippedBottom ? 1 : 0);

            if (_current != null)
                _current.FinishFitting(equippedCount);

            ServiceLocator.Events.Publish(new FittingUIClosed());

            _current = null;
            _previewItem = null;
            SetVisible(false);
        }

        #endregion
    }
}
