using UnityEngine;
using MMDress.Core;
using CheckoutEvt = MMDress.Gameplay.CustomerCheckout;
using TimedOutEvt = MMDress.Gameplay.CustomerTimedOut;

namespace MMDress.Runtime.Reputation
{
    [DisallowMultipleComponent]
    public sealed class ReputationOnCheckout : MonoBehaviour
    {
        [SerializeField] private ReputationService reputation;
        [SerializeField] private bool autoFindReputation = true;

        private void Awake()
        {
            if (autoFindReputation && !reputation)
                reputation = FindObjectOfType<ReputationService>(true);
        }

        private void OnEnable()
        {
            ServiceLocator.Events?.Subscribe<CheckoutEvt>(OnCheckout);
            ServiceLocator.Events?.Subscribe<TimedOutEvt>(OnTimedOut);
        }

        private void OnDisable()
        {
            ServiceLocator.Events?.Unsubscribe<CheckoutEvt>(OnCheckout);
            ServiceLocator.Events?.Unsubscribe<TimedOutEvt>(OnTimedOut);
        }

        private void OnCheckout(CheckoutEvt e)
        {
            if (!reputation)
                return;

            bool served = e.itemsEquipped >= 2 && e.isCorrectOrder;
            bool failed = e.itemsEquipped < 2 || !e.isCorrectOrder;

            reputation.ApplyCheckout(served, failed);
        }

        private void OnTimedOut(TimedOutEvt e)
        {
            if (!reputation)
                return;

            reputation.ApplyCheckout(served: false, failed: true);
        }
    }
}