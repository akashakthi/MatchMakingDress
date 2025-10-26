// Assets/MMDress/Scripts/Runtime/Camera/CameraPanZoomController.cs
using UnityEngine;
using UnityEngine.EventSystems;

namespace MMDress.Runtime.CameraControl
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class CameraPanZoomController : MonoBehaviour
    {
        [Header("Zoom")]
        [SerializeField, Min(0.01f)] float zoomSpeed = 5f;            // scroll/pinch sensitivity
        [SerializeField] float minOrthoSize = 2f;
        [SerializeField] float maxOrthoSize = 12f;
        [SerializeField] float zoomLerp = 10f;                        // smoothing

        [Header("Pan")]
        [SerializeField] float panSpeed = 1f;                          // drag sensitivity
        [SerializeField] float panDamping = 15f;                       // inertia damp

        [Header("Bounds (world units)")]
        [SerializeField] bool useAutoBounds = false;                   // ambil dari BoxCollider2D/SpriteRenderer di target
        [SerializeField] Transform boundsSource;                       // opsional
        [SerializeField] Rect worldBounds = new Rect(-10, -6, 20, 12); // manual fallback

        [Header("Shortcuts (Web/Editor)")]
        [SerializeField] KeyCode focusKey = KeyCode.F;
        [SerializeField] KeyCode resetKey = KeyCode.R;
        [SerializeField] KeyCode zoomInKey = KeyCode.Equals;   // '+'
        [SerializeField] KeyCode zoomOutKey = KeyCode.Minus;   // '-'

        [Header("Behaviour")]
        [SerializeField] bool ignoreWhenPointerOverUI = true;
        [SerializeField] bool enabledAtStart = true;

        Camera cam;
        Vector3 targetPos;         // untuk pan smoothing
        float targetSize;          // untuk zoom smoothing
        bool controlsEnabled;

        // double-tap / double-click
        float lastTapTime;
        const float doubleTapWindow = 0.3f;

        // cache
        int lastActiveTouchId = -1;
        Vector3 dragScreenPrev;

        void Reset()
        {
            cam = GetComponent<Camera>();
            cam.orthographic = true;
        }

        void Awake()
        {
            cam = GetComponent<Camera>();
            if (!cam.orthographic) cam.orthographic = true;

            if (useAutoBounds && boundsSource)
            {
                TryBakeBoundsFromSource(boundsSource, out worldBounds);
            }

            controlsEnabled = enabledAtStart;
            targetPos = transform.position;
            targetSize = Mathf.Clamp(cam.orthographicSize, minOrthoSize, maxOrthoSize);
            cam.orthographicSize = targetSize;
            ClampToBoundsImmediate();
        }

        public void EnableControls(bool enable) => controlsEnabled = enable;

        public void ResetView(Vector3 worldCenter, float orthoSize)
        {
            targetPos = new Vector3(worldCenter.x, worldCenter.y, transform.position.z);
            targetSize = Mathf.Clamp(orthoSize, minOrthoSize, maxOrthoSize);
            ApplySmoothingImmediate();
            ClampToBoundsImmediate();
        }

        public void FocusOn(Transform t, float orthoSize = -1f, float snapStrength = 0.35f)
        {
            if (!t) return;
            var pos = t.position;
            targetPos = new Vector3(pos.x, pos.y, transform.position.z);
            if (orthoSize > 0f) targetSize = Mathf.Clamp(orthoSize, minOrthoSize, maxOrthoSize);

            // sedikit lerp agar terasa “snap halus”
            transform.position = Vector3.Lerp(transform.position, targetPos, snapStrength);
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetSize, snapStrength);
            ClampToBoundsImmediate();
        }

        void Update()
        {
            if (!controlsEnabled) return;

            // block kalau di atas UI
            if (ignoreWhenPointerOverUI && IsPointerOverUI()) { SmoothUpdate(); return; }

            // --- platform-agnostic: pakai touch kalau ada, else mouse ---
            if (Input.touchCount > 0)
            {
                HandleTouch();
            }
            else
            {
                HandleMouseKeyboard();
            }

            SmoothUpdate();
        }

        void HandleMouseKeyboard()
        {
            // Scroll zoom (zoom ke arah cursor)
            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.0001f)
            {
                var cursorWorld = ScreenToWorldPointSafe(Input.mousePosition);
                AddZoomDelta(-scroll * zoomSpeed, cursorWorld);
            }

            // Drag pan (LMB atau MMB). Space + drag juga boleh.
            bool drag =
                Input.GetMouseButton(0) ||
                Input.GetMouseButton(2) ||
                (Input.GetKey(KeyCode.Space) && Input.GetMouseButton(0));

            if (drag)
            {
                if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(2))
                    dragScreenPrev = Input.mousePosition;

                Vector3 delta = Input.mousePosition - dragScreenPrev;
                dragScreenPrev = Input.mousePosition;
                PanByScreenDelta(delta);
            }

            // Shortcuts
            if (Input.GetKeyDown(zoomInKey)) AddZoomDelta(+zoomSpeed, ScreenToWorldPointSafe(Input.mousePosition));
            if (Input.GetKeyDown(zoomOutKey)) AddZoomDelta(-zoomSpeed, ScreenToWorldPointSafe(Input.mousePosition));
            if (Input.GetKeyDown(resetKey)) ResetView(worldBounds.center, Mathf.Clamp((worldBounds.height * 0.5f), minOrthoSize, maxOrthoSize));
            // Focus (opsional, panggil dari luar dengan FocusOn)
            if (Input.GetKeyDown(focusKey))
            {
                // noop default
            }

            // Double-click → fokus ke titik di bawah cursor (rasa cepat)
            if (Input.GetMouseButtonDown(0))
            {
                if (Time.unscaledTime - lastTapTime <= doubleTapWindow)
                {
                    var w = ScreenToWorldPointSafe(Input.mousePosition);
                    FocusOnPoint(w);
                }
                lastTapTime = Time.unscaledTime;
            }
        }

        void HandleTouch()
        {
            // Double-tap → fokus
            if (Input.touchCount == 1 && Input.touches[0].phase == TouchPhase.Began)
            {
                if (Time.unscaledTime - lastTapTime <= doubleTapWindow)
                {
                    var w = ScreenToWorldPointSafe(Input.touches[0].position);
                    FocusOnPoint(w);
                }
                lastTapTime = Time.unscaledTime;
            }

            if (Input.touchCount == 1)
            {
                var t0 = Input.touches[0];
                if (t0.phase == TouchPhase.Began) { lastActiveTouchId = t0.fingerId; dragScreenPrev = t0.position; }
                if (t0.fingerId == lastActiveTouchId && (t0.phase == TouchPhase.Moved || t0.phase == TouchPhase.Stationary))
                {
                    Vector3 delta = (Vector3)t0.position - dragScreenPrev;
                    dragScreenPrev = t0.position;
                    PanByScreenDelta(delta);
                }
            }
            else if (Input.touchCount >= 2)
            {
                // Pinch zoom (pakai jarak dua jari)
                var t0 = Input.touches[0];
                var t1 = Input.touches[1];

                Vector2 p0Prev = t0.position - t0.deltaPosition;
                Vector2 p1Prev = t1.position - t1.deltaPosition;

                float prevDist = (p0Prev - p1Prev).magnitude;
                float currDist = (t0.position - t1.position).magnitude;
                float delta = currDist - prevDist;

                // titik tengah sebagai referensi zoom
                Vector2 mid = (t0.position + t1.position) * 0.5f;
                AddZoomDelta(-delta * (zoomSpeed / 100f), ScreenToWorldPointSafe(mid));
            }
        }

        void AddZoomDelta(float delta, Vector3 focusWorld)
        {
            float before = cam.orthographicSize;
            targetSize = Mathf.Clamp(targetSize + delta * -1f, minOrthoSize, maxOrthoSize); // delta positif → zoom in
            // menjaga titik fokus tetap di tempat yang sama (zoom to cursor/fingers)
            var after = targetSize;
            ZoomAroundPoint(focusWorld, before, after);
        }

        void ZoomAroundPoint(Vector3 worldPoint, float beforeSize, float afterSize)
        {
            if (Mathf.Approximately(beforeSize, afterSize)) return;
            var camToPoint = worldPoint - transform.position;
            float t = (afterSize / beforeSize) - 1f; // rasio perubahan
            // geser posisi sepanjang vektor menuju worldPoint
            targetPos += camToPoint * t;
            ClampTargetToBounds();
        }

        void PanByScreenDelta(Vector3 screenDelta)
        {
            if (screenDelta.sqrMagnitude < 0.0001f) return;
            // screen delta → world delta
            var a = ScreenToWorldPointSafe(Vector3.zero);
            var b = ScreenToWorldPointSafe(new Vector3(screenDelta.x, screenDelta.y, 0f));
            Vector3 worldDelta = a - b; // minus supaya drag kiri → geser kamera ke kanan (rasa natural)
            targetPos += worldDelta * panSpeed;
            ClampTargetToBounds();
        }

        void SmoothUpdate()
        {
            // Smooth zoom
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetSize, 1f - Mathf.Exp(-zoomLerp * Time.unscaledDeltaTime));
            // Smooth pan
            transform.position = Vector3.Lerp(transform.position, targetPos, 1f - Mathf.Exp(-panDamping * Time.unscaledDeltaTime));
        }

        void ApplySmoothingImmediate()
        {
            cam.orthographicSize = targetSize;
            transform.position = targetPos;
        }

        void FocusOnPoint(Vector3 worldPoint)
        {
            targetPos = new Vector3(worldPoint.x, worldPoint.y, transform.position.z);
            ClampTargetToBounds();
        }

        // ==== Bounds Helpers ====
        void ClampToBoundsImmediate()
        {
            ClampTargetToBounds();
            ApplySmoothingImmediate();
        }

        void ClampTargetToBounds()
        {
            // hitung extents kamera (ortho) dalam world
            float halfH = targetSize;
            float halfW = halfH * cam.aspect;

            float minX = worldBounds.xMin + halfW;
            float maxX = worldBounds.xMax - halfW;
            float minY = worldBounds.yMin + halfH;
            float maxY = worldBounds.yMax - halfH;

            // kalau ruangan terlalu kecil, center saja
            if (minX > maxX) { minX = maxX = worldBounds.center.x; }
            if (minY > maxY) { minY = maxY = worldBounds.center.y; }

            targetPos.x = Mathf.Clamp(targetPos.x, minX, maxX);
            targetPos.y = Mathf.Clamp(targetPos.y, minY, maxY);
            targetPos.z = transform.position.z; // jaga Z
        }

        bool TryBakeBoundsFromSource(Transform src, out Rect rect)
        {
            rect = worldBounds;
            if (!src) return false;

            // BoxCollider2D
            var bc = src.GetComponentInChildren<BoxCollider2D>();
            if (bc)
            {
                Vector2 size = Vector2.Scale(bc.size, bc.transform.lossyScale);
                Vector2 center = (Vector2)bc.transform.TransformPoint(bc.offset);
                rect = new Rect(center - size * 0.5f, size);
                return true;
            }
            // SpriteRenderer
            var sr = src.GetComponentInChildren<SpriteRenderer>();
            if (sr)
            {
                var b = sr.bounds;
                rect = new Rect((Vector2)b.min, (Vector2)(b.size));
                return true;
            }
            return false;
        }

        // ==== Utils ====
        Vector3 ScreenToWorldPointSafe(Vector3 screen)
        {
            // pastikan Z di depan kamera
            float z = -cam.transform.position.z;
            screen.z = z;
            return cam.ScreenToWorldPoint(screen);
        }

        bool IsPointerOverUI()
        {
            if (!ignoreWhenPointerOverUI) return false;

#if UNITY_ANDROID || UNITY_IOS
            if (Input.touchCount > 0)
            {
                // kalau ada salah satu touch di atas UI → block
                for (int i = 0; i < Input.touchCount; i++)
                {
                    if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(Input.touches[i].fingerId))
                        return true;
                }
            }
            return false;
#else
            // WebGL/PC
            if (EventSystem.current == null) return false;
            return EventSystem.current.IsPointerOverGameObject();
#endif
        }

        // ==== Public setters (opsional dipanggil dari Bootstrap) ====
        public void SetBounds(Rect rect) { worldBounds = rect; ClampToBoundsImmediate(); }
        public void SetZoomLimits(float min, float max) { minOrthoSize = min; maxOrthoSize = max; targetSize = Mathf.Clamp(targetSize, minOrthoSize, maxOrthoSize); ClampToBoundsImmediate(); }
        public void SetSpeeds(float pan, float zoom) { panSpeed = pan; zoomSpeed = zoom; }
    }
}
