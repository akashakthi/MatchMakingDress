using UnityEngine;
using UnityEngine.UI;
using TMPro;                       // untuk label TMP
using MMDress.Core;
using MMDress.Gameplay;
using MMDress.Data;
using MMDress.UI;                 // CharacterOutfitController, events
using MMDress.Runtime.Fitting;    // FittingSession

namespace MMDress.UI
{
    /// <summary>
    /// Fitting UI (no Close button):
    /// - Soft-preview per slot (Top/Bottom) bertahan lintas tab.
    /// - Equip = commit semua soft-preview lalu auto-close.
    /// - Render preview = kombinasi (soft ?? equipped).
    /// - Label judul slot dinamis: "Kebaya" (Top) / "Jarik" (Bottom).
    /// - Prefill tanpa akses CatalogSO.items: pakai session.Equipped* atau defaultTop/defaultBottom dari Inspector.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("MMDress/UI/Fitting Room UI")]
    public sealed class FittingRoomUI : MonoBehaviour
    {
        [Header("Panel Root")]
        [SerializeField] private GameObject panelRoot;

        [Header("Data")]
        [SerializeField] private CatalogSO catalog;          // tetap diset agar ItemGridView bisa baca via API internalnya

        [Header("List (Horizontal)")]
        [SerializeField] private ItemGridView listView;

        [Header("Preview (UI)")]
        [SerializeField] private CharacterOutfitController preview;

        [Header("Buttons")]
        [SerializeField] private Button tabTopButton;
        [SerializeField] private Button tabBottomButton;
        [SerializeField] private Button equipButton;

        [Header("Labels (UI)")]
        [SerializeField] private TMP_Text slotTitleLabel;    // label judul slot

        [Header("Fitting Session")]
        [SerializeField] private FittingSession session;

        [Header("Resolver (opsional)")]
        [SerializeField] private FittingResultResolver resolver;

        [Header("Options")]
        [SerializeField] private bool autoFindInChildren = true;
        [SerializeField] private bool autoFindSession = true;

        [Header("Defaults (tanpa akses CatalogSO.items)")]
        [SerializeField] private ItemSO defaultTop;          // drag contoh Kebaya default di Inspector
        [SerializeField] private ItemSO defaultBottom;       // drag contoh Jarik default di Inspector

        // --- State ---
        private Customer.CustomerController _current;
        private OutfitSlot _activeTab = OutfitSlot.Top;
        private CanvasGroup _cg;

        // Soft-preview per slot
        private ItemSO _softTop;
        private ItemSO _softBottom;

        // Legacy (opsional; tidak dipakai untuk enable/disable equip)
        private ItemSO _previewItem;

        // ---------- Unity ----------
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
            if (equipButton) equipButton.onClick.AddListener(EquipAndClose);

            if (listView) listView.OnItemSelected = OnItemClicked;

            SetVisible(false);
        }

        /// Dipanggil dari CustomerController saat customer diklik
        public void Open(Customer.CustomerController target)
        {
            _current = target;
            _previewItem = null;

            session?.ResetSession();
            preview?.Clear();

            // Prefill awal tanpa menyentuh CatalogSO.items:
            // Prioritas: session.Equipped* -> default* (Inspector)
            _softTop = session?.EquippedTop ?? defaultTop;
            _softBottom = session?.EquippedBottom ?? defaultBottom;

            SetVisible(true);

            // Buka tab Top sebagai default dan render awal
            ShowTab(OutfitSlot.Top);
            RefreshPreviewCombined();

            ServiceLocator.Events.Publish(new FittingUIOpened());
            UpdateEquipButton();
        }

        // ---------- UI State ----------
        private void SetVisible(bool on)
        {
            if (_cg)
            {
                _cg.alpha = on ? 1f : 0f;
                _cg.interactable = on;
                _cg.blocksRaycasts = on;
            }
            if (panelRoot) panelRoot.SetActive(true); // keep-alive layout
        }

        private void RefreshPreviewCombined()
        {
            if (!preview) return;
            var topToShow = _softTop ?? session?.EquippedTop;
            var bottomToShow = _softBottom ?? session?.EquippedBottom;
            preview.ApplyEquipped(topToShow, bottomToShow);
        }

        private void ShowTab(OutfitSlot slot)
        {
            _activeTab = slot;

            // Ubah label judul
            if (slotTitleLabel)
                slotTitleLabel.text = (slot == OutfitSlot.Top) ? "Kebaya" : "Jarik";

            if (listView)
            {
                listView.SetCatalog(catalog);
                listView.SetSlot(slot);

                // highlight item aktif (soft > equipped)
                var selected = (slot == OutfitSlot.Top)
                    ? (_softTop ?? session?.EquippedTop)
                    : (_softBottom ?? session?.EquippedBottom);

                // Jika ItemGridView punya Refresh(selected) gunakan ini; jika tidak, Anda bisa ubah ke Refresh(); dan (opsional) panggil listView.SetSelected(selected) bila tersedia.
                listView.Refresh(selected);
            }

            RefreshPreviewCombined();

            if (tabTopButton) tabTopButton.interactable = (slot != OutfitSlot.Top);
            if (tabBottomButton) tabBottomButton.interactable = (slot != OutfitSlot.Bottom);

            UpdateEquipButton();
        }

        private void UpdateEquipButton()
        {
            bool hasSoft = (_softTop != null) || (_softBottom != null);
            equipButton.InteractableIf(hasSoft);
        }

        // ---------- Interaksi Grid ----------
        private void OnItemClicked(ItemSO item)
        {
            if (item == null) return;

            // Hormati lock per slot
            if (item.slot == OutfitSlot.Top && (session?.IsTopLocked ?? false)) return;
            if (item.slot == OutfitSlot.Bottom && (session?.IsBottomLocked ?? false)) return;

            _previewItem = item; // legacy

            if (item.slot == OutfitSlot.Top) _softTop = item;
            if (item.slot == OutfitSlot.Bottom) _softBottom = item;

            RefreshPreviewCombined();
            ServiceLocator.Events.Publish(new ItemPreviewed(item));
            UpdateEquipButton();
        }

        // ---------- Commit & Auto-Close ----------
        private void EquipAndClose()
        {
            if (session == null) return;

            bool anyCommitted = false;

            if (_softTop != null && session.EquipTop(_softTop))
            {
                anyCommitted = true;
                if (_current != null)
                    ServiceLocator.Events.Publish(new OutfitEquippedCommitted(_current, OutfitSlot.Top, _softTop));
            }

            if (_softBottom != null && session.EquipBottom(_softBottom))
            {
                anyCommitted = true;
                if (_current != null)
                    ServiceLocator.Events.Publish(new OutfitEquippedCommitted(_current, OutfitSlot.Bottom, _softBottom));
            }

            _softTop = null;
            _softBottom = null;
            _previewItem = null;

            RefreshPreviewCombined();
            UpdateEquipButton();

            if (anyCommitted) Close();
        }

        /// Tetap tersedia untuk dipanggil resolver / eksternal (tanpa tombol Close di UI).
        public void Close()
        {
            if (resolver && _current && session)
            {
                if (resolver.ResolveAndClose(_current, session, this)) return;
            }

            session?.FinalizeSession();
            int equippedCount =
                (session?.EquippedTop ? 1 : 0) +
                (session?.EquippedBottom ? 1 : 0);

            _current?.FinishFitting(equippedCount);
            InternalClose();
        }

        public void InternalClose()
        {
            ServiceLocator.Events.Publish(new FittingUIClosed());
            _current = null;
            _previewItem = null;
            _softTop = null;
            _softBottom = null;
            SetVisible(false);
        }
    }

    // Util
    static class UIButtonExt
    {
        public static void InteractableIf(this Button b, bool cond)
        {
            if (b) b.interactable = cond;
        }
    }
}
