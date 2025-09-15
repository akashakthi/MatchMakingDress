using UnityEngine;
using MMDress.Core;
using MMDress.UI;

namespace MMDress.Services
{
    [DisallowMultipleComponent]
    public class EconomyService : MonoBehaviour
    {
        [SerializeField] int basePricePerItem = 50;
        [SerializeField] float fullMultiplier = 1.0f;    // 2 item
        [SerializeField] float partialMultiplier = 0.5f; // 1 item

        int _balance;
        System.Action<CustomerCheckout> _onCheckout;

        void OnEnable()
        {
            _onCheckout = e =>
            {
                float mul = (e.itemsEquipped >= 2) ? fullMultiplier :
                            (e.itemsEquipped == 1) ? partialMultiplier : 0f;
                int payout = Mathf.RoundToInt(e.itemsEquipped * basePricePerItem * mul);
                _balance += payout;
                ServiceLocator.Events.Publish(new MoneyChanged(payout, _balance));
            };
            ServiceLocator.Events.Subscribe(_onCheckout);
        }

        void OnDisable()
        {
            if (_onCheckout != null) ServiceLocator.Events.Unsubscribe(_onCheckout);
        }
    }
}
