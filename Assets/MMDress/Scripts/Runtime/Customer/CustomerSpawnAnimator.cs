// Assets/MMDress/Scripts/Runtime/Customer/Presentation/CustomerSpawnAnimator.cs
using UnityEngine;
using DG.Tweening;

namespace MMDress.Runtime.Customer.Presentation
{
    /// <summary>
    /// Animasi squash & stretch saat objek (customer) muncul.
    /// Aman untuk 2D (Sprite/RectTransform) atau 3D (Transform biasa).
    /// Tidak perlu Animator Controller; cukup DOTween.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CustomerSpawnAnimator : MonoBehaviour
    {
        [Header("Play Options")]
        [Tooltip("Jika true, animasi otomatis jalan saat di-enable/spawn.")]
        [SerializeField] private bool playOnEnable = true;
        [Tooltip("Jika true, gunakan TimeScale-independent (Realtime)")]
        [SerializeField] private bool ignoreTimeScale = false;
        [Tooltip("Opsional delay sebelum animasi mulai (detik).")]
        [SerializeField, Min(0f)] private float startDelay = 0f;

        [Header("Squash & Stretch")]
        [Tooltip("Skala awal saat spawn (X,Y). Misal 1.15, 0.85 untuk efek squash).")]
        [SerializeField] private Vector2 startScale = new Vector2(1.15f, 0.85f);
        [Tooltip("Overshoot kecil setelah settle (contoh 1.03 untuk bounce halus).")]
        [SerializeField, Min(1f)] private float overshootScale = 1.03f;

        [Header("Durasi & Easing")]
        [SerializeField, Min(0f)] private float inDuration = 0.20f;
        [SerializeField, Min(0f)] private float settleDuration = 0.10f;
        [SerializeField] private Ease inEase = Ease.OutBack;
        [SerializeField] private Ease settleEase = Ease.OutSine;

        [Header("Randomization (opsional)")]
        [Tooltip("Acak ± persen pada durasi agar terlihat hidup (0 = nonaktif).")]
        [SerializeField, Range(0f, 0.5f)] private float durationJitter = 0.1f;
        [Tooltip("Acak ± persen pada startScale agar tiap customer sedikit beda.")]
        [SerializeField, Range(0f, 0.3f)] private float scaleJitter = 0.05f;

        private Vector3 _originalScale;
        private Tween _t;

        private void Awake()
        {
            _originalScale = transform.localScale;
        }

        private void OnEnable()
        {
            if (playOnEnable)
                Play();
        }

        private void OnDisable()
        {
            _t?.Kill();
            _t = null;
            // jaga-jaga: kembalikan ke skala asli
            transform.localScale = _originalScale;
        }

        /// <summary>
        /// Panggil manual bila tidak ingin otomatis saat enable.
        /// </summary>
        public void Play()
        {
            _t?.Kill();

            // Jitter kecil biar variasi natural
            var durIn = Jitter(inDuration, durationJitter);
            var durSettle = Jitter(settleDuration, durationJitter);
            var sX = Jitter(startScale.x, scaleJitter);
            var sY = Jitter(startScale.y, scaleJitter);

            // Set skala awal (squash)
            transform.localScale = new Vector3(sX, sY, _originalScale.z);

            var seq = DOTween.Sequence();
            if (ignoreTimeScale) seq.SetUpdate(true);

            if (startDelay > 0f) seq.AppendInterval(startDelay);

            // Kembali ke skala asli dengan ease
            seq.Append(transform.DOScale(_originalScale, durIn).SetEase(inEase));

            // Bounce kecil (overshoot → balik)
            var overshoot = _originalScale * overshootScale;
            seq.Append(transform.DOScale(overshoot, durSettle).SetEase(settleEase));
            seq.Append(transform.DOScale(_originalScale, durSettle).SetEase(settleEase));

            _t = seq;
        }

        /// <summary>
        /// Helper untuk panggil dari spawner setelah Instantiate.
        /// </summary>
        public static void TryPlayOn(GameObject go)
        {
            if (!go) return;
            var anim = go.GetComponent<CustomerSpawnAnimator>();
            if (anim != null) anim.Play();
        }

        private static float Jitter(float value, float percent)
        {
            if (percent <= 0f) return value;
            float delta = value * percent;
            return value + Random.Range(-delta, delta);
        }

        private static float JitterClamped(float value, float percent, float minValue)
        {
            return Mathf.Max(minValue, Jitter(value, percent));
        }

        private void OnValidate()
        {
            inDuration = Mathf.Max(0f, inDuration);
            settleDuration = Mathf.Max(0f, settleDuration);
            overshootScale = Mathf.Max(1f, overshootScale);
            startScale.x = Mathf.Max(0.01f, startScale.x);
            startScale.y = Mathf.Max(0.01f, startScale.y);
        }

        private void Reset()
        {
            playOnEnable = true;
            ignoreTimeScale = false;
            startDelay = 0f;

            startScale = new Vector2(1.15f, 0.85f);
            overshootScale = 1.03f;

            inDuration = 0.20f;
            settleDuration = 0.10f;
            inEase = Ease.OutBack;
            settleEase = Ease.OutSine;

            durationJitter = 0.10f;
            scaleJitter = 0.05f;
        }
    }
}
