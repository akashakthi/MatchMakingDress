// Assets/MMDress/Scripts/Runtime/UI/Animations/UIPageZoomFader.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;

namespace MMDress.UI.Animations
{
    /// <summary>
    /// Page switcher untuk beberapa child panel (Batik-1..N) dengan anim zoom+fade.
    /// - Simpan daftar pages (RectTransform tiap "Batik-x").
    /// - Next/Prev/GoTo() untuk navigasi.
    /// - Opsional loop di ujung.
    /// - Tanpa DOTweenModuleUI (alpha via DOTween.To).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UIPageZoomFader : MonoBehaviour
    {
        public enum ChildActivation { KeepActiveUseCanvasGroup, SetActiveOnHide }

        [Header("Pages")]
        [Tooltip("Urutkan sesuai sequence (Batik-1, Batik-2, ...).")]
        [SerializeField] private List<RectTransform> pages = new();
        [SerializeField, Min(0)] private int startIndex = 0;
        [SerializeField] private ChildActivation childActivation = ChildActivation.KeepActiveUseCanvasGroup;
        [SerializeField] private bool loop = false;
        [SerializeField] private bool updateIndependent = true;

        [Header("Show (Zoom-In)")]
        [SerializeField, Min(0f)] private float showDuration = 0.25f;
        [SerializeField] private Ease showEase = Ease.OutBack;
        [SerializeField] private Vector3 showFromScale = new(0.85f, 0.85f, 1f);
        [SerializeField, Range(0f, 1f)] private float showFromAlpha = 0f;

        [Header("Hide (Zoom-Out)")]
        [SerializeField, Min(0f)] private float hideDuration = 0.20f;
        [SerializeField] private Ease hideEase = Ease.InBack;
        [SerializeField] private Vector3 hideToScale = new(0.85f, 0.85f, 1f);
        [SerializeField, Range(0f, 1f)] private float hideToAlpha = 0f;

        [Header("Events")]
        public UnityEvent<int> onPageShown;  // callback index baru
        public UnityEvent<int> onPageHidden; // callback index lama

        int _index = -1;
        bool _transitioning;
        readonly Dictionary<RectTransform, CanvasGroup> _cg = new();
        readonly Dictionary<RectTransform, Tween> _scaleTween = new();
        readonly Dictionary<RectTransform, Tween> _alphaTween = new();

        void Reset()
        {
            // auto-isi semua child langsung
            pages.Clear();
            foreach (Transform t in transform)
                if (t is RectTransform rt) pages.Add(rt);
        }

        void Awake()
        {
            // siapkan CanvasGroup tiap page
            for (int i = 0; i < pages.Count; i++)
            {
                var rt = pages[i];
                if (!rt) continue;
                var cg = rt.GetComponent<CanvasGroup>();
                if (!cg) cg = rt.gameObject.AddComponent<CanvasGroup>();
                _cg[rt] = cg;
            }
        }

        void OnEnable()
        {
            // set semua hidden kecuali startIndex
            for (int i = 0; i < pages.Count; i++)
            {
                if (!pages[i]) continue;
                if (i == Mathf.Clamp(startIndex, 0, pages.Count - 1))
                    ApplyShownInstant(pages[i]);
                else
                    ApplyHiddenInstant(pages[i]);
            }
            _index = Mathf.Clamp(startIndex, 0, pages.Count - 1);
        }

        // === Public API ===
        public void Next()
        {
            if (_transitioning || pages.Count == 0) return;
            int target = _index + 1;
            if (target >= pages.Count)
            {
                if (!loop) return;
                target = 0;
            }
            GoTo(target);
        }

        public void Prev()
        {
            if (_transitioning || pages.Count == 0) return;
            int target = _index - 1;
            if (target < 0)
            {
                if (!loop) return;
                target = pages.Count - 1;
            }
            GoTo(target);
        }

        public void GoTo(int targetIndex)
        {
            if (_transitioning || targetIndex == _index) return;
            if (targetIndex < 0 || targetIndex >= pages.Count) return;

            var from = (_index >= 0 && _index < pages.Count) ? pages[_index] : null;
            var to = pages[targetIndex];

            _transitioning = true;

            // siapkan target (aktifkan jika setActive policy)
            if (childActivation == ChildActivation.SetActiveOnHide && to && !to.gameObject.activeSelf)
                to.gameObject.SetActive(true);
            PrepareForShow(to);

            // hide yang lama
            if (from) PlayHide(from, () =>
            {
                if (childActivation == ChildActivation.SetActiveOnHide && from)
                    from.gameObject.SetActive(false);
                onPageHidden?.Invoke(_index);
            });

            // show yang baru
            PlayShow(to, () =>
            {
                int prev = _index;
                _index = targetIndex;
                onPageShown?.Invoke(_index);
                _transitioning = false;
            });
        }

        // === Anim core ===
        void PlayShow(RectTransform rt, TweenCallback onComplete)
        {
            if (!rt) { onComplete?.Invoke(); return; }
            Kill(rt);

            // scale
            _scaleTween[rt] = rt.DOScale(Vector3.one, showDuration)
                                .SetEase(showEase)
                                .SetUpdate(updateIndependent);

            // alpha (via DOTween.To)
            var cg = _cg[rt];
            float a = cg.alpha;
            _alphaTween[rt] = DOTween.To(() => a, v => { a = v; cg.alpha = v; }, 1f, showDuration)
                                     .SetUpdate(updateIndependent)
                                     .OnComplete(onComplete);
            cg.interactable = true;
            cg.blocksRaycasts = true;
        }

        void PlayHide(RectTransform rt, TweenCallback onComplete)
        {
            if (!rt) { onComplete?.Invoke(); return; }
            Kill(rt);

            // scale
            _scaleTween[rt] = rt.DOScale(hideToScale, hideDuration)
                                .SetEase(hideEase)
                                .SetUpdate(updateIndependent);

            // alpha
            var cg = _cg[rt];
            float a = cg.alpha;
            _alphaTween[rt] = DOTween.To(() => a, v => { a = v; cg.alpha = v; }, hideToAlpha, hideDuration)
                                     .SetUpdate(updateIndependent)
                                     .OnComplete(onComplete);
            cg.interactable = false;
            cg.blocksRaycasts = false;
        }

        // === Instant states ===
        void ApplyHiddenInstant(RectTransform rt)
        {
            if (!rt) return;
            EnsureCG(rt);
            rt.localScale = hideToScale;
            _cg[rt].alpha = hideToAlpha;
            _cg[rt].interactable = false;
            _cg[rt].blocksRaycasts = false;
            if (childActivation == ChildActivation.SetActiveOnHide)
                rt.gameObject.SetActive(false);
        }

        void ApplyShownInstant(RectTransform rt)
        {
            if (!rt) return;
            EnsureCG(rt);
            if (childActivation == ChildActivation.SetActiveOnHide && !rt.gameObject.activeSelf)
                rt.gameObject.SetActive(true);
            rt.localScale = Vector3.one;
            _cg[rt].alpha = 1f;
            _cg[rt].interactable = true;
            _cg[rt].blocksRaycasts = true;
        }

        void PrepareForShow(RectTransform rt)
        {
            if (!rt) return;
            EnsureCG(rt);
            rt.localScale = showFromScale;
            _cg[rt].alpha = showFromAlpha;
            _cg[rt].interactable = false;
            _cg[rt].blocksRaycasts = false;
        }

        void EnsureCG(RectTransform rt)
        {
            if (_cg.ContainsKey(rt) && _cg[rt]) return;
            var cg = rt.GetComponent<CanvasGroup>();
            if (!cg) cg = rt.gameObject.AddComponent<CanvasGroup>();
            _cg[rt] = cg;
        }

        void Kill(RectTransform rt)
        {
            if (!rt) return;
            if (_scaleTween.TryGetValue(rt, out var s) && s != null) s.Kill();
            if (_alphaTween.TryGetValue(rt, out var a) && a != null) a.Kill();
            _scaleTween[rt] = null;
            _alphaTween[rt] = null;
        }
    }
}
