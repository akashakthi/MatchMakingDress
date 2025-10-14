using UnityEngine;
using MMDress.Gameplay;
using MMDress.Core;

namespace MMDress.UI
{
    /// Menjembatani klik customer → membuka FittingRoomUI.
    [DisallowMultipleComponent]
    public sealed class FittingRoomFlow : MonoBehaviour
    {
        [SerializeField] private FittingRoomUI fittingUI;

        private void OnEnable()
        {
            ServiceLocator.Events.Subscribe<CustomerSelected>(OnCustomerSelected);
        }

        private void OnDisable()
        {
            ServiceLocator.Events.Unsubscribe<CustomerSelected>(OnCustomerSelected);
        }

        private void OnCustomerSelected(CustomerSelected e)
        {
            if (fittingUI && e.customer != null)
                fittingUI.Open(e.customer);
        }
    }
}
