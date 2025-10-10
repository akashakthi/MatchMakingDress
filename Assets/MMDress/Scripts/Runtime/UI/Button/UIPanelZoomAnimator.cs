// Assets/MMDress/Scripts/Runtime/UI/Animations/UIPanelZoomAnimator.cs
using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;

namespace MMDress.UI.Animations
{
    /// <summary>
    /// Animator panel UI: show (zoom-in + fade) & hide (zoom-out + fade).
    /// - Bisa SetActive ON/OFF atau biarkan aktif (pakai CanvasGroup).
    /// - Blok raycast & interactable saat tersembunyi.
    /// - Timescale-independent (opsional).
    /// Cara pakai:
    ///   1) Tempel di root panel (mis. FYIPanel).
    ///   2) Hubungkan tombol "FYI" -> Show(), tombol "Back" -> Hide().
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UIPanelZoomAnimator : MonoBehaviour
    {
        public enum ActivationPolicy
        {
            SetActiveOnHide,   // sembunyikan: SetActive(false)
            KeepActiveUseCanvasGroup // sembunyikan: alpha 0 + non-interactable
        }

        [Header("Targets (auto di Reset)")]
        [SerializeField] RectTransform target;   // panel utama
        [SerializeField] CanvasGroup canvasGroup; // opsional; auto-add bila kosong

        [Header("Activation")]
        [SerializeField] ActivationPolicy activation = ActivationPolicy.KeepActiveUseCanvasGroup;
        [SerializeField] bool startHidden = true;        // kondisi awal saat OnEnable
        [SerializeField] bool updateIndependent = true;  // jalan saat timescale=0

        [Header("Show (Zoom-In)")]
        [SerializeField, Min(0f)] float showDuration = 0.28f;
        [SerializeField] Ease showEase = Ease.OutBack;
        [SerializeField] Vector3 showFromScale = new Vector3(0.75f, 0.75f, 1f);
        [SerializeField, Range(0f, 1f)] float showFromAlpha = 0f;

        [Header("Hide (Zoom-Out)")]
        [SerializeField, Min(0f)] float hideDuration = 0.22f;
        [SerializeField] Ease hideEase = Ease.InBack;
        [SerializeField] Vector3 hideToScale = new Vector3(0.75f, 0.75f, 1f);
        [SerializeField, Range(0f, 1f)] float hideToAlpha = 0f;

        [Header("Events")]
        public UnityEvent onShown;
        public UnityEvent onHidden;

        Vector3 _baseScale;
        Tween _scaleT, _alphaT;
        bool _isShown;

        void Reset()
        {
            target = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
        }

        void Awake()
        {
            if (!target) target = transform as RectTransform;
            _baseScale = target ? target.localScale : Vector3.one;

            if (!canvasGroup)
            {
                canvasGroup = GetComponent<CanvasGroup>();
                if (!canvasGroup) canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        void OnEnable()
        {
            KillTweens();

            if (startHidden)
                ApplyHiddenInstant();
            else
                ApplyShownInstant();
        }

        void OnDisable() => KillTweens();

        // === Public API ===
        public void Show()
        {
            if (_isShown) return;

            // Pastikan aktif dulu jika kebijakan setActive
            if (activation == ActivationPolicy.SetActiveOnHide && !gameObject.activeSelf)
                gameObject.SetActive(true);

            PrepareForShow(); // atur scale & alpha awal

            // Tween scale
            if (target)
                _scaleT = target.DOScale(_baseScale, showDuration)
                                .SetEase(showEase)
                                .SetUpdate(updateIndependent);

            // Tween alpha (tanpa DOTweenModuleUI)
            float a = canvasGroup ? canvasGroup.alpha : 1f;
            _alphaT = DOTween.To(() => a, v => { a = v; if (canvasGroup) canvasGroup.alpha = v; }, 1f, showDuration)
                             .SetUpdate(updateIndependent)
                             .OnComplete(() =>
                             {
                                 SetInteractable(true);
                                 _isShown = true;
                                 onShown?.Invoke();
                             });
        }

        public void Hide()
        {
            if (!_isShown && (activation == ActivationPolicy.SetActiveOnHide ? !gameObject.activeSelf : true))
                return;

            KillTweens();
            SetInteractable(false);

            // Tween scale
            if (target)
                _scaleT = target.DOScale(hideToScale, hideDuration)
                                .SetEase(hideEase)
                                .SetUpdate(updateIndependent);

            // Tween alpha
            float a = canvasGroup ? canvasGroup.alpha : 1f;
            _alphaT = DOTween.To(() => a, v => { a = v; if (canvasGroup) canvasGroup.alpha = v; }, hideToAlpha, hideDuration)
                             .SetUpdate(updateIndependent)
                             .OnComplete(() =>
                             {
                                 if (activation == ActivationPolicy.SetActiveOnHide)
                                     gameObject.SetActive(false);
                                 _isShown = false;
                                 onHidden?.Invoke();
                             });
        }

        public void Toggle()
        {
            if (activation == ActivationPolicy.SetActiveOnHide)
            {
                // Jika kebijakan setActive, cek juga activeSelf
                bool isActive = gameObject.activeSelf;
                if (_isShown || isActive)
                    Hide();
                else
                    Show();
            }
            else
            {
                // Panel selalu aktif (pakai CanvasGroup)
                if (_isShown)
                    Hide();
                else
                    Show();
            }
        }


        // === Helpers ===
        void PrepareForShow()
        {
            KillTweens();

            if (target) target.localScale = showFromScale;

            if (canvasGroup)
            {
                canvasGroup.alpha = showFromAlpha;
                // meski alpha 0, blok raycast saat anim show dimulai? biasanya iya -> ON
                SetInteractable(true); // biar bisa tangkap back segera setelah terlihat
                canvasGroup.blocksRaycasts = false; // cegah klik saat transisi awal
            }
        }

        void ApplyHiddenInstant()
        {
            if (activation == ActivationPolicy.SetActiveOnHide)
            {
                // tetap aktif saat OnEnable supaya method bisa dipanggil;
                // tapi kalau startHidden, matikan langsung.
                gameObject.SetActive(false);
                _isShown = false;
                return;
            }

            if (target) target.localScale = hideToScale;
            if (canvasGroup) canvasGroup.alpha = hideToAlpha;
            SetInteractable(false);
            _isShown = false;
        }

        void ApplyShownInstant()
        {
            if (activation == ActivationPolicy.SetActiveOnHide && !gameObject.activeSelf)
                gameObject.SetActive(true);

            if (target) target.localScale = _baseScale;
            if (canvasGroup) canvasGroup.alpha = 1f;
            SetInteractable(true);
            _isShown = true;
        }

        void SetInteractable(bool v)
        {
            if (!canvasGroup) return;
            canvasGroup.interactable = v;
            canvasGroup.blocksRaycasts = v;
        }

        void KillTweens()
        {
            _scaleT?.Kill(); _scaleT = null;
            _alphaT?.Kill(); _alphaT = null;
        }
    }
}
