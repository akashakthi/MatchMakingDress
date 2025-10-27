// Assets/MMDress/Scripts/Runtime/UI/Animation/UIButtonFancy.cs
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

namespace MMDress.UI.Animation
{
    /// <summary>
    /// Satu script untuk efek tombol:
    /// - Hover (scale & tilt)
    /// - Click/Press (squash + bounce)
    /// - VFX & SFX opsional
    /// - Tint warna opsional (Image / SpriteRenderer), TANPA DOTweenModuleUI/Sprite (pakai DOTween.To)
    /// Tempelkan ke GameObject visual tombol (child yang menampung grafik).
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class UIButtonFancy : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler,
        ISubmitHandler
    {
        [Header("Target Graphic (opsional, auto-detect)")]
        [SerializeField] Image uiImage;
        [SerializeField] SpriteRenderer spriteRenderer;

        [Header("Hover")]
        [SerializeField] float hoverScale = 1.08f;
        [SerializeField] float hoverTiltZ = 3f;
        [SerializeField, Min(0.01f)] float hoverDur = 0.18f;
        [SerializeField] Ease hoverEase = Ease.OutQuad;

        [Header("Press / Click")]
        [SerializeField] float pressedScale = 0.92f;
        [SerializeField, Min(0.01f)] float pressDur = 0.06f;
        [SerializeField] float releaseBounceScale = 1.06f;
        [SerializeField, Min(0.01f)] float releaseDur = 0.16f;
        [SerializeField] Ease releaseEase = Ease.OutBack;

        [Header("Tint (opsional)")]
        [SerializeField] bool enableTint = false;
        [SerializeField] Color normalColor = Color.white;
        [SerializeField] Color hoverColor = new Color(1f, 1f, 1f, 0.95f);
        [SerializeField] Color pressedColor = new Color(0.95f, 0.95f, 0.95f, 1f);
        [SerializeField, Min(0.01f)] float tintDur = 0.12f;
        [SerializeField] Ease tintEase = Ease.OutQuad;

        [Header("VFX/SFX (opsional)")]
        [SerializeField] GameObject hoverVfxPrefab;
        [SerializeField] GameObject clickVfxPrefab;
        [SerializeField] Transform vfxParent;
        [SerializeField, Min(0f)] float vfxAutoDestroy = 1.5f;
        [SerializeField] AudioSource clickSfx;

        RectTransform rt;
        bool hasImage, hasSprite;
        Color initialColor;

        void Awake()
        {
            rt = GetComponent<RectTransform>();
            if (!uiImage) uiImage = GetComponent<Image>();
            if (!spriteRenderer) spriteRenderer = GetComponent<SpriteRenderer>();
            hasImage = uiImage;
            hasSprite = spriteRenderer;

            if (hasImage) initialColor = uiImage.color;
            else if (hasSprite) initialColor = spriteRenderer.color;
            else initialColor = normalColor;
        }

        void OnEnable() => ResetVisual(true);
        void OnDisable()
        {
            KillTweens();
            ResetVisual(false);
        }

        // ===== Pointer Events =====
        public void OnPointerEnter(PointerEventData e)
        {
            KillTweens();
            rt.DOScale(hoverScale, hoverDur).SetEase(hoverEase);
            rt.DOLocalRotate(new Vector3(0, 0, hoverTiltZ), hoverDur).SetEase(hoverEase);
            if (enableTint) TweenColor(hoverColor);
            SpawnVfx(hoverVfxPrefab);
        }

        public void OnPointerExit(PointerEventData e)
        {
            KillTweens();
            rt.DOScale(1f, hoverDur).SetEase(hoverEase);
            rt.DOLocalRotate(Vector3.zero, hoverDur).SetEase(hoverEase);
            if (enableTint) TweenColor(normalColor);
        }

        public void OnPointerDown(PointerEventData e)
        {
            KillTweens(true);
            rt.DOScale(pressedScale, pressDur).SetEase(Ease.OutQuad);
            if (enableTint) TweenColor(pressedColor);
        }

        public void OnPointerUp(PointerEventData e) => PlayRelease();
        public void OnSubmit(BaseEventData e) => PlayRelease();

        // ===== Internals =====
        void PlayRelease()
        {
            KillTweens(true);
            var seq = DOTween.Sequence();
            seq.Append(rt.DOScale(releaseBounceScale, releaseDur).SetEase(releaseEase));
            seq.Append(rt.DOScale(1f, 0.10f).SetEase(Ease.OutQuad));
            if (enableTint) TweenColor(hoverColor); // kembali ke state hover
            SpawnVfx(clickVfxPrefab);
            if (clickSfx) clickSfx.Play();
        }

        void TweenColor(Color target)
        {
            if (hasImage)
            {
                var from = uiImage.color;
                DOTween.To(() => from, v => { from = v; uiImage.color = v; }, target, tintDur)
                       .SetEase(tintEase);
            }
            else if (hasSprite)
            {
                var from = spriteRenderer.color;
                DOTween.To(() => from, v => { from = v; spriteRenderer.color = v; }, target, tintDur)
                       .SetEase(tintEase);
            }
        }

        void SpawnVfx(GameObject prefab)
        {
            if (!prefab) return;
            var parent = vfxParent ? vfxParent : transform;
            var go = Instantiate(prefab, parent);
            go.transform.position = transform.position;
            go.transform.rotation = Quaternion.identity;
            if (vfxAutoDestroy > 0f) Destroy(go, vfxAutoDestroy);
        }

        void KillTweens(bool includeChildren = false)
        {
            rt.DOKill(includeChildren);
            if (hasImage) uiImage.DOKill();
            if (hasSprite) spriteRenderer.DOKill();
        }

        void ResetVisual(bool toNormal)
        {
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;

            if (enableTint && toNormal) SetColorImmediate(normalColor);
            else if (!toNormal) SetColorImmediate(initialColor);
        }

        void SetColorImmediate(Color c)
        {
            if (hasImage) uiImage.color = c;
            if (hasSprite) spriteRenderer.color = c;
        }
    }
}
