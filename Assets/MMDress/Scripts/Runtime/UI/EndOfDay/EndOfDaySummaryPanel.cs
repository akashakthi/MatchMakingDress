using UnityEngine;
using TMPro;
using DG.Tweening;
using MMDress.Core;
using MMDress.UI;
using MMDress.Runtime.Timer;
using MMDress.Services;                // EconomyService
using RepService = MMDress.Runtime.Reputation.ReputationService;

namespace MMDress.Runtime.UI.EndOfDay
{
    [DisallowMultipleComponent]
    public sealed class EndOfDaySummaryPanel : MonoBehaviour
    {
        [Header("Root Panel (parent visual)")]
        [SerializeField] private GameObject panelRoot;   // SummaryDays

        [Header("Visual Groups (CanvasGroup)")]
        [SerializeField] private CanvasGroup raysGroup;     // Background rays
        [SerializeField] private CanvasGroup summaryGroup;  // BgSummary
        [SerializeField] private CanvasGroup goodJobGroup;  // BGGoodJob

        [Header("Rects for Pop Effect")]
        [SerializeField] private RectTransform summaryRect; // BgSummary RectTransform
        [SerializeField] private RectTransform goodJobRect; // BGGoodJob RectTransform

        [Header("Texts")]
        [SerializeField] private TMP_Text moneyText;        // child 'money'
        [SerializeField] private TMP_Text reputationText;   // child 'Reputation'

        [Header("Timing (sec)")]
        [SerializeField] private float raysDuration = 0.25f;
        [SerializeField] private float summaryDuration = 0.30f;
        [SerializeField] private float goodJobDuration = 0.30f;
        [SerializeField] private float countDuration = 2f;

        [Header("Target Values (auto diisi)")]
        [SerializeField] private int targetMoney;
        [SerializeField] private int targetReputation;

        [Header("Services (opsional auto-find)")]
        [SerializeField] private TimeOfDayService timeOfDay;
        [SerializeField] private EconomyService economy;
        [SerializeField] private RepService reputation;
        [SerializeField] private bool autoFindServices = true;

        System.Action<EndOfDayArrived> _onEod;
        Sequence _seq;
        Tween _moneyTween, _repTween;

        void Awake()
        {
            if (!panelRoot)
                panelRoot = gameObject;

            if (panelRoot)
                panelRoot.SetActive(false);

            if (autoFindServices)
            {
#if UNITY_2023_1_OR_NEWER
                timeOfDay ??= Object.FindAnyObjectByType<TimeOfDayService>(FindObjectsInactive.Include);
                economy ??= Object.FindAnyObjectByType<EconomyService>(FindObjectsInactive.Include);
                reputation ??= Object.FindAnyObjectByType<RepService>(FindObjectsInactive.Include);
#else
                timeOfDay  ??= FindObjectOfType<TimeOfDayService>(true);
                economy    ??= FindObjectOfType<EconomyService>(true);
                reputation ??= FindObjectOfType<RepService>(true);
#endif
            }
        }

        void OnEnable()
        {
            _onEod = _ => PlaySequence();
            ServiceLocator.Events.Subscribe(_onEod);
        }

        void OnDisable()
        {
            if (_onEod != null)
                ServiceLocator.Events.Unsubscribe(_onEod);

            KillTweens();
        }

        void KillTweens()
        {
            _seq?.Kill(); _seq = null;
            _moneyTween?.Kill(); _moneyTween = null;
            _repTween?.Kill(); _repTween = null;
        }

        void AutoFillFromServices()
        {
            targetMoney = economy ? economy.Balance : 0;
            targetReputation = reputation ? Mathf.RoundToInt(reputation.RepPercent) : 0;
        }

        void PrepareInitialVisual()
        {
            if (panelRoot)
                panelRoot.SetActive(true);

            if (raysGroup) raysGroup.alpha = 0f;
            if (summaryGroup) summaryGroup.alpha = 0f;
            if (goodJobGroup) goodJobGroup.alpha = 0f;

            if (summaryRect) summaryRect.localScale = Vector3.one * 0.6f;
            if (goodJobRect) goodJobRect.localScale = Vector3.one * 0.6f;

            if (moneyText) moneyText.text = "+0";
            if (reputationText) reputationText.text = "+0%";
        }

        // helper fade manual (karena CanvasGroup.DOFade kadang ga ke-resolve)
        Tween FadeCanvas(CanvasGroup g, float duration)
        {
            if (!g || duration <= 0f) return null;
            g.alpha = 0f;
            return DOTween.To(() => g.alpha, a => g.alpha = a, 1f, duration);
        }

        void PlaySequence()
        {
            KillTweens();
            AutoFillFromServices();
            PrepareInitialVisual();

            _seq = DOTween.Sequence().SetUpdate(true);

            // 1) Rays fade
            if (raysGroup)
                _seq.Append(FadeCanvas(raysGroup, raysDuration));

            // 2) Summary panel fade + pop
            if (summaryGroup && summaryRect)
            {
                _seq.Append(FadeCanvas(summaryGroup, summaryDuration));
                _seq.Join(
                    summaryRect
                        .DOScale(1f, summaryDuration)
                        .SetEase(Ease.OutBack)
                );
            }

            // 3) GOOD JOB fade + pop
            if (goodJobGroup && goodJobRect)
            {
                _seq.Append(FadeCanvas(goodJobGroup, goodJobDuration));
                _seq.Join(
                    goodJobRect
                        .DOScale(1f, goodJobDuration)
                        .SetEase(Ease.OutBack)
                );
            }

            // 4) angka jalan
            _seq.AppendCallback(PlayCountAnimation);
        }

        void PlayCountAnimation()
        {
            // money
            if (moneyText)
            {
                _moneyTween = DOTween.To(
                        () => 0,
                        v => moneyText.text = "+" + v.ToString("N0"),
                        targetMoney,
                        countDuration
                    )
                    .SetUpdate(true);
            }

            // reputasi
            if (reputationText)
            {
                _repTween = DOTween.To(
                        () => 0,
                        v => reputationText.text = "+" + v + "%",
                        targetReputation,
                        countDuration
                    )
                    .SetUpdate(true);
            }
        }

        // tombol Next / Skip Day
        public void OnClickNextDay()
        {
            KillTweens();

            if (panelRoot)
                panelRoot.SetActive(false);

            if (timeOfDay != null)
            {
                timeOfDay.SetPaused(false);
                timeOfDay.JumpToPhase(DayPhase.Prep);
            }
        }
    }
}
