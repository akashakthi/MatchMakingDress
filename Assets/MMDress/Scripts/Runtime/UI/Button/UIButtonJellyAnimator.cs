using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

#if UNITY_TEXTMESHPRO
using TMPro;
#endif

namespace MMDress.UI.Animations
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class UIButtonJellyAnimator : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler
    {
        [Header("Target")]
        [SerializeField] private RectTransform target;

        [Header("Jelly")]
        [SerializeField] private float hoverScale = 1.05f;
        [SerializeField] private float pressedScale = 0.92f;

        [Header("Scale Limit 🔥")]
        [SerializeField] private float maxScaleMultiplier = 1.2f;

        [Header("Punch Feel")]
        [SerializeField] private float punchDuration = 0.1f;

        // =========================
        // AUDIO
        // =========================
        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip clickSound;
        [SerializeField] private AudioClip maxPitchSound;

        [Header("Pitch")]
        [SerializeField] private float basePitch = 1f;
        [SerializeField] private float pitchStep = 0.05f;
        [SerializeField] private float maxPitch = 1.6f;
        [SerializeField] private float pitchResetDelay = 0.4f;

        float _currentPitch;
        float _lastClickTime;
        bool _hitMaxPitch;

        // =========================
        // HOLD SYSTEM
        // =========================
        [Header("Hold Settings")]
        [SerializeField] private float holdStartDelay = 0.25f;
        [SerializeField] private float holdInterval = 0.08f;

        bool _isHolding;
        float _holdTimer;
        float _repeatTimer;

        Button _btn;
        Vector3 _baseScale;
        Tween _tween;

        void Awake()
        {
            _btn = GetComponent<Button>();

            if (!target) target = transform as RectTransform;
            if (!audioSource) audioSource = GetComponent<AudioSource>();

            _baseScale = target.localScale;
            _currentPitch = basePitch;
        }

        void Update()
        {
            if (!_isHolding || !_btn.interactable) return;

            _holdTimer += Time.unscaledDeltaTime;

            if (_holdTimer >= holdStartDelay)
            {
                _repeatTimer += Time.unscaledDeltaTime;

                if (_repeatTimer >= holdInterval)
                {
                    _repeatTimer = 0f;
                    TriggerClick();
                }
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!_btn.interactable) return;

            _isHolding = true;
            _holdTimer = 0f;
            _repeatTimer = 0f;

            PlayScale(pressedScale);
            TriggerClick();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _isHolding = false;
            PlayScale(hoverScale);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!_isHolding)
                PlayScale(hoverScale);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!_isHolding)
                PlayScale(1f);
        }

        void TriggerClick()
        {
            PlaySound();
            PlayPunch();
        }

        // =========================
        // AUDIO
        // =========================

        void PlaySound()
        {
            if (audioSource == null || clickSound == null) return;

            if (Time.unscaledTime - _lastClickTime > pitchResetDelay)
            {
                _currentPitch = basePitch;
                _hitMaxPitch = false;
            }

            audioSource.pitch = _currentPitch + Random.Range(-0.02f, 0.02f);
            audioSource.PlayOneShot(clickSound);

            float speedMultiplier = Mathf.Lerp(1f, 3f,
                Mathf.InverseLerp(basePitch, maxPitch, _currentPitch));

            _currentPitch = Mathf.Min(_currentPitch + pitchStep * speedMultiplier, maxPitch);

            if (_currentPitch >= maxPitch && !_hitMaxPitch)
            {
                _hitMaxPitch = true;

                if (maxPitchSound != null)
                    audioSource.PlayOneShot(maxPitchSound);
            }

            _lastClickTime = Time.unscaledTime;
        }

        // =========================
        // 🔥 SCALE FIX (NO MORE OVERGROW)
        // =========================

        void PlayPunch()
        {
            if (!target) return;

            _tween?.Kill();

            // Hit scale (dibatasi max 1.2x)
            Vector3 targetScale = _baseScale * maxScaleMultiplier;

            _tween = target.DOScale(targetScale, punchDuration)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    target.DOScale(_baseScale, punchDuration)
                        .SetEase(Ease.InQuad);
                });
        }

        // =========================
        // HOVER / PRESS SCALE
        // =========================

        void PlayScale(float scale)
        {
            if (!target) return;

            _tween?.Kill();

            Vector3 targetScale = _baseScale * scale;

            // clamp juga biar aman
            float max = maxScaleMultiplier;
            targetScale.x = Mathf.Min(targetScale.x, _baseScale.x * max);
            targetScale.y = Mathf.Min(targetScale.y, _baseScale.y * max);

            _tween = target.DOScale(targetScale, 0.15f)
                .SetEase(Ease.OutBack);
        }
    }
}