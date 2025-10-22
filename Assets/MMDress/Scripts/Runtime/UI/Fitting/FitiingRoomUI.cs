using UnityEngine;
using UnityEngine.UI;
using MMDress.Core;
using MMDress.Gameplay;
using MMDress.Data;
using MMDress.UI;                 // CharacterOutfitController, events
using MMDress.Runtime.Fitting;    // FittingSession

namespace MMDress.UI
{
    /// Fitting UI (hybrid close).
    [DisallowMultipleComponent]
    [AddComponentMenu("MMDress/UI/Fitting Room UI")]
    public sealed class FittingRoomUI : MonoBehaviour
    {
        [Header("Panel Root")]
        [SerializeField] private GameObject panelRoot;

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

        [Header("Fitting Session")]
        [SerializeField] private FittingSession session;

        [Header("Resolver (opsional)")]
        [SerializeField] private FittingResultResolver resolver;

        [Header("Options")]
        [SerializeField] private bool autoFindInChildren = true;
        [SerializeField] private bool autoFindSession = true;

        private Customer.CustomerController _current;
        private ItemSO _previewItem;
        private OutfitSlot _activeTab = OutfitSlot.Top;
        private CanvasGroup _cg;

        void Awake()
        {
            _cg = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
            if (autoFindInChildren && !listView) listView = GetComponentInChildren<ItemGridView>(true);

#if UNITY_2023_1_OR_NEWER
            if (autoFindSession && !session) session = Object.FindFirstObjectByType<FittingSession>(FindObjectsInactive.Include);
#else
            if (autoFindSession && !session) session = FindObjectOfType<FittingSession>(true);
#endif

            if (tabTopButton) tabTopButton.onClick.AddListener(() => ShowTab(OutfitSlot.Top));
            if (tabBottomButton) tabBottomButton.onClick.AddListener(() => ShowTab(OutfitSlot.Bottom));
            if (equipButton) equipButton.onClick.AddListener(EquipPreview);
            if (closeButton) closeButton.onClick.AddListener(Close);

            if (listView) listView.OnItemSelected = OnItemClicked;

            SetVisible(false);
        }

        // Dipanggil dari CustomerController saat customer diklik
        public void Open(Customer.CustomerController target)
        {
            _current = target;
            _previewItem = null;

            session?.ResetSession();
            preview?.Clear();

            SetVisible(true);
            ShowTab(OutfitSlot.Top);

            ServiceLocator.Events.Publish(new FittingUIOpened());
            UpdateEquipButton();
        }

        void SetVisible(bool on)
        {
            if (_cg)
            {
                _cg.alpha = on ? 1f : 0f;
                _cg.interactable = on;
                _cg.blocksRaycasts = on;
            }
            if (panelRoot) panelRoot.SetActive(true); // keep-alive
        }

        void RefreshPreviewFromSession()
        {
            if (!preview || !session) return;
            preview.ApplyEquipped(session.EquippedTop, session.EquippedBottom);
        }

        void ShowTab(OutfitSlot slot)
        {
            _activeTab = slot;

            if (listView)
            {
                listView.SetCatalog(catalog);
                listView.SetSlot(slot);
                var equipped = (slot == OutfitSlot.Top) ? session?.EquippedTop : session?.EquippedBottom;
                listView.Refresh(equipped);
            }

            RefreshPreviewFromSession();

            if (tabTopButton) tabTopButton.interactable = (slot != OutfitSlot.Top);
            if (tabBottomButton) tabBottomButton.interactable = (slot != OutfitSlot.Bottom);

            UpdateEquipButton();
        }

        void UpdateEquipButton() => equipButton.InteractableIf(_previewItem != null);

        void OnItemClicked(ItemSO item)
        {
            if (item == null) return;

            // Hormati lock per slot
            if (item.slot == OutfitSlot.Top && (session?.IsTopLocked ?? false)) return;
            if (item.slot == OutfitSlot.Bottom && (session?.IsBottomLocked ?? false)) return;

            _previewItem = item;

            // (Optional) kalau mau DIM saat stok 0: ambil dari StockService di tempat lain
            bool dim = false;
            preview?.TryOn(item, dim);

            ServiceLocator.Events.Publish(new ItemPreviewed(item));
            UpdateEquipButton();
        }

        void EquipPreview()
        {
            if (_previewItem == null || session == null) return;

            bool ok = _previewItem.slot == OutfitSlot.Top
                ? session.EquipTop(_previewItem)
                : session.EquipBottom(_previewItem);

            if (!ok) return;

            if (_current != null)
                ServiceLocator.Events.Publish(new OutfitEquippedCommitted(_current, _previewItem.slot, _previewItem));

            _previewItem = null;
            RefreshPreviewFromSession(); // render dari state agar tidak hilang
            UpdateEquipButton();
        }

        /// Close hybrid: jika ada resolver → pakai resolver; else fallback lama.
        public void Close()
        {
            if (resolver && _current && session)
            {
                if (resolver.ResolveAndClose(_current, session, this)) return;
            }

            session?.FinalizeSession();
            int equippedCount = (session?.EquippedTop ? 1 : 0) + (session?.EquippedBottom ? 1 : 0);
            _current?.FinishFitting(equippedCount);
            InternalClose();
        }

        /// Dipanggil internal & oleh resolver agar tidak dobel FinishFitting.
        public void InternalClose()
        {
            ServiceLocator.Events.Publish(new FittingUIClosed());
            _current = null;
            _previewItem = null;
            SetVisible(false);
        }
    }

    static class UIButtonExt
    {
        public static void InteractableIf(this Button b, bool cond)
        {
            if (b) b.interactable = cond;
        }
    }
}
