using UnityEngine;
using MMDress.Runtime.Timer;
using MMDress.Customer;   // DayPhase, TimeOfDayService

namespace MMDress.Gameplay
{
    [RequireComponent(typeof(CustomerSpawner))]
    public sealed class SpawnPhaseGate : MonoBehaviour
    {
        [SerializeField] private TimeOfDayService timeOfDay;
        [SerializeField] private DayPhase activePhase = DayPhase.Open;

        private CustomerSpawner spawner;

        private void Awake()
        {
            spawner = GetComponent<CustomerSpawner>();
            if (!timeOfDay) timeOfDay = FindObjectOfType<TimeOfDayService>(true);
        }

        private void OnEnable()
        {
            if (timeOfDay) timeOfDay.DayPhaseChanged += OnPhase;
            // Terapkan status awal (hindari spawn sebelum 08:00)
            ApplyPhase(timeOfDay ? timeOfDay.CurrentPhase : DayPhase.Night);
        }

        private void OnDisable()
        {
            if (timeOfDay) timeOfDay.DayPhaseChanged -= OnPhase;
        }

        private void OnPhase(DayPhase phase) => ApplyPhase(phase);

        private void ApplyPhase(DayPhase phase)
        {
            bool allow = (phase == activePhase);
            if (spawner) spawner.enabled = allow;
        }
    }
}
