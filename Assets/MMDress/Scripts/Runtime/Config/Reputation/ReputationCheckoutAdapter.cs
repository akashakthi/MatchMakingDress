using UnityEngine;
using System.Collections.Generic;
using MMDress.Core;                 // ServiceLocator.Events
using MMDress.UI;                   // CustomerCheckout
using MMDress.Gameplay;             // CustomerTimedOut
using MMDress.Runtime.Reputation;   // ReputationService
using MMDress.Customer;             // CustomerController

namespace MMDress.Runtime.Integration
{
    [DisallowMultipleComponent]
    public sealed class ReputationOnCheckout : MonoBehaviour
    {
        [SerializeField] private ReputationService reputation;
        [Tooltip("Jeda minimal untuk mengabaikan event duplikat untuk customer yang sama (detik).")]
        [SerializeField] private float dedupeWindowSeconds = 0.25f;

        // Simpan timestamp terakhir diproses per customer → cegah +2/+3 akibat duplicate publish/subscriber kembar
        private readonly Dictionary<CustomerController, float> _lastProcessed = new Dictionary<CustomerController, float>();

        System.Action<CustomerCheckout> _onCheckout;
        System.Action<CustomerTimedOut> _onTimeout;

        private static ReputationOnCheckout _instance; // guard sederhana agar tidak dobel di scene

        private void Awake()
        {
            if (!reputation)
                reputation = FindObjectOfType<ReputationService>(includeInactive: true);

            // OPTIONAL: cegah duplikasi komponen
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning($"[ReputationOnCheckout] Duplicate di '{gameObject.name}' → dinonaktifkan.");
                enabled = false;
                return;
            }
            _instance = this;
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        private void OnEnable()
        {
            _onCheckout = e =>
            {
                if (!reputation || e.customer == null) return;

                var now = Time.unscaledTime;
                if (_lastProcessed.TryGetValue(e.customer, out var last) && (now - last) < dedupeWindowSeconds)
                    return; // duplikat dekat → abaikan

                _lastProcessed[e.customer] = now;

                // HANYA +1 kalau lengkap (Top & Bottom = 2), selain itu (0/1) dianggap empty → -1
                bool served = e.itemsEquipped >= 2;
                bool empty = e.itemsEquipped < 2;
                reputation.ApplyCheckout(served, empty);
                // Debug.Log($"[Rep] Checkout {e.customer.name} items={e.itemsEquipped} → {(served?"+1":"-1")}%");
            };

            _onTimeout = e =>
            {
                if (!reputation || e.customer == null) return;

                var now = Time.unscaledTime;
                if (_lastProcessed.TryGetValue(e.customer, out var last) && (now - last) < dedupeWindowSeconds)
                    return;

                _lastProcessed[e.customer] = now;

                reputation.ApplyCheckout(served: false, empty: true);
                // Debug.Log($"[Rep] Timeout {e.customer.name} → -1%");
            };

            ServiceLocator.Events.Subscribe(_onCheckout);
            ServiceLocator.Events.Subscribe(_onTimeout);
        }

        private void OnDisable()
        {
            if (_onCheckout != null) ServiceLocator.Events.Unsubscribe(_onCheckout);
            if (_onTimeout != null) ServiceLocator.Events.Unsubscribe(_onTimeout);
            _lastProcessed.Clear();
        }
    }
}
