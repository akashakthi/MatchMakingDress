using System;
using UnityEngine;

namespace MMDress.Runtime.Timer
{
    public enum DayPhase { Night, Prep, Open, Closed }

    public sealed class TimeOfDayService : MonoBehaviour
    {
        [Header("Schedule (Game Clock)")]
        [Tooltip("Prep (06:00–08:00) dipadatkan menjadi 1 menit realtime.")]
        [SerializeField] private float prepRealSeconds = 60f;
        [Tooltip("Durasi fase Open (08:00–16:00) dalam detik realtime (sementara konstan).")]
        [SerializeField] private float openRealSeconds = 240f;

        [Header("State (read-only)")]
        [SerializeField] private DayPhase currentPhase = DayPhase.Night;
        public DayPhase CurrentPhase => currentPhase;

        public event Action<DayPhase> DayPhaseChanged;
        public event Action ShopOpened;
        public event Action ShopClosed;
        public event Action DayLooped;

        // internal timers
        private float _phaseTimer;
        private bool _loopStarted;

        private void Awake()
        {
            // Mulai dari Night → tuasikan ke Prep on Start
            currentPhase = DayPhase.Night;
            _phaseTimer = 0f;
        }

        private void Start()
        {
            // Fast-forward malam → masuk Prep
            EnterPhase(DayPhase.Prep);
        }

        private void Update()
        {
            _phaseTimer += Time.deltaTime;

            switch (currentPhase)
            {
                case DayPhase.Prep:
                    if (_phaseTimer >= prepRealSeconds)
                        EnterPhase(DayPhase.Open);
                    break;

                case DayPhase.Open:
                    if (_phaseTimer >= openRealSeconds)
                        EnterPhase(DayPhase.Closed);
                    break;

                case DayPhase.Closed:
                    // Fast-forward malam singkat → balik ke Prep
                    // Bisa bikin delay kecil kalau mau animasi transisi
                    EnterPhase(DayPhase.Night);
                    break;

                case DayPhase.Night:
                    // langsung loop (skip malam)
                    EnterPhase(DayPhase.Prep);
                    DayLooped?.Invoke();
                    break;
            }
        }

        private void EnterPhase(DayPhase next)
        {
            currentPhase = next;
            _phaseTimer = 0f;
            DayPhaseChanged?.Invoke(currentPhase);

            if (next == DayPhase.Open) ShopOpened?.Invoke();
            if (next == DayPhase.Closed) ShopClosed?.Invoke();
        }

        // Format sederhana untuk HUD (mock jam)
        public string GetClockText()
        {
            switch (currentPhase)
            {
                case DayPhase.Prep:
                    // map 0..prepRealSeconds ke 06:00..08:00
                    float tP = Mathf.Clamp01(_phaseTimer / Mathf.Max(0.0001f, prepRealSeconds));
                    int minutesP = Mathf.RoundToInt(Mathf.Lerp(6 * 60, 8 * 60, tP));
                    return ToHHMM(minutesP);
                case DayPhase.Open:
                    float tO = Mathf.Clamp01(_phaseTimer / Mathf.Max(0.0001f, openRealSeconds));
                    int minutesO = Mathf.RoundToInt(Mathf.Lerp(8 * 60, 16 * 60, tO));
                    return ToHHMM(minutesO);
                case DayPhase.Closed:
                    return "16:00";
                case DayPhase.Night:
                    return "—";
                default:
                    return "";
            }
        }

        private static string ToHHMM(int totalMinutes)
        {
            int hh = totalMinutes / 60;
            int mm = totalMinutes % 60;
            return $"{hh:00}:{mm:00}";
        }
    }
}
