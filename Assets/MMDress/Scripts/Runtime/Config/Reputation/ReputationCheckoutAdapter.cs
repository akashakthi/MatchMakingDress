// Assets/MMDress/Scripts/Runtime/Reputation/ReputationOnCheckout.cs
using UnityEngine;
using MMDress.Core;
using MMDress.Runtime.Reputation;
using CheckoutEvt = MMDress.Gameplay.CustomerCheckout;

namespace MMDress.Runtime.Reputation
{
    [DisallowMultipleComponent]
    public sealed class ReputationOnCheckout : MonoBehaviour
    {
        [SerializeField] private ReputationService reputation;
        [SerializeField] private float minEventGap = 0.25f;

        private float _lastTime;

        private void OnEnable()
        {
            ServiceLocator.Events.Subscribe<CheckoutEvt>(OnCheckout);
        }

        private void OnDisable()
        {
            ServiceLocator.Events.Unsubscribe<CheckoutEvt>(OnCheckout);
        }

        private void OnCheckout(CheckoutEvt e)
        {
            // anti double-event
            if (Time.unscaledTime - _lastTime < minEventGap)
                return;

            _lastTime = Time.unscaledTime;

            if (!reputation) return;

            // Definisi:
            // - served = outfit lengkap + order benar
            // - empty  = kurang dari 2 item ATAU order salah
            bool served = (e.itemsEquipped >= 2) && e.isCorrectOrder;
            bool empty = (e.itemsEquipped < 2) || !e.isCorrectOrder;

            reputation.ApplyCheckout(served, empty);
        }
    }
}
