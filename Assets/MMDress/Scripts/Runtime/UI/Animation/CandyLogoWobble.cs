// Assets/MMDress/Scripts/Runtime/UI/Animation/CandyLogoWobble.cs
using UnityEngine;
using DG.Tweening;

namespace MMDress.UI.Animation
{
    /// <summary>
    /// Animasi idle: bob (naik-turun, anchored), sway (rotasi Z), squash-stretch (scale).
    /// TANPA DOTweenModuleUI: anchoredPosition dianimasi via DOTween.To (Vector2).
    /// Tempelkan pada RectTransform logo.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class CandyLogoWobble : MonoBehaviour
    {
        [Header("Bob (naik-turun, px)")]
        [SerializeField, Min(0f)] float bobAmplitude = 10f;
        [SerializeField, Min(0.1f)] float bobDuration = 1.4f;

        [Header("Sway (rotasi Z, derajat)")]
        [SerializeField] float swayAngle = 4f;
        [SerializeField, Min(0.1f)] float swayDuration = 1.2f;

        [Header("Squash-Stretch")]
        [SerializeField] float squashScale = 0.96f;     // 0.96 = sedikit gepeng
        [SerializeField, Min(0.05f)] float squashDuration = 0.28f;

        [Header("Natural Variance")]
        [SerializeField] bool randomizePhase = true;

        RectTransform rt;
        Sequence loopSeq;
        Vector2 startAnchored;

        void Awake()
        {
            rt = GetComponent<RectTransform>();
            startAnchored = rt.anchoredPosition;
        }

        void OnEnable() => Play();
        void OnDisable()
        {
            if (loopSeq != null && loopSeq.IsActive()) loopSeq.Kill();
            if (rt)
            {
                rt.anchoredPosition = startAnchored;
                rt.localRotation = Quaternion.identity;
                rt.localScale = Vector3.one;
            }
        }

        public void Play()
        {
            if (loopSeq != null && loopSeq.IsActive()) loopSeq.Kill();

            float rnd = randomizePhase ? Random.Range(0f, 0.6f) : 0f;

            // --- Bob (anchoredPosition.y) menggunakan DOTween.To (Vector2) ---
            Tween bob = null;
            {
                var up = new Vector2(startAnchored.x, startAnchored.y + bobAmplitude);
                // yoyo manual via sequence agar tetap pakai anchoredPosition tanpa ModuleUI
                Sequence bobSeq = DOTween.Sequence().SetDelay(rnd);
                bobSeq.Append(DOTween.To(() => rt.anchoredPosition, v => rt.anchoredPosition = v, up, bobDuration)
                                     .SetEase(Ease.InOutSine));
                bobSeq.Append(DOTween.To(() => rt.anchoredPosition, v => rt.anchoredPosition = v, startAnchored, bobDuration)
                                     .SetEase(Ease.InOutSine));
                bobSeq.SetLoops(-1, LoopType.Restart);
                bob = bobSeq;
            }

            // --- Sway (rotasi Z) ---
            Tween sway = rt.DOLocalRotate(new Vector3(0, 0, swayAngle), swayDuration)
                           .SetEase(Ease.InOutSine)
                           .SetLoops(-1, LoopType.Yoyo)
                           .SetDelay(rnd);

            // --- Squash-Stretch (scale) ---
            Sequence squash = DOTween.Sequence()
                .Append(rt.DOScale(new Vector3(1f + (1f - squashScale), squashScale, 1f), squashDuration).SetEase(Ease.InOutSine))
                .Append(rt.DOScale(Vector3.one, squashDuration).SetEase(Ease.InOutSine))
                .SetLoops(-1, LoopType.Yoyo)
                .SetDelay(rnd * 0.5f);

            loopSeq = DOTween.Sequence();
            loopSeq.Join(bob);
            loopSeq.Join(sway);
            loopSeq.Join(squash);
            loopSeq.OnKill(() => { if (rt) rt.anchoredPosition = startAnchored; });
        }
    }
}
