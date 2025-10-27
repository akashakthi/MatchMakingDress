using UnityEngine;

namespace MMDress.UI.Layout
{
    /// <summary>
    /// Menjaga panel tetap di dalam Screen.safeArea.
    /// - Mode Anchors: mengatur anchorMin/Max agar tepat mengikuti safe area.
    /// - Mode Offsets: anchor tetap full-stretch, offsetMin/Max diatur berdasar margin safe area.
    /// Bekerja di Editor + runtime (otomatis re-apply saat resolusi/orientasi/safeArea berubah).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaController : MonoBehaviour
    {
        public enum ApplyMode { Anchors, Offsets }

        [Header("Target")]
        [SerializeField] RectTransform target;      // default: self
        [SerializeField] ApplyMode applyMode = ApplyMode.Anchors;

        [Header("Sisi yang dipatuhi")]
        public bool respectLeft = true;
        public bool respectRight = true;
        public bool respectTop = true;
        public bool respectBottom = true;

        [Header("Perilaku")]
        [Tooltip("Apply saat enable, dengan 1-frame delay agar CanvasScaler siap.")]
        public bool applyOnEnable = true;
        [Tooltip("Otomatis apply saat ada perubahan resolusi/orientasi/safeArea.")]
        public bool autoUpdate = true;

        [Header("Editor")]
        [Tooltip("Tetap pakai safeArea di Editor (cocok dengan Device Simulator).")]
        public bool simulateInEditor = true;

        // cache untuk debounce
        Rect _lastSafe;
        Vector2Int _lastScreen;
        ScreenOrientation _lastOri;
        bool _pendingDelayedApply;

        void Reset()
        {
            target = GetComponent<RectTransform>();
        }

        void Awake()
        {
            if (!target) target = GetComponent<RectTransform>();
        }

        void OnEnable()
        {
            if (applyOnEnable)
            {
                Apply();
                // sekali lagi frame berikutnya (CanvasScaler + Device Simulator settle)
                _pendingDelayedApply = true;
            }
        }

        void Update()
        {
            if (_pendingDelayedApply)
            {
                _pendingDelayedApply = false;
#if UNITY_EDITOR
                Canvas.ForceUpdateCanvases();
#endif
                Apply();
            }

            if (!autoUpdate) return;

            var safe = GetCurrentSafeArea();
            var scr = new Vector2Int(Screen.width, Screen.height);
            var ori = Screen.orientation;

            if (safe != _lastSafe || scr != _lastScreen || ori != _lastOri)
            {
                Apply();
            }
        }

        public void Apply()
        {
            if (!target) target = GetComponent<RectTransform>();

            Rect screenSafe = GetCurrentSafeArea();            // dalam pixel layar
            // Jika ada sisi yang tidak dipatuhi → gunakan tepi layar
            if (!respectLeft) screenSafe.xMin = 0f;
            if (!respectRight) screenSafe.xMax = Screen.width;
            if (!respectTop) screenSafe.yMax = Screen.height;
            if (!respectBottom) screenSafe.yMin = 0f;

            screenSafe.xMin = Mathf.Clamp(screenSafe.xMin, 0, Screen.width);
            screenSafe.xMax = Mathf.Clamp(screenSafe.xMax, 0, Screen.width);
            screenSafe.yMin = Mathf.Clamp(screenSafe.yMin, 0, Screen.height);
            screenSafe.yMax = Mathf.Clamp(screenSafe.yMax, 0, Screen.height);

            if (applyMode == ApplyMode.Anchors)
            {
                // Konversi ke normalized anchors (0..1)
                Vector2 aMin = new Vector2(
                    Screen.width > 0 ? screenSafe.xMin / Screen.width : 0f,
                    Screen.height > 0 ? screenSafe.yMin / Screen.height : 0f);

                Vector2 aMax = new Vector2(
                    Screen.width > 0 ? screenSafe.xMax / Screen.width : 1f,
                    Screen.height > 0 ? screenSafe.yMax / Screen.height : 1f);

                target.anchorMin = aMin;
                target.anchorMax = aMax;
                target.offsetMin = Vector2.zero;
                target.offsetMax = Vector2.zero;
            }
            else // ApplyMode.Offsets
            {
                // Anchors tetap full-stretch; hitung margin pixel relatif ke parent
                var parent = target.parent as RectTransform;
                if (!parent)
                {
                    // fallback: pakai anchors
                    applyMode = ApplyMode.Anchors;
                    Apply();
                    return;
                }

                // Margin pixel di tepi layar:
                float leftMargin = screenSafe.xMin;                    // px dari kiri layar
                float rightMargin = Screen.width - screenSafe.xMax;    // px dari kanan
                float bottomMargin = screenSafe.yMin;                    // px dari bawah
                float topMargin = Screen.height - screenSafe.yMax;    // px dari atas

                // Normalisasikan margin → skalakan ke ukuran parent rect
                Vector2 parentSize = parent.rect.size;
                Vector2 aMin = target.anchorMin;
                Vector2 aMax = target.anchorMax;

                // Kita butuh target full-stretch agar offset bekerja seperti padding
                target.anchorMin = new Vector2(0f, 0f);
                target.anchorMax = new Vector2(1f, 1f);

                // offset dalam ruang parent = marginNormalized * parentSize
                float offLeft = (parentSize.x > 0 ? leftMargin / Screen.width : 0f) * parentSize.x;
                float offRight = (parentSize.x > 0 ? rightMargin / Screen.width : 0f) * parentSize.x;
                float offBottom = (parentSize.y > 0 ? bottomMargin / Screen.height : 0f) * parentSize.y;
                float offTop = (parentSize.y > 0 ? topMargin / Screen.height : 0f) * parentSize.y;

                target.offsetMin = new Vector2(offLeft, offBottom);
                target.offsetMax = new Vector2(-offRight, -offTop);

                // (Opsional) jika kamu ingin mempertahankan anchor lama: hapus 4 baris set anchorMin/Max di atas,
                // tapi pastikan target memang full-stretch agar offset bekerja seperti padding.
            }

            _lastSafe = screenSafe;
            _lastScreen = new Vector2Int(Screen.width, Screen.height);
            _lastOri = Screen.orientation;
        }

        Rect GetCurrentSafeArea()
        {
#if UNITY_EDITOR
            if (!simulateInEditor)
                return new Rect(0, 0, Screen.width, Screen.height);
#endif
            return Screen.safeArea;
        }
    }
}
