using UnityEngine;
using MMDress.Core;
using MMDress.UI;
using CheckoutEvt = MMDress.Gameplay.CustomerCheckout;

namespace MMDress.Services
{
    [DisallowMultipleComponent]
    public sealed class EconomyService : MonoBehaviour
    {
        private const string PrefKey = "eco_balance";

        [Header("Balance (runtime)")]
        [SerializeField] private int balance = 0;
        [SerializeField] private int defaultStartingBalance = 5000;
        [SerializeField] private bool saveDefaultOnFirstLoad = true;
        public int Balance => balance;

        [Header("Payout saat CustomerCheckout")]
        [SerializeField] private bool enablePayoutOnCheckout = true;

        [Tooltip("Uang yang diterima jika outfit LENGKAP dan SESUAI order.")]
        [SerializeField] private int payoutFull = 500;

        [Tooltip("Jika outfit lengkap tetapi SALAH order.")]
        [SerializeField] private int payoutWrong = 0;

        [Tooltip("Jika hanya 1 item atau 0 item.")]
        [SerializeField] private int payoutPartialOrEmpty = 0;

        private System.Action<CheckoutEvt> _onCheckout;

        private void Awake()
        {
            LoadOrInit();
        }

        private void Start()
        {
            ServiceLocator.Events?.Publish(new MoneyChanged(0, balance));
        }

        private void OnEnable()
        {
            if (!enablePayoutOnCheckout) return;

            _onCheckout = OnCustomerCheckout;
            ServiceLocator.Events?.Subscribe(_onCheckout);
        }

        private void OnDisable()
        {
            if (_onCheckout != null)
                ServiceLocator.Events?.Unsubscribe(_onCheckout);
        }

        private void LoadOrInit()
        {
            if (PlayerPrefs.HasKey(PrefKey))
            {
                balance = PlayerPrefs.GetInt(PrefKey, balance);
                return;
            }

            balance = defaultStartingBalance;

            if (saveDefaultOnFirstLoad)
            {
                PlayerPrefs.SetInt(PrefKey, balance);
                PlayerPrefs.Save();
            }
        }

        private void OnCustomerCheckout(CheckoutEvt e)
        {
            int amt = 0;

            if (e.itemsEquipped >= 2)
                amt = e.isCorrectOrder ? payoutFull : payoutWrong;
            else
                amt = payoutPartialOrEmpty;

            if (amt != 0)
                Add(amt);
        }

        public bool CanSpend(int amount) => amount <= balance;

        public bool Spend(int amount)
        {
            if (amount <= 0) return true;
            if (balance < amount) return false;

            balance -= amount;
            Save();
            ServiceLocator.Events?.Publish(new MoneyChanged(-amount, balance));
            return true;
        }

        public void Add(int amount)
        {
            if (amount == 0) return;

            balance += amount;
            Save();
            ServiceLocator.Events?.Publish(new MoneyChanged(+amount, balance));
        }

        private void Save()
        {
            PlayerPrefs.SetInt(PrefKey, balance);
            PlayerPrefs.Save();
        }
    }
}