using UnityEngine;
using MMDress.Data;

namespace MMDress.Character
{
    public class CharacterOutfitController : MonoBehaviour
    {
        [Header("Anchors")]
        [SerializeField] private Transform topAnchor;
        [SerializeField] private Transform bottomAnchor;

        private SpriteRenderer _topR, _botR;
        private ItemSO _eqTop, _eqBot;     // equipped
        private ItemSO _prevTop, _prevBot; // last preview

        private void Awake()
        {
            _topR = EnsureRenderer(topAnchor);
            _botR = EnsureRenderer(bottomAnchor);
        }

        private SpriteRenderer EnsureRenderer(Transform t)
        {
            if (!t) { Debug.LogWarning("[MMDress] Missing anchor."); return null; }
            var r = t.GetComponent<SpriteRenderer>();
            if (!r) r = t.gameObject.AddComponent<SpriteRenderer>();
            return r;
        }

        public void TryOn(ItemSO item) { Apply(item); CachePreview(item); }
        public void Equip(ItemSO item) { Apply(item); CacheEquip(item); }
        public void RevertPreview()
        {
            if (_prevTop) Apply(_eqTop);
            if (_prevBot) Apply(_eqBot);
            _prevTop = _prevBot = null;
        }

        private void Apply(ItemSO item)
        {
            if (item == null) return;
            var a = item.slot == OutfitSlot.Top ? topAnchor : bottomAnchor;
            var r = item.slot == OutfitSlot.Top ? _topR : _botR;

            if (r) r.sprite = item.sprite;
            if (a)
            {
                a.localPosition = item.localPos;
                a.localScale = item.localScale;
                a.localRotation = Quaternion.Euler(0, 0, item.localRotZ);
            }
        }

        private void CacheEquip(ItemSO item)
        { if (item.slot == OutfitSlot.Top) _eqTop = item; else _eqBot = item; }

        private void CachePreview(ItemSO item)
        { if (item.slot == OutfitSlot.Top) _prevTop = item; else _prevBot = item; }
    }
}
