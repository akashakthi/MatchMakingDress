// Assets/MMDress/Scripts/Runtime/UI/PhaseVisibilityGate.cs
using UnityEngine;
using MMDress.Runtime.Timer;   // <- pastikan pakai namespace "Time"

namespace MMDress.Runtime.UI
{
    [DisallowMultipleComponent]
    public sealed class PhaseVisibilityGate : MonoBehaviour
    {
        [SerializeField] private TimeOfDayService timeOfDay;
        [SerializeField] private DayPhase visibleIn = DayPhase.Prep; // tampil hanya saat Prep
        [SerializeField] private GameObject target;                   // kosong = gameObject sendiri

        void Awake()
        {
            if (!timeOfDay) timeOfDay = FindObjectOfType<TimeOfDayService>(true);
            if (!target) target = gameObject;
        }

        void OnEnable()
        {
            if (timeOfDay)
            {
                timeOfDay.DayPhaseChanged += OnPhase;
                OnPhase(timeOfDay.CurrentPhase); // seed awal
            }
        }

        void OnDisable()
        {
            if (timeOfDay) timeOfDay.DayPhaseChanged -= OnPhase;
        }

        void OnPhase(DayPhase phase)
        {
            bool show = (phase == visibleIn);
            if (target && target.activeSelf != show) target.SetActive(show);
        }
    }
}
