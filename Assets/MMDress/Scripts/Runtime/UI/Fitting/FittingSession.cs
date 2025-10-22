using UnityEngine;
using MMDress.Data;
using MMDress.Services;

namespace MMDress.Runtime.Fitting
{
    /// Menampung state outfit selama panel Fitting terbuka.
    /// - Equip() langsung commit ke stok (jika stok ada)
    /// - Jika stok habis: tetap boleh equip visual, ditandai outOfStock=true (akan dihitung "empty" saat resolve)
    [DisallowMultipleComponent]
    public sealed class FittingSession : MonoBehaviour
    {
        [Header("Services")]
        [SerializeField] private StockService stock;          // drag dari _Services
        [SerializeField] private bool autoFind = true;

        [Header("Options")]
        [Tooltip("Jika stok 0, tetap izinkan equip visual dan tandai sebagai out-of-stock (dihitung empty saat Close).")]
        [SerializeField] private bool allowVisualWhenOutOfStock = true;

        // State equip
        public ItemSO EquippedTop { get; private set; }
        public ItemSO EquippedBottom { get; private set; }

        // Lock per slot (set true setelah berhasil equip)
        public bool IsTopLocked { get; private set; }
        public bool IsBottomLocked { get; private set; }

        // Flag dipakai resolver untuk menilai 'empty'
        public bool TopOutOfStock { get; private set; }
        public bool BottomOutOfStock { get; private set; }

        void Awake()
        {
#if UNITY_2023_1_OR_NEWER
            if (autoFind && !stock) stock = Object.FindFirstObjectByType<StockService>(FindObjectsInactive.Include);
#else
            if (autoFind && !stock) stock = FindObjectOfType<StockService>(true);
#endif
        }

        public void ResetSession()
        {
            EquippedTop = EquippedBottom = null;
            IsTopLocked = IsBottomLocked = false;
            TopOutOfStock = BottomOutOfStock = false;
        }

        /// Equip TOP. Mengurangi stok jika ada; kalau tidak ada stok dan diizinkan, equip visual + flag out-of-stock.
        public bool EquipTop(ItemSO top)
        {
            if (!top || top.slot != OutfitSlot.Top) return false;
            if (IsTopLocked) return false;

            bool tookStock = ConsumeIfPossible(top, 1);
            TopOutOfStock = !tookStock;     // true kalau stok 0 dan kita hanya equip visual

            EquippedTop = top;
            IsTopLocked = true;
            return true;
        }

        /// Equip BOTTOM.
        public bool EquipBottom(ItemSO bottom)
        {
            if (!bottom || bottom.slot != OutfitSlot.Bottom) return false;
            if (IsBottomLocked) return false;

            bool tookStock = ConsumeIfPossible(bottom, 1);
            BottomOutOfStock = !tookStock;

            EquippedBottom = bottom;
            IsBottomLocked = true;
            return true;
        }

        /// No-op untuk kompatibilitas flow lama (biarkan ada).
        public void FinalizeSession() { /* stok final sudah diproses saat equip */ }

        // ───────── helpers stok (pakai API minimal StockService: Get + Set) ─────────
        bool ConsumeIfPossible(ItemSO item, int qty)
        {
            if (!stock) return allowVisualWhenOutOfStock;     // tanpa stock service tetap allow visual

            int cur = stock.GetGarment(item);
            if (cur >= qty)
            {
                stock.SetGarment(item, cur - qty);
                return true;
            }
            return allowVisualWhenOutOfStock;
        }
    }
}
