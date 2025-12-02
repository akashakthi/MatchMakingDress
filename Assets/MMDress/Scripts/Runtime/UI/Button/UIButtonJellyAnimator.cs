// Assets/MMDress/Scripts/Runtime/UI/Button/UIButtonJellyAnimator.cs
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

// NOTE: TMP optional. File akan tetap compile walau TMP belum terpasang.
// Saat TMP ada, symbol UNITY_TEXTMESHPRO otomatis diset Unity.
#if UNITY_TEXTMESHPRO
using TMPro;
#endif

namespace MMDress.UI.Animations
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class UIButtonJellyAnimator : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler,
        ISelectHandler, IDeselectHandler
    {
        [Header("Targets (auto di Reset)")]
        [SerializeField] private RectTransform target;
        [SerializeField] private Image targetImage;
#if UNITY_TEXTMESHPRO
        [SerializeField] private TMP_Text targetText;
#endif

        [Header("Colors")]
        [SerializeField] private bool tintImage = true;
        [SerializeField] private bool tintText = true;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color hoverColor = Color.white;
        [SerializeField] private Color pressedColor = new Color(0.95f, 0.95f, 0.95f, 1f);
        [SerializeField] private Color disabledColor = new Color(1f, 1f, 1f, 0.5f);

        [Header("Jelly Settings")]
        [SerializeField, Min(0.01f)] private float hoverScale = 1.06f;
        [SerializeField, Min(0.01f)] private float pressedScale = 0.94f;
        [SerializeField, Min(0f)] private float durHover = 0.18f;
        [SerializeField, Min(0f)] private float durPressed = 0.12f;
        [SerializeField] private Ease easeHover = Ease.OutBack;
        [SerializeField] private Ease easePressed = Ease.OutBack;

        [Header("Click Pop (Punch)")]
        [SerializeField] private Vector3 punch = new Vector3(0.18f, 0.18f, 0f);
        [SerializeField, Min(1)] private int vibrato = 12;
        [SerializeField, Range(0f, 1f)] private float elasticity = 0.8f;
        [SerializeField, Min(0f)] private float durPunch = 0.28f;

        // --- BAGIAN BARU: AUDIO ---
        [Header("Audio")]
        [Tooltip("Drag AudioSource di sini. Jika kosong, script akan mencari di GameObject ini.")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip clickSound;
        // --------------------------

        [Header("Misc")]
        [SerializeField] private bool updateIndependent = true;

        Button _btn;
        Vector3 _baseScale;
        Tween _scaleTween, _imgTween;
#if UNITY_TEXTMESHPRO
        Tween _txtTween;
#endif

        void Reset()
        {
            _btn = GetComponent<Button>();
            if (!target) target = GetComponent<RectTransform>();
            if (!targetImage) targetImage = GetComponent<Image>();
#if UNITY_TEXTMESHPRO
            if (!targetText)  targetText = GetComponentInChildren<TMP_Text>(true);
#endif
            // Auto find AudioSource saat di-reset di editor
            if (!audioSource) audioSource = GetComponent<AudioSource>();
        }

        void Awake()
        {
            _btn = GetComponent<Button>();
            if (!target) target = transform as RectTransform;
            _baseScale = target ? target.localScale : Vector3.one;

            if (tintImage && targetImage) targetImage.color = normalColor;
#if UNITY_TEXTMESHPRO
            if (tintText  && targetText)  targetText.color  = normalColor;
#endif
            // Fallback jika audioSource lupa diassign di inspector
            if (!audioSource) audioSource = GetComponent<AudioSource>();

            _btn.onClick.AddListener(OnClicked);
        }

        void OnEnable()
        {
            KillAll();
            ApplyStateInstant(_btn && _btn.interactable ? ButtonState.Normal : ButtonState.Disabled);
        }

        void OnDisable() => KillAll();

        void OnDestroy()
        {
            if (_btn) _btn.onClick.RemoveListener(OnClicked);
            KillAll();
        }

        enum ButtonState { Normal, Hover, Pressed, Disabled }

        public void OnPointerEnter(PointerEventData e) { if (IsInteractable()) PlayState(ButtonState.Hover); }
        public void OnPointerExit(PointerEventData e) { if (IsInteractable()) PlayState(ButtonState.Normal); }
        public void OnPointerDown(PointerEventData e) { if (IsInteractable()) PlayState(ButtonState.Pressed); }
        public void OnPointerUp(PointerEventData e)
        {
            if (!IsInteractable()) return;
            PlayState(IsPointerOverMe(e) ? ButtonState.Hover : ButtonState.Normal);
        }
        public void OnSelect(BaseEventData e) { if (IsInteractable()) PlayState(ButtonState.Hover); }
        public void OnDeselect(BaseEventData e) { if (IsInteractable()) PlayState(ButtonState.Normal); }

        public void SetInteractable(bool v)
        {
            if (_btn) _btn.interactable = v;
            PlayState(v ? ButtonState.Normal : ButtonState.Disabled);
        }

        void PlayState(ButtonState s)
        {
            KillAll();
            switch (s)
            {
                case ButtonState.Normal:
                    TweenScale(_baseScale, durHover, easeHover);
                    TweenColors(normalColor, durHover);
                    break;
                case ButtonState.Hover:
                    TweenScale(_baseScale * hoverScale, durHover, easeHover);
                    TweenColors(hoverColor, durHover);
                    break;
                case ButtonState.Pressed:
                    TweenScale(_baseScale * pressedScale, durPressed, easePressed);
                    TweenColors(pressedColor, durPressed);
                    break;
                case ButtonState.Disabled:
                    ApplyStateInstant(ButtonState.Disabled);
                    break;
            }
        }

        void ApplyStateInstant(ButtonState s)
        {
            KillAll();
            switch (s)
            {
                case ButtonState.Disabled:
                    if (target) target.localScale = _baseScale;
                    SetColorsInstant(disabledColor);
                    break;
                case ButtonState.Normal:
                    if (target) target.localScale = _baseScale;
                    SetColorsInstant(normalColor);
                    break;
                case ButtonState.Hover:
                    if (target) target.localScale = _baseScale * hoverScale;
                    SetColorsInstant(hoverColor);
                    break;
                case ButtonState.Pressed:
                    if (target) target.localScale = _baseScale * pressedScale;
                    SetColorsInstant(pressedColor);
                    break;
            }
        }

        void OnClicked()
        {
            if (!IsInteractable()) return;

            KillAll();

            // --- AUDIO PLAYBACK ---
            if (audioSource != null && clickSound != null)
            {
                audioSource.PlayOneShot(clickSound);
            }
            // ----------------------

            if (target)
            {
                _scaleTween = target.DOPunchScale(punch, durPunch, vibrato, elasticity)
                    .SetUpdate(updateIndependent)
                    .OnComplete(() =>
                    {
                        TweenScale(_baseScale * hoverScale, durHover, easeHover);
                        TweenColors(hoverColor, durHover);
                    });
            }

#if UNITY_TEXTMESHPRO
            if (targetText)
            {
                targetText.transform.DOPunchScale(punch * 0.6f, durPunch * 0.9f, vibrato, elasticity)
                    .SetUpdate(updateIndependent);
            }
#endif
        }

        // --- Tween Helpers (tanpa Module UI/TMP) ---
        void TweenScale(Vector3 to, float duration, Ease ease)
        {
            if (!target) return;
            _scaleTween = target.DOScale(to, duration).SetEase(ease).SetUpdate(updateIndependent);
        }

        void TweenColors(Color to, float duration)
        {
            if (tintImage && targetImage)
                _imgTween = TweenGraphicColor(targetImage, to, duration);

#if UNITY_TEXTMESHPRO
            if (tintText && targetText)
                _txtTween = TweenGraphicColor(targetText, to, duration);
#endif
        }

        void SetColorsInstant(Color c)
        {
            if (tintImage && targetImage) targetImage.color = c;
#if UNITY_TEXTMESHPRO
            if (tintText  && targetText)  targetText.color  = c;
#endif
        }

        // Generic color tween untuk Graphic & TMP_Text via DOTween.To
        Tween TweenGraphicColor(Graphic g, Color to, float duration)
        {
            Color start = g.color;
            return DOTween.To(() => start, v => { start = v; g.color = v; }, to, duration)
                          .SetUpdate(updateIndependent);
        }

#if UNITY_TEXTMESHPRO
        Tween TweenGraphicColor(TMP_Text t, Color to, float duration)
        {
            Color start = t.color;
            return DOTween.To(() => start, v => { start = v; t.color = v; }, to, duration)
                          .SetUpdate(updateIndependent);
        }
#endif

        void KillAll()
        {
            _scaleTween?.Kill();
            _imgTween?.Kill();
#if UNITY_TEXTMESHPRO
            _txtTween?.Kill();
            _txtTween = null;
#endif
            _scaleTween = _imgTween = null;
        }

        bool IsInteractable() => _btn && _btn.interactable;

        bool IsPointerOverMe(PointerEventData e)
        {
            if (e == null) return false;
            var results = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(e, results);
            for (int i = 0; i < results.Count; i++)
                if (results[i].gameObject == gameObject) return true;
            return false;
        }
    }
}