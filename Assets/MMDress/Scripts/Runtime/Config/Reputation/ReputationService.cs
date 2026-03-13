using System;
using UnityEngine;

namespace MMDress.Runtime.Reputation
{
    [DisallowMultipleComponent]
    public sealed class ReputationService : MonoBehaviour
    {
        [Header("State (0..100)")]
        [Range(0f, 100f)]
        [SerializeField] private float repPercent = 10f;

        [SerializeField] private float defaultStartingPercent = 10f;
        [SerializeField] private float loseThresholdPercent = 0f;

        public float RepPercent => repPercent;
        public bool IsDepleted => repPercent <= loseThresholdPercent;

        [Header("Stage thresholds (%)")]
        [SerializeField] private float stage1Max = 30f;
        [SerializeField] private float stage2Max = 60f;

        [Header("Speed factors")]
        [SerializeField] private float stage1Speed = 1.0f;
        [SerializeField] private float stage2Speed = 1.2f;
        [SerializeField] private float stage3Speed = 2.0f;

        [Header("Persistence")]
        [SerializeField] private bool usePlayerPrefs = true;
        [SerializeField] private bool saveDefaultOnFirstLoad = true;
        private const string PrefKey = "MMDress.RepPercent";

        public int Stage { get; private set; } = 1;
        public float CurrentSpeedFactor =>
            Stage == 1 ? stage1Speed : (Stage == 2 ? stage2Speed : stage3Speed);

        public event Action<float> ReputationChanged;
        public event Action<int, int, int> ReputationStageChanged;
        public event Action ReputationDepleted;

        private bool _depletedTriggered;

        private void Awake()
        {
            LoadOrInit();
        }

        private void OnEnable()
        {
            RecalcStageAndNotify(initial: true);
            TryNotifyDepleted();
        }

        public void AddPercent(float delta)
        {
            SetPercent(repPercent + delta);
        }

        public void SetPercent(float value)
        {
            float old = repPercent;
            repPercent = Mathf.Clamp(value, 0f, 100f);

            if (Mathf.Approximately(old, repPercent))
            {
                RecalcStageAndNotify(initial: false);
                TryNotifyDepleted();
                return;
            }

            Save();
            RecalcStageAndNotify(initial: false);
            TryNotifyDepleted();
        }

        public void ResetToDefault()
        {
            _depletedTriggered = false;
            SetPercent(defaultStartingPercent);
        }

        // benar = +2, salah/kurang/timeout = -1
        public void ApplyCheckout(bool served, bool failed)
        {
            if (failed) AddPercent(-1f);
            else if (served) AddPercent(+2f);
        }

        private void LoadOrInit()
        {
            _depletedTriggered = false;

            if (!usePlayerPrefs)
            {
                repPercent = Mathf.Clamp(defaultStartingPercent, 0f, 100f);
                return;
            }

            if (PlayerPrefs.HasKey(PrefKey))
            {
                repPercent = Mathf.Clamp(
                    PlayerPrefs.GetFloat(PrefKey, defaultStartingPercent),
                    0f,
                    100f);
                return;
            }

            repPercent = Mathf.Clamp(defaultStartingPercent, 0f, 100f);

            if (saveDefaultOnFirstLoad)
                Save();
        }

        private void Save()
        {
            if (!usePlayerPrefs) return;

            PlayerPrefs.SetFloat(PrefKey, repPercent);
            PlayerPrefs.Save();
        }

        private void RecalcStageAndNotify(bool initial)
        {
            int prevStage = Stage;
            Stage = (repPercent <= stage1Max) ? 1 : (repPercent <= stage2Max ? 2 : 3);

            ReputationChanged?.Invoke(repPercent);

            if (!initial && Stage != prevStage)
            {
                int dir = Stage > prevStage ? +1 : -1;
                ReputationStageChanged?.Invoke(prevStage, Stage, dir);
            }
        }

        private void TryNotifyDepleted()
        {
            if (_depletedTriggered || !IsDepleted)
                return;

            _depletedTriggered = true;
            ReputationDepleted?.Invoke();
        }
    }
}