using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MMDress.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ScrollRect))]
    public sealed class ScrollRectClampMargins : MonoBehaviour, IBeginDragHandler, IEndDragHandler
    {
        [Header("Refs (auto jika kosong)")]
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private RectTransform viewport;
        [SerializeField] private RectTransform content;

        [Header("Boundary pakai GameObject (opsional)")]
        [Tooltip("Batas kiri di dalam viewport (HARUS child dari Viewport).")]
        [SerializeField] private RectTransform leftBoundary;
        [Tooltip("Batas kanan di dalam viewport (HARUS child dari Viewport).")]
        [SerializeField] private RectTransform rightBoundary;

        [Header("Fallback: Margins (px) dari tepi viewport")]
        [Min(0)] public float leftMargin = 16f;
        [Min(0)] public float rightMargin = 16f;

        [Header("Return Mode")]
        public bool smoothReturn = true;
        [Min(0.01f)] public float smoothTime = 0.15f;
        [Min(0f)] public float maxReturnSpeed = 5000f;

        bool _dragging;
        float _velX;

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
            scrollRect.movementType = ScrollRect.MovementType.Unrestricted;
        }

        void LateUpdate()
        {
            if (!scrollRect || !viewport || !content) return;

            // bounds konten relatif ke viewport
            var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, content);
            var vpRect = viewport.rect;

            // tentukan batas kiri/kanan di local-space viewport
            float minAllowed, maxAllowed;

            if (leftBoundary && rightBoundary)
            {
                var l = viewport.InverseTransformPoint(leftBoundary.position);
                var r = viewport.InverseTransformPoint(rightBoundary.position);
                minAllowed = l.x;
                maxAllowed = r.x;
            }
            else
            {
                minAllowed = vpRect.xMin + leftMargin;
                maxAllowed = vpRect.xMax - rightMargin;
            }

            // hitung overshoot
            float leftExcess = minAllowed - bounds.min.x;  // >0 = terlalu ke kanan (bolong kiri)
            float rightExcess = bounds.max.x - maxAllowed;  // >0 = terlalu ke kiri (bolong kanan)

            if (leftExcess <= 0f && rightExcess <= 0f)
            {
                // di dalam range aman
                _velX = 0f;
                return;
            }

            // delta koreksi posisi konten (local viewport)
            float delta = (leftExcess > 0f) ? leftExcess : -rightExcess;

            var pos = content.anchoredPosition;

            if (_dragging || !smoothReturn)
            {
                pos.x += delta;
                content.anchoredPosition = pos;
                scrollRect.velocity = Vector2.zero;
            }
            else
            {
                float targetX = pos.x + delta;
                float newX = Mathf.SmoothDamp(
                    pos.x,
                    targetX,
                    ref _velX,
                    smoothTime,
                    maxReturnSpeed <= 0f ? Mathf.Infinity : maxReturnSpeed,
                    Time.unscaledDeltaTime
                );

                pos.x = newX;
                content.anchoredPosition = pos;
                scrollRect.velocity = Vector2.zero;
            }
        }

        public void OnBeginDrag(PointerEventData e)
        {
            _dragging = true;
            _velX = 0f;
        }

        public void OnEndDrag(PointerEventData e)
        {
            _dragging = false;
        }
    }
}
