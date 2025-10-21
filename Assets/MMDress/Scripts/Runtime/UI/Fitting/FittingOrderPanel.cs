// Assets/MMDress/Scripts/Runtime/UI/Fitting/FittingOrderPanel.cs
using UnityEngine;
using UnityEngine.UI;
using MMDress.Data;

namespace MMDress.UI
{
    [DisallowMultipleComponent]
    public sealed class FittingOrderPanel : MonoBehaviour
    {
        [Header("UI (single icon per slot)")]
        [SerializeField] private Image topIcon;     // hanya ikon permintaan Top
        [SerializeField] private Image bottomIcon;  // hanya ikon permintaan Bottom

        [Header("Tint Colors")]
        [SerializeField] private Color okTint = new(0.30f, 1f, 0.30f, 1f); // cocok
        [SerializeField] private Color failTint = new(1f, 0.30f, 0.30f, 1f); // tidak cocok
        [SerializeField] private Color neutral = Color.white;               // netral (tidak diminta)

        private OrderSO _order;

        public void Bind(OrderSO order)
        {
            _order = order;

            // TOP
            if (topIcon)
            {
                if (order && order.requiredTop && order.requiredTop.sprite)
                {
                    topIcon.enabled = true;
                    topIcon.sprite = order.requiredTop.sprite;
                    topIcon.preserveAspect = true;
                    topIcon.color = neutral; // reset tint saat bind
                }
                else
                {
                    // tidak ada permintaan Top → sembunyikan ikon
                    topIcon.enabled = false;
                    topIcon.sprite = null;
                }
            }

            // BOTTOM
            if (bottomIcon)
            {
                if (order && order.requiredBottom && order.requiredBottom.sprite)
                {
                    bottomIcon.enabled = true;
                    bottomIcon.sprite = order.requiredBottom.sprite;
                    bottomIcon.preserveAspect = true;
                    bottomIcon.color = neutral;
                }
                else
                {
                    bottomIcon.enabled = false;
                    bottomIcon.sprite = null;
                }
            }
        }

        public void ShowMatch(ItemSO equippedTop, ItemSO equippedBottom)
        {
            if (!_order)
            {
                if (topIcon) topIcon.color = neutral;
                if (bottomIcon) bottomIcon.color = neutral;
                return;
            }

            // TOP
            if (topIcon)
            {
                if (_order.requiredTop == null)
                {
                    // tidak diminta → ikon disembunyikan pada Bind, atau biarkan neutral kalau kamu ingin tetap tampil
                    // topIcon.color = neutral;
                }
                else
                {
                    bool topOk = (equippedTop == _order.requiredTop);
                    topIcon.color = topOk ? okTint : failTint;
                }
            }

            // BOTTOM
            if (bottomIcon)
            {
                if (_order.requiredBottom == null)
                {
                    // tidak diminta
                    // bottomIcon.color = neutral;
                }
                else
                {
                    bool botOk = (equippedBottom == _order.requiredBottom);
                    bottomIcon.color = botOk ? okTint : failTint;
                }
            }
        }
    }
}
