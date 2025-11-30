// Assets/MMDress/Scripts/Runtime/Timer/EndOfDayTimeBridge.cs
using UnityEngine;
using MMDress.Core;
using MMDress.Runtime.Timer;
using MMDress.UI; // EndOfDayArrived

namespace MMDress.Runtime.Timer
{
    [DisallowMultipleComponent]
    [AddComponentMenu("MMDress/Timer/End Of Day Time Bridge")]
    public sealed class EndOfDayTimeBridge : MonoBehaviour
    {
        [SerializeField] private TimeOfDayService timeOfDay;
        [SerializeField] private bool autoFind = true;

        void Awake()
        {
            if (autoFind && !timeOfDay)
            {
#if UNITY_2023_1_OR_NEWER
                timeOfDay ??= Object.FindAnyObjectByType<TimeOfDayService>(FindObjectsInactive.Include);
#else
                timeOfDay ??= FindObjectOfType<TimeOfDayService>(true);
#endif
            }
        }

        void OnEnable()
        {
            if (timeOfDay != null)
                timeOfDay.DayPhaseChanged += OnPhaseChanged;
        }

        void OnDisable()
        {
            if (timeOfDay != null)
                timeOfDay.DayPhaseChanged -= OnPhaseChanged;
        }

        void OnPhaseChanged(DayPhase phase)
        {
            if (phase == DayPhase.Closed)
            {
                Debug.Log("[EOD Bridge] Phase Closed → pause day & show summary");
                if (timeOfDay != null)
                    timeOfDay.SetPaused(true); // stop jam di fase Closed

                ServiceLocator.Events?.Publish(new EndOfDayArrived());
            }
        }
    }
}
