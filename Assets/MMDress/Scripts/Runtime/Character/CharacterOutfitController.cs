using UnityEngine;
using UnityEngine.UI;
using MMDress.Data;

namespace MMDress.UI
{
    /// Preview outfit versi UI (Canvas): cukup ganti sprite pada 2 Image.
    [DisallowMultipleComponent]
    [AddComponentMenu("MMDress/UI/UI Character Outfit Preview")]
    public sealed class CharacterOutfitController : MonoBehaviour
    {
        [Header("Anchors (UI Image)")]
        [SerializeField] private Image topImage;     // drag: Image baju/kebaya
        [SerializeField] private Image bottomImage;  // drag: Image rok

        [Header("Options")]
        [SerializeField] private bool preserveAspect = true;

        private void Reset()
        {
            if (!topImage) topImage = transform.Find("TopGrid")?.GetComponent<Image>()
                                 ?? transform.Find("TopImage")?.GetComponent<Image>();
            if (!bottomImage) bottomImage = transform.Find("BottomGrid")?.GetComponent<Image>()
                                 ?? transform.Find("BottomImage")?.GetComponent<Image>();
        }

        public void Clear()
        {
            SetImage(topImage, null);
            SetImage(bottomImage, null);
        }

        public void TryOn(ItemSO item)
        {
            if (!item) return;

            switch (item.slot)
            {
                case OutfitSlot.Top:
                    SetImage(topImage, item.sprite);
                    break;
                case OutfitSlot.Bottom:
                    SetImage(bottomImage, item.sprite);
                    break;
            }
        }

        public void ApplyEquipped(ItemSO top, ItemSO bottom)
        {
            SetImage(topImage, top ? top.sprite : null);
            SetImage(bottomImage, bottom ? bottom.sprite : null);
        }

        private void SetImage(Image img, Sprite s)
        {
            if (!img) return;
            img.sprite = s;
            img.enabled = (s != null);
            img.type = Image.Type.Simple;
            img.raycastTarget = false;              // jangan halangi klik di bawahnya
            if (preserveAspect) img.preserveAspect = true;

            var c = img.color;                      // pastikan tampak jelas
            c.a = (s != null) ? 1f : c.a;
            img.color = c;
        }
    }
}
