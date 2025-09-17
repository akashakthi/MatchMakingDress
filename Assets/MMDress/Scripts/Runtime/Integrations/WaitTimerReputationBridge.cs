using UnityEngine;
using MMDress.Customer;
using MMDress.Runtime.Reputation;

namespace MMDress.Runtime.Integration
{
    /// <summary>
    /// Bridge ReputationService → WaitTimer (class biasa). 
    /// Panggil Bind(timer) saat timer dibuat/reset.
    /// </summary>
    public sealed class WaitTimerReputationBridge : MonoBehaviour
    {
        [SerializeField] private ReputationService reputation;
        private WaitTimer _timer;

        public void Bind(WaitTimer timer)
        {
            _timer = timer;
            if (reputation != null && _timer != null)
                _timer.SetExternalSpeedFactor(reputation.CurrentSpeedFactor);
        }

        private void OnEnable()
        {
            if (reputation != null)
                reputation.ReputationStageChanged += OnStageChanged;
        }

        private void OnDisable()
        {
            if (reputation != null)
                reputation.ReputationStageChanged -= OnStageChanged;
        }

        private void OnStageChanged(int prev, int next, int dir)
        {
            if (_timer != null && reputation != null)
                _timer.SetExternalSpeedFactor(reputation.CurrentSpeedFactor);
        }
    }
}
