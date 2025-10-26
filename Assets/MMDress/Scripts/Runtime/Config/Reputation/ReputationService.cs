// Assets/MMDress/Scripts/Runtime/Reputation/ReputationService.cs
using System;
using UnityEngine;

namespace MMDress.Runtime.Reputation
{
    [DisallowMultipleComponent]
    public sealed class ReputationService : MonoBehaviour
    {
        [Header("State (0..100)")]
        [Range(0f, 100f)][SerializeField] private float repPercent = 0f;
        public float RepPercent => repPercent;

        [Header("Stage thresholds (%)")]
        [SerializeField] private float stage1Max = 30f; // ≤30 => Stage 1
        [SerializeField] private float stage2Max = 60f; // 31–60 => Stage 2, >60 => Stage 3

        [Header("Speed factors")]
        [SerializeField] private float stage1Speed = 1.0f;
        [SerializeField] private float stage2Speed = 1.2f;
        [SerializeField] private float stage3Speed = 2.0f;

        [Header("Persistence")]
        [SerializeField] private bool usePlayerPrefs = true;   // default: true biar ke-save
        private const string PrefKey = "MMDress.RepPercent";

        public int Stage { get; private set; } = 1;
        public float CurrentSpeedFactor =>
            Stage == 1 ? stage1Speed : (Stage == 2 ? stage2Speed : stage3Speed);

        public event Action<float> ReputationChanged;                   // percent terbaru
        public event Action<int, int, int> ReputationStageChanged;      // prev, next, dir(+1/-1)

        private void OnEnable()
        {
            if (usePlayerPrefs && PlayerPrefs.HasKey(PrefKey))
                repPercent = Mathf.Clamp(PlayerPrefs.GetFloat(PrefKey, 0f), 0f, 100f);

            RecalcStageAndNotify(initial: true);
        }

        public void AddPercent(float delta)
        {
            repPercent = Mathf.Clamp(repPercent + delta, 0f, 100f);
            if (usePlayerPrefs)
            {
                PlayerPrefs.SetFloat(PrefKey, repPercent);
                PlayerPrefs.Save();
            }
            RecalcStageAndNotify(initial: false);
        }

        /// Dipanggil saat customer selesai. served => +1%; empty/timeout => -1%.
        public void ApplyCheckout(bool served, bool empty)
        {
            if (empty) AddPercent(-1f);
            else if (served) AddPercent(+1f);
        }

        private void RecalcStageAndNotify(bool initial)
        {
            int prevStage = Stage;
            Stage = (repPercent <= stage1Max) ? 1 : (repPercent <= stage2Max ? 2 : 3);

            // selalu notify agar HUD refresh
            ReputationChanged?.Invoke(repPercent);

            if (!initial && Stage != prevStage)
            {
                int dir = Stage > prevStage ? +1 : -1;
                ReputationStageChanged?.Invoke(prevStage, Stage, dir);
            }
        }
    }
}
