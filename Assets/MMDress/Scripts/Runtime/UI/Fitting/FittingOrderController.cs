// Assets/MMDress/Scripts/Runtime/UI/Fitting/FittingOrderController.cs
using UnityEngine;
using MMDress.Data;
using MMDress.Services;
using MMDress.Customer;
using MMDress.Core;
using MMDress.Gameplay;

// Alias ke service reputasi yang bener namespace-nya
using RepService = MMDress.Runtime.Reputation.ReputationService;

namespace MMDress.UI
{
    [DisallowMultipleComponent]
    public sealed class FittingOrderController : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private FittingOrderPanel orderPanel;
        [SerializeField] private EconomyService economy;
        [SerializeField] private RepService reputation;   // ← sekarang tipe-nya jelas

        // state sementara (untuk validasi/preview)
        private CustomerController _customer;
        private OrderSO _order;
        private ItemSO _equippedTop;
        private ItemSO _equippedBottom;

        public void OpenFor(CustomerController customer, ItemSO currentTop, ItemSO currentBottom)
        {
            _customer = customer;
            _equippedTop = currentTop;
            _equippedBottom = currentBottom;

            if (!_customer) { orderPanel?.Bind(null); return; }

            var holder = _customer.GetComponent<CustomerOrder>();
            _order = holder ? holder.CurrentOrder : null;

            if (orderPanel)
            {
                orderPanel.Bind(_order);
                orderPanel.ShowMatch(_equippedTop, _equippedBottom);
            }
        }

        // panggil dari UI pilihan item (opsional, biar indikator real-time)
        public void OnPreviewChanged(ItemSO top, ItemSO bottom)
        {
            _equippedTop = top;
            _equippedBottom = bottom;
            if (orderPanel) orderPanel.ShowMatch(_equippedTop, _equippedBottom);
        }

        public void ConfirmEquip()
        {
            if (!_customer)
            {
                Debug.LogWarning("[FittingOrder] Tidak ada customer aktif.");
                return;
            }

            bool topOk = _order == null || _order.requiredTop == null || _equippedTop == _order.requiredTop;
            bool bottomOk = _order == null || _order.requiredBottom == null || _equippedBottom == _order.requiredBottom;
            bool allOk = topOk && bottomOk;

            int payout = (_order ? _order.payout : 100);

            if (allOk)
            {
                if (economy) economy.Add(payout);
                if (reputation) reputation.AddPercent(+1f);   // ← pakai AddPercent
            }
            else
            {
                if (reputation) reputation.AddPercent(-1f);   // ← pakai AddPercent
            }

            ServiceLocator.Events?.Publish(new OrderResolved(_customer, _order, topOk, bottomOk, allOk, allOk ? payout : 0));

            // optional: clear order supaya customer selesai
            var holder = _customer.GetComponent<CustomerOrder>();
            if (holder) holder.SetOrder(null);
        }
    }
}
