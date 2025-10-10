using UnityEngine;
using MMDress.Core;     // ServiceLocator.Events
using MMDress.UI;       // MoneyChanged  // CustomerCheckout (opsional payout)

namespace MMDress.Services
{
    [DisallowMultipleComponent]
    public sealed class EconomyService : MonoBehaviour
    {
        [Header("Balance (runtime)")]
        [SerializeField] private int balance;

        [Header("Payout saat CustomerCheckout (opsional)")]
        [SerializeField] private bool enablePayoutOnCheckout = false;
        [SerializeField] private int payoutFull = 200;     // 2 item
        [SerializeField] private int payoutPartial = 0;    // 1 item (kita nol-kan, reputasi -1%)
        [SerializeField] private int payoutEmpty = 0;      // 0 item
        [SerializeField, Min(0f)] private float receiveWindowSec = 0.25f;

        public int Balance => balance;

        System.Action<CustomerCheckout> _onCheckout;

        private void OnEnable()
        {
            if (enablePayoutOnCheckout)
            {
                _onCheckout = e =>
                {
                    int amt = e.itemsEquipped >= 2 ? payoutFull :
                              e.itemsEquipped == 1 ? payoutPartial : payoutEmpty;
                    if (amt > 0) Add(amt);
                };
                ServiceLocator.Events.Subscribe(_onCheckout);
            }
        }

        private void OnDisable()
        {
            if (_onCheckout != null) ServiceLocator.Events.Unsubscribe(_onCheckout);
        }

        // ==== API ====
        public bool CanSpend(int amount) => amount <= balance;

        public bool Spend(int amount)
        {
            if (amount <= 0) return true;
            if (balance < amount) return false;
            balance -= amount;
            ServiceLocator.Events?.Publish(new MoneyChanged(-amount, balance));
            return true;
        }

        public void Add(int amount)
        {
            if (amount == 0) return;
            balance += amount;
            ServiceLocator.Events?.Publish(new MoneyChanged(amount, balance));
        }

        // Convenience untuk debug
        [ContextMenu("Add 1000")]
        private void _DebugAdd() => Add(1000);
    }
}
