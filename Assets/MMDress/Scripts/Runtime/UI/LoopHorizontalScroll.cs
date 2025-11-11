// Assets/MMDress/Scripts/Runtime/UI/ScrollRectClampMargins.cs
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MMDress.UI
{
    /// Clamp batas kiri/kanan untuk ScrollRect Horizontal (Unrestricted)
    /// dengan margin kiri/kanan di dalam viewport. Sederhana & reusable.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ScrollRect))]
    public sealed class ScrollRectClampMargins : MonoBehaviour, IBeginDragHandler, IEndDragHandler
    {
        [Header("Refs (auto jika kosong)")]
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private RectTransform viewport;   // jika null: pakai RT ScrollRect
        [SerializeField] private RectTransform content;    // jika null: pakai scrollRect.content

        [Header("Margins (px) dari tepi viewport")]
        [Min(0)] public float leftMargin = 16f;
        [Min(0)] public float rightMargin = 16f;

        [Header("Return Mode")]
        [Tooltip("Hard clamp saat drag, dan lerp balik saat dilepas.")]
        public bool smoothReturn = true;
        [Min(1f)] public float returnSpeed = 1800f;

        private bool _dragging;

        void Reset()
        {
            scrollRect = GetComponent<ScrollRect>();
            viewport = scrollRect ? (scrollRect.viewport ? scrollRect.viewport : (RectTransform)scrollRect.transform) : null;
            content = scrollRect ? scrollRect.content : null;
        }

        void Awake()
        {
            if (!scrollRect) scrollRect = GetComponent<ScrollRect>();
            if (!viewport) viewport = scrollRect ? (scrollRect.viewport ? scrollRect.viewport : (RectTransform)scrollRect.transform) : null;
            if (!content) content = scrollRect ? scrollRect.content : null;
        }

        void OnEnable()
        {
            if (!scrollRect || !viewport || !content) return;
            scrollRect.horizontal = true;
            scrollRect.vertical = false;
            scrollRect.movementType = ScrollRect.MovementType.Unrestricted; // sesuai permintaan
        }

        void LateUpdate()
        {
            if (!scrollRect || !viewport || !content) return;

            // Hitung bounds konten relatif ke viewport (ruang lokal viewport).
            // Ini otomatis memasukkan padding/spacing dari HorizontalLayoutGroup.
            var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, content);
            var vp = viewport.rect;

            // Batas yang diizinkan (di local-space viewport)
            float minAllowed = vp.xMin + leftMargin;     // tepi kiri viewport + margin
            float maxAllowed = vp.xMax - rightMargin;    // tepi kanan viewport - margin

            float contentWidth = bounds.size.x;
            float allowedWidth = vp.width - leftMargin - rightMargin;

            // Kalau konten tidak lebih lebar dari area diizinkan, tidak ada yang perlu diclamp
            if (contentWidth <= allowedWidth + 0.5f) return;

            // Overshoot positif jika melewati batas
            float leftExcess = minAllowed - bounds.min.x; // > 0 artinya terlalu ke kiri
            float rightExcess = bounds.max.x - maxAllowed; // > 0 artinya terlalu ke kanan

            if (leftExcess <= 0f && rightExcess <= 0f) return;

            // Konversi overshoot ke delta anchoredPosition.x
            float delta = (leftExcess > 0f) ? leftExcess : -rightExcess;

            // Apply clamp
            var pos = content.anchoredPosition;
            if (_dragging || !smoothReturn)
            {
                pos.x += delta;                // tahan keras saat drag
                content.anchoredPosition = pos;
                if (_dragging) scrollRect.velocity = Vector2.zero; // hentikan dorongan keluar
            }
            else
            {
                float step = returnSpeed * Time.unscaledDeltaTime; // smooth balik saat tidak drag
                pos.x += Mathf.Clamp(delta, -step, step);
                content.anchoredPosition = pos;
                scrollRect.velocity = Vector2.zero;
            }
        }//

        public void OnBeginDrag(PointerEventData e) => _dragging = true;
        public void OnEndDrag(PointerEventData e) => _dragging = false;
    }
}
