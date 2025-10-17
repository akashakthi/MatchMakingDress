using System.Collections.Generic;
using UnityEngine;

namespace MMDress.Data
{
    [CreateAssetMenu(menuName = "MMDress/Catalog")]
    public sealed class CatalogSO : ScriptableObject
    {
        [SerializeField] private List<ItemSO> items = new();
        public IReadOnlyList<ItemSO> Items => items;

        public int TopCount
        {
            get
            {
                int c = 0;
                foreach (var it in items) if (it && it.slot == OutfitSlot.Top) c++;
                return c;
            }
        }

        public int BottomCount
        {
            get
            {
                int c = 0;
                foreach (var it in items) if (it && it.slot == OutfitSlot.Bottom) c++;
                return c;
            }
        }

        // Cari index absolut di list items
        public int IndexOf(ItemSO item) => items.IndexOf(item);

        // Dapatkan index relatif (per-slot) untuk StockService
        public int GetRelativeIndex(ItemSO item)
        {
            if (!item) return -1;

            if (item.slot == OutfitSlot.Top)
            {
                int rel = 0;
                foreach (var it in items)
                {
                    if (!it) continue;
                    if (it.slot == OutfitSlot.Top)
                    {
                        if (it == item) return rel;
                        rel++;
                    }
                }
            }
            else
            {
                int rel = 0;
                foreach (var it in items)
                {
                    if (!it) continue;
                    if (it.slot == OutfitSlot.Bottom)
                    {
                        if (it == item) return rel;
                        rel++;
                    }
                }
            }
            return -1;
        }

#if UNITY_EDITOR
        // ===== Editor-only helpers (agar EditorWindow tidak akses field private) =====
        /// <summary>List yang bisa dimodifikasi dari editor scripts (jangan dipakai di runtime).</summary>
        public IList<ItemSO> EditorItems => items;

        /// <summary>Tambah item jika belum ada (khusus editor).</summary>
        public void Editor_AddItem(ItemSO item)
        {
            if (item && !items.Contains(item)) items.Add(item);
        }

        /// <summary>Hapus entry null di list (khusus editor).</summary>
        public void Editor_RemoveNulls()
        {
            items.RemoveAll(i => !i);
        }
#endif
    }
}
