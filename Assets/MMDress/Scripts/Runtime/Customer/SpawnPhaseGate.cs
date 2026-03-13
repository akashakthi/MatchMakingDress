using UnityEngine;
using MMDress.Runtime.Timer;

namespace MMDress.Customer
{
    [RequireComponent(typeof(CustomerSpawner))]
    public sealed class SpawnPhaseGate : MonoBehaviour
    {
        [SerializeField] private TimeOfDayService timeOfDay;
        [SerializeField] private DayPhase activePhase = DayPhase.Open;

        [Header("Behavior")]
        [Tooltip("Saat keluar dari fase Open, customer aktif langsung dibersihkan agar tidak timeout di Prep / Closed.")]
        [SerializeField] private bool clearActiveCustomersWhenLeavingOpen = true;

        [Tooltip("Reset spawn accumulator agar tidak langsung numpuk spawn saat masuk Open.")]
        [SerializeField] private bool resetSpawnAccumulatorOnPhaseChange = true;

        [Header("Debug")]
        [SerializeField] private bool verbose = true;

        private CustomerSpawner spawner;
        private DayPhase _lastPhase;

        private void Awake()
        {
            spawner = GetComponent<CustomerSpawner>();
            if (!timeOfDay) timeOfDay = FindObjectOfType<TimeOfDayService>(true);
        }

        private void OnEnable()
        {
            if (timeOfDay) timeOfDay.DayPhaseChanged += OnPhase;

            _lastPhase = timeOfDay ? timeOfDay.CurrentPhase : DayPhase.Night;
            ApplyPhase(_lastPhase, true);
        }

        private void OnDisable()
        {
            if (timeOfDay) timeOfDay.DayPhaseChanged -= OnPhase;
        }

        private void OnPhase(DayPhase phase)
        {
            ApplyPhase(phase, false);
            _lastPhase = phase;
        }

        private void ApplyPhase(DayPhase phase, bool initial)
        {
            if (!spawner) return;

            bool allow = (phase == activePhase);
            spawner.SetSpawnAllowed(allow, resetSpawnAccumulatorOnPhaseChange);

            bool leavingOpen = !initial && _lastPhase == activePhase && phase != activePhase;

            if (leavingOpen && clearActiveCustomersWhenLeavingOpen)
            {
                if (verbose)
                    Debug.Log($"[SpawnPhaseGate] Leaving {activePhase} -> {phase}, clearing active customers.", this);

                spawner.ClearAllActiveCustomers(resetSpawnAccumulatorOnPhaseChange);
            }

            if (verbose)
            {
                Debug.Log(
                    $"[SpawnPhaseGate] ApplyPhase | current={phase} | allowSpawn={allow} | initial={initial}",
                    this);
            }
        }
    }
}