using UnityEngine;
using MMDress.Core;
using MMDress.Gameplay;
using MMDress.Data;

namespace MMDress.UI
{
    public class FittingRoomUI : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot; // HARUS anak Canvas yang aktif
        private Customer.CustomerController _current;
        private bool _subscribed;

        void Awake()
        {
            if (panelRoot) panelRoot.SetActive(false);
        }

        void Start()
        {
            if (ServiceLocator.Events == null)
            {
                Debug.LogError("[MMDress] EventBus belum ter-init. Pastikan GameBootstrap ada di scene.");
                return;
            }
            ServiceLocator.Events.Subscribe<CustomerSelected>(OnSelected);
            _subscribed = true;
        }

        void OnDestroy()
        {
            if (_subscribed && ServiceLocator.Events != null)
                ServiceLocator.Events.Unsubscribe<CustomerSelected>(OnSelected);
        }

        private void OnSelected(CustomerSelected e)
        {
            _current = e.customer;
            Debug.Log("[MMDress] FittingRoomUI: Customer selected, membuka panel.");
            if (panelRoot) panelRoot.SetActive(true);
            else Debug.LogWarning("[MMDress] panelRoot belum di-assign ke FittingRoomUI.");

            ServiceLocator.Events.Publish(new FittingUIOpened());
        }

        public void Close()
        {
            if (_current != null)
            {
                _current.Outfit.RevertPreview(); // UI memanggil domain
                _current.FinishFitting();        // -> state Leaving (logic)
            }
            ServiceLocator.Events.Publish(new FittingUIClosed());
            _current = null;
            if (panelRoot) panelRoot.SetActive(false);
        }


        public void Preview(ItemSO item) => _current?.Outfit.TryOn(item);
        public void Equip(ItemSO item) => _current?.Outfit.Equip(item);
    }
}
