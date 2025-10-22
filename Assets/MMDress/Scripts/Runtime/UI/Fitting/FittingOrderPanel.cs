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
        [SerializeField] private Image topIcon;     // ikon permintaan Top (optional)
        [SerializeField] private Image bottomIcon;  // ikon permintaan Bottom (optional)

        [Header("Style")]
        [SerializeField] private Color neutral = Color.white;

        private OrderSO _order;

        public void Bind(OrderSO order)
        {
            _order = order;

            // TOP
            if (topIcon)
            {
                var top = order && order.requiredTop ? order.requiredTop.sprite : null;
                topIcon.sprite = top;
                topIcon.enabled = (top != null);
                topIcon.preserveAspect = true;
                topIcon.color = neutral; // selalu netral
            }

            // BOTTOM
            if (bottomIcon)
            {
                var bot = order && order.requiredBottom ? order.requiredBottom.sprite : null;
                bottomIcon.sprite = bot;
                bottomIcon.enabled = (bot != null);
                bottomIcon.preserveAspect = true;
                bottomIcon.color = neutral; // selalu netral
            }
        }

        // Tetap ada buat kompatibilitas, tapi tidak melakukan apa pun.
        public void ShowMatch(ItemSO equippedTop, ItemSO equippedBottom)
        {
            // no-op: tidak ada tinting/efek warna
        }
    }
}
