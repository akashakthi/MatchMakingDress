using UnityEngine;
using UnityEngine.UI;
using MMDress.Runtime.Reputation;
using MMDress.Runtime.Timer;

namespace MMDress.Runtime.UI.HUD
{
    public sealed class HUDClockReputationView : MonoBehaviour
    {
        [SerializeField] private TimeOfDayService timeOfDay;
        [SerializeField] private ReputationService reputation;

        [Header("UI Refs (UnityEngine.UI.Text)")]
        [SerializeField] private Text clockText;
        [SerializeField] private Text repPercentText;
        [SerializeField] private Text stageText;

        private void Awake()
        {
            // Fallback auto-wire supaya tidak null saat lupa drag
            if (!reputation) reputation = FindObjectOfType<ReputationService>(includeInactive: true);
            if (!timeOfDay) timeOfDay = FindObjectOfType<TimeOfDayService>(includeInactive: true);
        }

        private void OnEnable()
        {
            if (timeOfDay != null) timeOfDay.DayPhaseChanged += OnDayPhaseChanged;
            else Debug.LogWarning("[HUD] TimeOfDayService belum terpasang di Inspector dan tidak ditemukan di scene.");

            if (reputation != null)
            {
                reputation.ReputationChanged += OnReputationChanged;
                reputation.ReputationStageChanged += OnReputationStageChanged;
            }
            else
            {
                Debug.LogWarning("[HUD] ReputationService belum terpasang di Inspector dan tidak ditemukan di scene.");
            }

            RefreshClock();
            RefreshReputation();
        }

        private void OnDisable()
        {
            if (timeOfDay != null) timeOfDay.DayPhaseChanged -= OnDayPhaseChanged;
            if (reputation != null)
            {
                reputation.ReputationChanged -= OnReputationChanged;
                reputation.ReputationStageChanged -= OnReputationStageChanged;
            }
        }

        private void Update() => RefreshClock(); // jam smooth

        private void OnDayPhaseChanged(DayPhase _) => RefreshClock();
        private void OnReputationChanged(float _) => RefreshReputation();
        private void OnReputationStageChanged(int prev, int next, int dir) => RefreshReputation();

        private void RefreshClock()
        {
            if (clockText && timeOfDay != null)
                clockText.text = timeOfDay.GetClockText();
        }

        private void RefreshReputation()
        {
            if (!reputation) return;

            if (repPercentText) repPercentText.text = $"{reputation.RepPercent:0}%";
            if (stageText) stageText.text = $"Stage {reputation.Stage}";
        }
    }
}
