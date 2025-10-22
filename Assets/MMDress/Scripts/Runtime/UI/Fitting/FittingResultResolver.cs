using UnityEngine;
using MMDress.Data;
using MMDress.Customer;
using MMDress.Services;
using RepService = MMDress.Runtime.Reputation.ReputationService;

namespace MMDress.UI
{
    /// Mengurus hasil fitting (cek order → payout → reputasi → customer pulang) lalu menutup UI.
    /// Dipanggil dari FittingRoomUI.Close() jika komponen ini terpasang.
    [DisallowMultipleComponent]
    [AddComponentMenu("MMDress/UI/Fitting Result Resolver")]
    public sealed class FittingResultResolver : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private EconomyService economy;
        [SerializeField] private RepService reputation;

        /// return true = sudah di-handle penuh (jangan fallback)
        public bool ResolveAndClose(CustomerController customer,
                                    MMDress.Runtime.Fitting.FittingSession session,
                                    FittingRoomUI ui)
        {
            if (!customer || !session || !ui) return false;

            var top = session.EquippedTop;
            var bottom = session.EquippedBottom;

            var holder = customer.GetComponent<CustomerOrder>();
            var order = holder ? holder.CurrentOrder : null;

            bool topOk = order == null || order.requiredTop == null || top == order.requiredTop;
            bool botOk = order == null || order.requiredBottom == null || bottom == order.requiredBottom;
            bool allOk = topOk && botOk;

            int payout = order ? Mathf.Max(0, order.payout) : 100;
            if (allOk)
            {
                if (economy) economy.Add(payout);
                if (reputation) reputation.AddPercent(+1f);
            }
            else
            {
                if (reputation) reputation.AddPercent(-1f);
            }

            if (holder) holder.SetOrder(null);

            // Customer pulang + tutup UI (JANGAN panggil ui.Close() lagi)
            int equippedCount = (top ? 1 : 0) + (bottom ? 1 : 0);
            customer.FinishFitting(equippedCount);
            ui.InternalClose();

            return true;
        }
    }
}
