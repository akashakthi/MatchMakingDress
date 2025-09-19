using UnityEngine;
using MMDress.Core;   // ServiceLocator.Events
using MMDress.UI;     // MoneyChanged, CustomerCheckout

namespace MMDress.Services
{
    /// <summary>
    /// Dompet ekonomi game: sumber kebenaran saldo uang.
    /// - Bisa di-set saldo awal (untuk start of day via ProcurementService).
    /// - Bisa Add/TrySpend untuk pembelian bahan.
    /// - (Opsional) Payout sederhana saat CustomerCheckout (full/partial/empty).
    /// Mempublish MoneyChanged agar MoneyHudView tetap sinkron.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EconomyService : MonoBehaviour
    {
        [Header("Balance (runtime)")]
        [SerializeField] private int _balance = 0;
        public int Balance => _balance;

        [Header("Payout saat CustomerCheckout (opsional)")]
        [SerializeField] private bool enablePayoutOnCheckout = true;
        [Tooltip("Bayaran jika outfit lengkap (Top & Bottom).")]
        [SerializeField] private int payoutFull = 200;
        [Tooltip("Bayaran jika parsial (hanya Top atau Bottom).")]
        [SerializeField] private int payoutPartial = 100;
        [Tooltip("Bayaran jika kosong (0 item).")]
        [SerializeField] private int payoutEmpty = 0;

        // cache subscriber
        private System.Action<CustomerCheckout> _onCheckout;

        private void OnEnable()
        {
            if (enablePayoutOnCheckout)
            {
                _onCheckout = e =>
                {
                    int items = Mathf.Max(0, e.itemsEquipped);
                    int delta =
                        (items >= 2) ? payoutFull :
                        (items == 1) ? payoutPartial :
                                       payoutEmpty;

                    if (delta != 0) Add(delta);
                    else PublishMoneyChanged(0); // tetap publish agar HUD sinkron jika perlu
                };
                ServiceLocator.Events?.Subscribe(_onCheckout);
            }

            // Publish sekali di awal supaya HUD render saldo saat scene start
            PublishMoneyChanged(0);
        }

        private void OnDisable()
        {
            if (_onCheckout != null) ServiceLocator.Events?.Unsubscribe(_onCheckout);
        }

        // ===== API Dompet =====

        /// <summary>Set saldo eksplisit (dipakai saat start of day). Selalu publish MoneyChanged.</summary>
        public void SetBalance(int value)
        {
            _balance = Mathf.Max(0, value);
            PublishMoneyChanged(0);
        }

        /// <summary>Tambah saldo. Nilai negatif juga boleh (akan dipaksa minimal 0).</summary>
        public void Add(int amount)
        {
            _balance = Mathf.Max(0, _balance + amount);
            PublishMoneyChanged(amount);
        }

        /// <summary>Coba kurangi saldo. Berhasil: publish MoneyChanged.</summary>
        public bool TrySpend(int amount)
        {
            if (amount <= 0) return true;
            if (_balance < amount) return false;
            _balance -= amount;
            PublishMoneyChanged(-amount);
            return true;
        }

        // ===== Helper =====
        private void PublishMoneyChanged(int delta)
        {
            ServiceLocator.Events?.Publish(new MoneyChanged { amount = delta, balance = _balance });
        }

    }
}
