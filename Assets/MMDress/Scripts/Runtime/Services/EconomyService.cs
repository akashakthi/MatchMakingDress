using UnityEngine;
using MMDress.Core;
using MMDress.UI;                  // MoneyChanged
using CheckoutEvt = MMDress.Gameplay.CustomerCheckout;

namespace MMDress.Services
{
    [DisallowMultipleComponent]
    public sealed class EconomyService : MonoBehaviour
    {
        const string PrefKey = "eco_balance";

        [Header("Balance (runtime)")]
        [SerializeField] private int balance = 0;
        public int Balance => balance;

        [Header("Payout saat CustomerCheckout")]
        [SerializeField] private bool enablePayoutOnCheckout = true;

        [Tooltip("Uang yang diterima jika outfit LENGKAP dan SESUAI order.")]
        [SerializeField] private int payoutFull = 500;

        [Tooltip("Jika outfit lengkap tetapi SALAH order (boleh 0 kalau tidak mau dibayar).")]
        [SerializeField] private int payoutWrong = 0;

        [Tooltip("Jika hanya 1 item atau 0 item (partial/empty).")]
        [SerializeField] private int payoutPartialOrEmpty = 0;

        [SerializeField, Min(0f)]
        private float receiveWindowSec = 0.25f; // reserved kalau mau anti double-event

        System.Action<CheckoutEvt> _onCheckout;

        void Awake()
        {
            if (PlayerPrefs.HasKey(PrefKey))
                balance = PlayerPrefs.GetInt(PrefKey, balance);
        }

        void Start()
        {
            // render HUD awal
            ServiceLocator.Events?.Publish(new MoneyChanged(0, balance));
        }

        void OnEnable()
        {
            if (!enablePayoutOnCheckout) return;

            _onCheckout = OnCustomerCheckout;
            ServiceLocator.Events.Subscribe(_onCheckout);
        }

        void OnDisable()
        {
            if (_onCheckout != null)
                ServiceLocator.Events.Unsubscribe(_onCheckout);
        }

        void OnCustomerCheckout(CheckoutEvt e)
        {
            // Definisi:
            // - e.itemsEquipped: 0..2 (top+bottom)
            // - e.isCorrectOrder: true kalau top & bottom sesuai request customer

            int amt = 0;

            if (e.itemsEquipped >= 2)
            {
                // outfit lengkap (top + bottom)
                amt = e.isCorrectOrder ? payoutFull : payoutWrong;
            }
            else
            {
                // 0 atau 1 item
                amt = payoutPartialOrEmpty;
            }

            if (amt != 0)
                Add(amt);
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
