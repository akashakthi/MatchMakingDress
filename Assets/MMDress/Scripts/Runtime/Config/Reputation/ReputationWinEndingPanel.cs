using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using MMDress.Runtime.Reputation;

namespace MMDress.Runtime.UI.Ending
{
    [DisallowMultipleComponent]
    [AddComponentMenu("MMDress/UI/Reputation Win Ending Panel")]
    public sealed class ReputationWinEndingPanel : MonoBehaviour
    {
        [Header("Win Condition")]
        [Range(0f, 100f)]
        [SerializeField] private float thresholdPercent = 100f;
        [SerializeField] private bool triggerOnce = true;

        [Header("UI Root & Pages")]
        [SerializeField] private GameObject panelRoot;      // parent panel (canvas group, dsb.)
        [SerializeField] private GameObject[] pages;        // tiap slide percakapan
        [SerializeField] private Button nextButton;         // tombol "Next"

        [Header("Config")]
        [SerializeField] private bool autoFindPagesFromChildren = true;
        [SerializeField] private string mainMenuSceneName = "MainMenu"; // ganti dengan nama scene main menu kamu

        [Header("Service (opsional auto find)")]
        [SerializeField] private ReputationService reputation;
        [SerializeField] private bool autoFindService = true;

        int _pageIndex = 0;
        bool _hasTriggered = false;

        void Reset()
        {
            panelRoot = gameObject;
            autoFindPagesFromChildren = true;
            autoFindService = true;
        }

        void Awake()
        {
            if (!panelRoot)
                panelRoot = gameObject;

            if (autoFindService && !reputation)
            {
#if UNITY_2023_1_OR_NEWER
                // pakai UnityEngine.Object biar tidak ambigu dengan System.Object
                reputation = UnityEngine.Object.FindAnyObjectByType<ReputationService>(FindObjectsInactive.Include);
#else
                reputation = FindObjectOfType<ReputationService>(true);
#endif
            }

            if (autoFindPagesFromChildren && (pages == null || pages.Length == 0))
            {
                // ambil semua child langsung sebagai page
                int childCount = panelRoot.transform.childCount;
                pages = new GameObject[childCount];
                for (int i = 0; i < childCount; i++)
                    pages[i] = panelRoot.transform.GetChild(i).gameObject;
            }

            if (panelRoot)
                panelRoot.SetActive(false); // hidden default
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
        }

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

        void ShowWinPanel()
        {
            if (panelRoot == null) return;

            // pause waktu (biar gameplay berhenti)
            Time.timeScale = 0f;

            panelRoot.SetActive(true);
            _pageIndex = 0;
            RefreshPages();
        }

        void RefreshPages()
        {
            if (pages == null || pages.Length == 0) return;

            for (int i = 0; i < pages.Length; i++)
            {
                if (pages[i] != null)
                    pages[i].SetActive(i == _pageIndex);
            }
        }

        void OnClickNext()
        {
            if (pages == null || pages.Length == 0)
            {
                FinishAndGoToMenu();
                return;
            }

            _pageIndex++;

            // kalau masih ada page berikutnya → tampilkan
            if (_pageIndex < pages.Length)
            {
                RefreshPages();
            }
            else
            {
                // sudah page terakhir
                FinishAndGoToMenu();
            }
        }

        void FinishAndGoToMenu()
        {
            // balikkan timeScale dulu
            Time.timeScale = 1f;

            if (panelRoot)
                panelRoot.SetActive(false);

            if (!string.IsNullOrEmpty(mainMenuSceneName))
            {
                SceneManager.LoadScene(mainMenuSceneName);
            }
            else
            {
                Debug.LogWarning("[ReputationWinEndingPanel] mainMenuSceneName belum diisi.");
            }
        }

        // Optional helper buat test dari Inspector
        [ContextMenu("Debug/Force Show Win Panel")]
        void DebugShowWin()
        {
            _hasTriggered = true;
            ShowWinPanel();
        }
    }
}
