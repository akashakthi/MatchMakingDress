// Assets/MMDress/Scripts/Runtime/UI/Credit/CreditTypewriter.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;
using TMPro;

namespace MMDress.UI.Credit
{
    public sealed class CreditTypewriter : MonoBehaviour
    {
        public enum PlayMode { PerEntry, TwoPhase }

        [Header("Targets (urut dari atas)")]
        public List<TMP_Text> roles = new List<TMP_Text>();
        public List<TMP_Text> names = new List<TMP_Text>();

        [Header("Mode")]
        public PlayMode playMode = PlayMode.PerEntry;

        [Header("Typing")]
        [Min(1f)] public float charsPerSec = 32f;
        [Min(0f)] public float gapRoleToName = 0.25f;
        [Min(0f)] public float gapBetweenEntries = 0.35f;
        [Min(0f)] public float gapBetweenPhases = 0.50f;

        [Header("Slide-in (opsional)")]
        public bool enableSlideIn = true;
        public float slideOffsetX = -40f;
        [Min(0f)] public float slideDuration = 0.20f;
        public Ease slideEase = Ease.OutQuad;

        [Header("SFX (opsional)")]
        public AudioSource sfxSource;
        public AudioClip typeSfx;
        [Min(1)] public int sfxEveryNChars = 2;

        [Header("Control")]
        public bool autoPlayOnEnable = true;
        public bool allowSkip = true;
        public KeyCode skipKey = KeyCode.Space;

        [Header("Events")]
        public UnityEvent onSequenceFinished;

        bool _running, _skip;

        // cache base; JANGAN di-overwrite saat reset biasa
        readonly List<Vector2> _baseRolePos = new();
        readonly List<Vector2> _baseNamePos = new();
        bool _baseCached;

        void OnEnable()
        {
            if (autoPlayOnEnable) PlayFromStart();
        }

        // === Public API ===
        public void PlayFromStart()
        {
            // pastikan base dicapture sekali (setelah layout siap)
            if (!_baseCached) StartCoroutine(CaptureBaseThenStart());
            else { ResetForReplay(); Play(); }
        }

        IEnumerator CaptureBaseThenStart()
        {
            // tunggu 1 frame supaya VerticalLayoutGroup/ContentSizeFitter selesai
            yield return null;
            Canvas.ForceUpdateCanvases();
            CaptureBasePositionsOnce();
            ResetForReplay();
            Play();
        }

        /// <summary> Paksa recache base (pakai jika layout berubah drastis: rotate, font size, dsb.). </summary>
        public void RecalculateBasePositions()
        {
            _baseRolePos.Clear();
            _baseNamePos.Clear();
            _baseCached = false;
            CaptureBasePositionsOnce();
        }

        public void Play()
        {
            if (_running) return;
            StopAllCoroutines();
            StartCoroutine(Run());
        }

        public void ResetForReplay()
        {
            StopAllCoroutines();
            _running = false;
            _skip = false;

            if (!_baseCached) CaptureBasePositionsOnce();

            int n = Mathf.Min(roles.Count, names.Count);
            for (int i = 0; i < n; i++)
            {
                var r = roles[i];
                if (r)
                {
                    r.ForceMeshUpdate();
                    r.maxVisibleCharacters = 0;
                    r.rectTransform.anchoredPosition = enableSlideIn
                        ? _baseRolePos[i] + new Vector2(slideOffsetX, 0f)
                        : _baseRolePos[i];
                }

                var nm = names[i];
                if (nm)
                {
                    nm.ForceMeshUpdate();
                    nm.maxVisibleCharacters = 0;
                    nm.rectTransform.anchoredPosition = enableSlideIn
                        ? _baseNamePos[i] + new Vector2(slideOffsetX, 0f)
                        : _baseNamePos[i];
                }
            }
        }

        public void Skip() => _skip = true;

        // === Core ===
        IEnumerator Run()
        {
            _running = true;
            _skip = false;

            int n = Mathf.Min(roles.Count, names.Count);

            if (playMode == PlayMode.PerEntry)
            {
                for (int i = 0; i < n; i++)
                {
                    if (roles[i])
                    {
                        if (enableSlideIn) SlideToBase(roles[i].rectTransform, _baseRolePos[i]);
                        yield return TypeText(roles[i]);
                    }

                    yield return WaitOrSkip(gapRoleToName);

                    if (names[i])
                    {
                        if (enableSlideIn) SlideToBase(names[i].rectTransform, _baseNamePos[i]);
                        yield return TypeText(names[i]);
                    }

                    yield return WaitOrSkip(gapBetweenEntries);
                }
            }
            else // TwoPhase
            {
                for (int i = 0; i < n; i++)
                {
                    if (roles[i])
                    {
                        if (enableSlideIn) SlideToBase(roles[i].rectTransform, _baseRolePos[i]);
                        yield return TypeText(roles[i]);
                    }
                    yield return WaitOrSkip(gapBetweenEntries);
                }

                yield return WaitOrSkip(gapBetweenPhases);

                for (int i = 0; i < n; i++)
                {
                    if (names[i])
                    {
                        if (enableSlideIn) SlideToBase(names[i].rectTransform, _baseNamePos[i]);
                        yield return TypeText(names[i]);
                    }
                    yield return WaitOrSkip(gapBetweenEntries);
                }
            }

            _running = false;
            onSequenceFinished?.Invoke();
        }

        // === Helpers ===
        void CaptureBasePositionsOnce()
        {
            if (_baseCached) return;

            _baseRolePos.Clear();
            _baseNamePos.Clear();

            for (int i = 0; i < roles.Count; i++)
                _baseRolePos.Add(roles[i] ? roles[i].rectTransform.anchoredPosition : Vector2.zero);

            for (int i = 0; i < names.Count; i++)
                _baseNamePos.Add(names[i] ? names[i].rectTransform.anchoredPosition : Vector2.zero);

            _baseCached = true;
        }

        void SlideToBase(RectTransform rt, Vector2 basePos)
        {
            if (!rt) return;
            Vector2 curr = rt.anchoredPosition; // mulai dari base+offset
            DOTween.To(() => curr, v => { curr = v; rt.anchoredPosition = v; }, basePos, slideDuration)
                   .SetEase(slideEase);
        }

        IEnumerator TypeText(TMP_Text tmp)
        {
            tmp.ForceMeshUpdate();
            int total = tmp.textInfo.characterCount;
            if (total <= 0) { tmp.maxVisibleCharacters = 0; yield break; }

            int visible = 0, sfxCount = 0;
            float dt = 1f / charsPerSec;

            while (visible < total)
            {
                if (_skip)
                {
                    tmp.maxVisibleCharacters = total;
                    _skip = false;
                    break;
                }

                visible++;
                tmp.maxVisibleCharacters = visible;

                if (typeSfx && sfxSource && (++sfxCount % sfxEveryNChars == 0))
                    sfxSource.PlayOneShot(typeSfx);

                yield return new WaitForSeconds(dt);
            }
        }

        IEnumerator WaitOrSkip(float t)
        {
            if (t <= 0f) yield break;
            float timer = 0f;
            while (timer < t)
            {
                if (_skip) { _skip = false; yield break; }
                if (allowSkip && (Input.GetMouseButtonDown(0) || Input.GetKeyDown(skipKey))) _skip = true;
                timer += Time.deltaTime;
                yield return null;
            }
        }

        void Update()
        {
            if (!_running || !allowSkip) return;
            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(skipKey)) _skip = true;
        }
    }
}
