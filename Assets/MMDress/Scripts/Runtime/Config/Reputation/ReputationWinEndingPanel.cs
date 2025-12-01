// Assets/MMDress/Scripts/Runtime/UI/Ending/ReputationWinEndingPanel.cs
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using DG.Tweening;
using MMDress.Runtime.Reputation;

namespace MMDress.Runtime.UI.Ending
{
    [DisallowMultipleComponent]
    [AddComponentMenu("MMDress/UI/Reputation Win Ending Panel (Summary Style)")]
    public sealed class ReputationWinEndingPanel : MonoBehaviour
    {
        [Header("Win Condition")]
        [Range(0f, 100f)]
        [SerializeField] private float thresholdPercent = 100f;
        [SerializeField] private bool triggerOnce = true;

        [Header("Root")]
        [SerializeField] private GameObject panelRoot;          // parent panel (Canvas)
        [SerializeField] private Button nextButton;             // tombol Next / OK

        [Header("Visual Groups")]
        [SerializeField] private CanvasGroup raysGroup;         // background rays (bisa kosong)
        [SerializeField] private CanvasGroup bgGroup;           // BG utama (win background)
        [SerializeField] private CanvasGroup titleGroup;        // Title / teks "Selamat" (optional)
        [SerializeField] private CanvasGroup repIconGroup;      // ikon reputasi (optional)

        [Header("Rects (untuk pop / rotasi)")]
        [SerializeField] private RectTransform raysRect;        // di-rotate pelan
        [SerializeField] private RectTransform bgRect;          // pop
        [SerializeField] private RectTransform titleRect;       // pop
        [SerializeField] private RectTransform repIconRect;     // pop

        [Header("Text")]
        [SerializeField] private TMP_Text reputationText;       // tampil "+100%" atau "100%"

        [Header("Timing (detik)")]
        [SerializeField] private float raysFadeDuration = 0.25f;
        [SerializeField] private float bgFadeDuration = 0.30f;
        [SerializeField] private float titleFadeDuration = 0.30f;
        [SerializeField] private float iconFadeDuration = 0.30f;
        [SerializeField] private float countDuration = 1.5f;

        [Header("Rays Rotation")]
        [SerializeField] private float raysRotateSpeed = 40f;   // derajat per detik

        [Header("Scene Config")]
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        [Header("Service (auto find)")]
        [SerializeField] private ReputationService reputation;
        [SerializeField] private bool autoFindService = true;

        // runtime
        bool _hasTriggered;
        int _targetReputation;
        Sequence _seq;
        Tween _countTween;
        Tween _raysRotateTween;

        void Reset()
        {
            panelRoot = gameObject;
            autoFindService = true;
        }

        void Awake()
        {
            if (!panelRoot)
                panelRoot = gameObject;

            if (autoFindService && !reputation)
            {
#if UNITY_2023_1_OR_NEWER
                reputation = UnityEngine.Object.FindAnyObjectByType<ReputationService>(FindObjectsInactive.Include);
#else
                reputation = FindObjectOfType<ReputationService>(true);
#endif
            }

            if (panelRoot)
                panelRoot.SetActive(false);    // default hidden
        }

        void OnEnable()
        {
            if (reputation != null)
                reputation.ReputationChanged += OnReputationChanged;

            if (nextButton != null)
            {
                nextButton.onClick.RemoveAllListeners();
                nextButton.onClick.AddListener(OnClickNext);
            }
        }

        void OnDisable()
        {
            if (reputation != null)
                reputation.ReputationChanged -= OnReputationChanged;

            if (nextButton != null)
                nextButton.onClick.RemoveListener(OnClickNext);

            KillTweens();
        }

        // --------------------------------------------------------------------
        // Trigger dari ReputationService
        // --------------------------------------------------------------------
        void OnReputationChanged(float percent)
        {
            if (_hasTriggered && triggerOnce)
                return;

            if (percent >= thresholdPercent)
            {
                _hasTriggered = true;
                ShowWinPanel();
            }
        }

        // --------------------------------------------------------------------
        // Visual setup + sequence
        // --------------------------------------------------------------------
        void ShowWinPanel()
        {
            if (!panelRoot)
                return;

            // Pause gameplay, animasi pakai SetUpdate(true) biar tetap jalan
            Time.timeScale = 0f;

            // baca target reputasi dari service (fallback = threshold)
            _targetReputation = reputation
                ? Mathf.RoundToInt(reputation.RepPercent)
                : Mathf.RoundToInt(thresholdPercent);

            PrepareInitialVisual();
            PlaySequence();
        }

        void PrepareInitialVisual()
        {
            panelRoot.SetActive(true);

            if (raysGroup) raysGroup.alpha = 0f;
            if (bgGroup) bgGroup.alpha = 0f;
            if (titleGroup) titleGroup.alpha = 0f;
            if (repIconGroup) repIconGroup.alpha = 0f;

            if (bgRect) bgRect.localScale = Vector3.one * 0.6f;
            if (titleRect) titleRect.localScale = Vector3.one * 0.6f;
            if (repIconRect) repIconRect.localScale = Vector3.one * 0.6f;

            if (reputationText)
                reputationText.text = "0%";
        }

        void PlaySequence()
        {
            KillTweens();

            _seq = DOTween.Sequence().SetUpdate(true);

            // 1) rays fade + mulai rotasi
            if (raysGroup)
            {
                _seq.Append(FadeCanvas(raysGroup, raysFadeDuration));
                StartRaysRotate();
            }

            // 2) BG fade + pop
            if (bgGroup && bgRect)
            {
                _seq.Append(FadeCanvas(bgGroup, bgFadeDuration));
                _seq.Join(bgRect.DOScale(1f, bgFadeDuration).SetEase(Ease.OutBack));
            }

            // 3) Title fade + pop
            if (titleGroup && titleRect)
            {
                _seq.Append(FadeCanvas(titleGroup, titleFadeDuration));
                _seq.Join(titleRect.DOScale(1f, titleFadeDuration).SetEase(Ease.OutBack));
            }

            // 4) Icon reputasi fade + pop
            if (repIconGroup && repIconRect)
            {
                _seq.Append(FadeCanvas(repIconGroup, iconFadeDuration));
                _seq.Join(repIconRect.DOScale(1f, iconFadeDuration).SetEase(Ease.OutBack));
            }

            // 5) angka reputasi jalan
            _seq.AppendCallback(PlayCountAnimation);
        }

        Tween FadeCanvas(CanvasGroup g, float duration)
        {
            if (!g || duration <= 0f) return null;
            g.alpha = 0f;
            return DOTween
                .To(() => g.alpha, a => g.alpha = a, 1f, duration)
                .SetUpdate(true);
        }

        void PlayCountAnimation()
        {
            if (!reputationText) return;

            _countTween = DOTween.To(
                    () => 0,
                    v => reputationText.text = v.ToString("0") + "%",  // misal "100%"
                    _targetReputation,
                    countDuration
                )
                .SetUpdate(true);
        }

        void StartRaysRotate()
        {
            if (!raysRect || raysRotateSpeed == 0f) return;

            _raysRotateTween?.Kill();
            _raysRotateTween = raysRect
                .DORotate(new Vector3(0f, 0f, 360f), 360f / Mathf.Abs(raysRotateSpeed), RotateMode.FastBeyond360)
                .SetLoops(-1, LoopType.Restart)
                .SetEase(Ease.Linear)
                .SetUpdate(true);
        }

        void KillTweens()
        {
            _seq?.Kill(); _seq = null;
            _countTween?.Kill(); _countTween = null;
            _raysRotateTween?.Kill(); _raysRotateTween = null;
        }

        // --------------------------------------------------------------------
        // Button Next → balik ke main menu
        // --------------------------------------------------------------------
        void OnClickNext()
        {
            KillTweens();

            if (panelRoot)
                panelRoot.SetActive(false);

            Time.timeScale = 1f;

            if (!string.IsNullOrEmpty(mainMenuSceneName))
            {
                SceneManager.LoadScene(mainMenuSceneName);
            }
            else
            {
                Debug.LogWarning("[ReputationWinEndingPanel] mainMenuSceneName belum diisi.");
            }
        }

        // Debug helper di Inspector
        [ContextMenu("Debug/Force Show Win Panel")]
        void DebugShowWin()
        {
            _hasTriggered = true;
            ShowWinPanel();
        }
    }
}
