using UnityEngine;
using MMDress.Data;

namespace MMDress.Character
{
    /// Outfit 2D sederhana (Top/Bottom) dengan Preview & Equip.
    [DisallowMultipleComponent]
    public class CharacterOutfitController : MonoBehaviour
    {
        [Header("Anchors")]
        [SerializeField] private Transform topAnchor;
        [SerializeField] private Transform bottomAnchor;

        private SpriteRenderer _topR, _botR;
        private ItemSO _eqTop, _eqBot;     // equipped
        private ItemSO _prevTop, _prevBot; // last preview

        void Awake()
        {
            _topR = EnsureRenderer(topAnchor);
            _botR = EnsureRenderer(bottomAnchor);
        }

        SpriteRenderer EnsureRenderer(Transform t)
        {
            if (!t)
            {
                Debug.LogWarning("[MMDress] Missing outfit anchor.");
                return null;
            }
            var r = t.GetComponent<SpriteRenderer>();
            if (!r) r = t.gameObject.AddComponent<SpriteRenderer>();
            // tidak memaksa material/warna—pakai setting prefab/material project
            return r;
        }

        // ===== Public API =====
        public void TryOn(ItemSO item)
        {
            if (!item) return;
            Apply(item);
            if (item.slot == OutfitSlot.Top) _prevTop = item; else _prevBot = item;
        }

        public void Equip(ItemSO item)
        {
            if (!item) return;
            Apply(item);
            if (item.slot == OutfitSlot.Top) { _eqTop = item; _prevTop = null; }
            else { _eqBot = item; _prevBot = null; }
        }

        /// Batalkan preview; jika belum ada equip untuk slot itu, kosongkan.
        public void RevertPreview()
        {
            if (_prevTop)
            {
                if (_eqTop) Apply(_eqTop); else ClearEquipped(OutfitSlot.Top);
            }
            if (_prevBot)
            {
                if (_eqBot) Apply(_eqBot); else ClearEquipped(OutfitSlot.Bottom);
            }
            _prevTop = _prevBot = null;
        }

        /// Kosongkan sprite pada slot & lupakan equip-nya.
        public void ClearEquipped(OutfitSlot slot)
        {
            if (slot == OutfitSlot.Top)
            {
                _eqTop = null;
                if (_topR) _topR.sprite = null;
            }
            else
            {
                _eqBot = null;
                if (_botR) _botR.sprite = null;
            }
        }
        // ===== Public API tambahan =====
        public void EquipAllPreview()
        {
            // Jika sebelumnya sempat preview Top, commit jadi equip
            if (_prevTop)
            {
                Apply(_prevTop);
                _eqTop = _prevTop;
                _prevTop = null;
            }
            // Jika sebelumnya sempat preview Bottom, commit jadi equip
            if (_prevBot)
            {
                Apply(_prevBot);
                _eqBot = _prevBot;
                _prevBot = null;
            }
        }

        /// Reset total (untuk pooling): bersihkan preview & equip kedua slot.
        public void ResetAll()
        {
            _prevTop = _prevBot = null;
            _eqTop = _eqBot = null;

            if (_topR) _topR.sprite = null;
            if (_botR) _botR.sprite = null;
        }

        // ===== Internal =====
        void Apply(ItemSO item)
        {
            if (!item) return;

            var a = (item.slot == OutfitSlot.Top) ? topAnchor : bottomAnchor;
            var r = (item.slot == OutfitSlot.Top) ? _topR : _botR;

            if (r) r.sprite = item.sprite;

            if (a)
            {
                a.localPosition = item.localPos;
                a.localScale = item.localScale;
                a.localRotation = Quaternion.Euler(0f, 0f, item.localRotZ);
            }
        }

        public MMDress.Data.ItemSO GetEquipped(MMDress.Data.OutfitSlot slot)
        {
            return (slot == MMDress.Data.OutfitSlot.Top) ? _eqTop : _eqBot;
        }
    }
}
