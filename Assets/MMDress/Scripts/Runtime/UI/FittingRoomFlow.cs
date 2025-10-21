// Assets/MMDress/Scripts/Runtime/UI/Fitting/FittingRoomFlow.cs
using UnityEngine;
using MMDress.Gameplay;
using MMDress.Core;
using MMDress.Customer;

namespace MMDress.UI
{
    /// Menjembatani klik customer → membuka FittingRoomUI + bind panel order.
    [DisallowMultipleComponent]
    public sealed class FittingRoomFlow : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private FittingRoomUI fittingUI;
        [SerializeField] private FittingOrderController orderController; // <-- drag FittingOrderController di sini

        [Header("Auto-Find (opsional)")]
        [SerializeField] private bool autoFindInScene = true;

        void Awake()
        {
            if (autoFindInScene)
            {
#if UNITY_2023_1_OR_NEWER
                fittingUI ??= Object.FindAnyObjectByType<FittingRoomUI>(FindObjectsInactive.Include);
                orderController ??= Object.FindAnyObjectByType<FittingOrderController>(FindObjectsInactive.Include);
#else
                fittingUI ??= FindObjectOfType<FittingRoomUI>(true);
                orderController ??= FindObjectOfType<FittingOrderController>(true);
#endif
            }
        }

        void OnEnable()
        {
            ServiceLocator.Events?.Subscribe<CustomerSelected>(OnCustomerSelected);
        }

        void OnDisable()
        {
            ServiceLocator.Events?.Unsubscribe<CustomerSelected>(OnCustomerSelected);
        }

        void OnCustomerSelected(CustomerSelected e)
        {
            if (!e.customer) return;
            //
            // 1) Buka panel fitting
            if (fittingUI) fittingUI.Open(e.customer);

            // 2) Bind panel order → ini yang ngisi ikon Top/Bottom sesuai OrderSO customer
            //    Untuk sekarang kirim null (belum ada item terpasang). Nanti bisa diganti
            //    dengan currentTop/currentBottom kalau FittingRoomUI expose properti tsb.
            if (orderController) orderController.OpenFor(e.customer, null, null);
        }
    }
}
