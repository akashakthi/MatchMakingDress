// Assets/MMDress/Scripts/Runtime/Services/EconomyService.cs
using UnityEngine;
using MMDress.Core;
using MMDress.UI;

namespace MMDress.Services
{
    [DisallowMultipleComponent]
    public sealed class EconomyService : MonoBehaviour
    {
        const string PrefKey = "eco_balance";

        [Header("Balance (runtime)")]
        [SerializeField] private int balance = 0;

        [Header("Payout saat CustomerCheckout (opsional)")]
        [SerializeField] private bool enablePayoutOnCheckout = false;
        [SerializeField] private int payoutFull = 200;
        [SerializeField] private int payoutPartial = 0;
        [SerializeField] private int payoutEmpty = 0;
        [SerializeField, Min(0f)] private float receiveWindowSec = 0.25f;

        public int Balance => balance;

        System.Action<CustomerCheckout> _onCheckout;

        void Awake()
        {
            if (PlayerPrefs.HasKey(PrefKey)) balance = PlayerPrefs.GetInt(PrefKey, balance);
        }

        void Start()
        {
            // render HUD awal
            ServiceLocator.Events?.Publish(new MoneyChanged(0, balance));
        }

        void OnEnable()
        {
            if (!enablePayoutOnCheckout) return;

            _onCheckout = e =>
            {
                int amt = e.itemsEquipped >= 2 ? payoutFull :
                          e.itemsEquipped == 1 ? payoutPartial : payoutEmpty;
                if (amt > 0) Add(amt);
            };
            ServiceLocator.Events.Subscribe(_onCheckout);
        }

        void OnDisable()
        {
            if (_onCheckout != null) ServiceLocator.Events.Unsubscribe(_onCheckout);
        }

        public bool CanSpend(int amount) => amount <= balance;

        public bool Spend(int amount)
        {
            if (amount <= 0) return true;
            if (balance < amount) return false;

            balance -= amount;
            PlayerPrefs.SetInt(PrefKey, balance);
            PlayerPrefs.Save();

            ServiceLocator.Events?.Publish(new MoneyChanged(-amount, balance));
            return true;
        }

        public void Add(int amount)
        {
            if (amount == 0) return;

            balance += amount;
            PlayerPrefs.SetInt(PrefKey, balance);
            PlayerPrefs.Save();

            ServiceLocator.Events?.Publish(new MoneyChanged(+amount, balance));
        }
    }
}
