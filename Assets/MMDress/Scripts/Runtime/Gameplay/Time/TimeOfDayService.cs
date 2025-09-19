using UnityEngine;
using System;

namespace MMDress.Runtime.Timer
{
    public enum DayPhase { Night, Prep, Open, Closed }

    [DisallowMultipleComponent]
    public sealed class TimeOfDayService : MonoBehaviour
    {
        [Header("Durasi real per fase (detik)")]
        public float night00to06Seconds = 10f;   // fast-forward 00–06
        public float prepSeconds = 120f;  // 06–08 (2 menit)
        public float openSeconds = 240f;  // 08–16
        public float closed16to24Seconds = 10f;   // fast-forward 16–24

        public DayPhase CurrentPhase { get; private set; } = DayPhase.Night;
        public event Action<DayPhase> DayPhaseChanged;

        float _timer;
        int _idx; // 0 Night, 1 Prep, 2 Open, 3 Closed

        void OnEnable()
        {
            _idx = 0; _timer = 0f;
            CurrentPhase = (DayPhase)_idx;
            DayPhaseChanged?.Invoke(CurrentPhase); // seed awal
        }

        void Update()
        {
            float dur = GetDur(_idx);
            if (dur <= 0f) dur = 0.0001f;
            _timer += Time.deltaTime;
            if (_timer >= dur)
            {
                _timer -= dur;
                _idx = (_idx + 1) % 4;
                CurrentPhase = (DayPhase)_idx;
                DayPhaseChanged?.Invoke(CurrentPhase);
            }
        }

        float GetDur(int i) => i switch
        {
            0 => night00to06Seconds,
            1 => prepSeconds,
            2 => openSeconds,
            3 => closed16to24Seconds,
            _ => 1f
        };

        // 24h virtual clock
        public string GetClockText()
        {
            int minutes = Mathf.RoundToInt(GetVirtualMinutes()) % 1440;
            if (minutes < 0) minutes += 1440;
            int h = minutes / 60, m = minutes % 60;
            return $"{h:00}:{m:00}";
        }
        float GetVirtualMinutes()
        {
            float dur = GetDur(_idx);
            float t = dur <= 0f ? 0f : Mathf.Clamp01(_timer / dur);
            return CurrentPhase switch
            {
                DayPhase.Night => 0f + t * 360f, // 00:00–06:00
                DayPhase.Prep => 360f + t * 120f, // 06:00–08:00
                DayPhase.Open => 480f + t * 480f, // 08:00–16:00
                _ => 960f + t * 480f, // 16:00–24:00
            };
        }
    }
}
