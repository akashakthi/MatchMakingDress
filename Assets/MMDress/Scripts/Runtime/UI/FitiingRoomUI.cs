using UnityEngine;
using MMDress.Core;
using MMDress.Gameplay;
using MMDress.Data;

namespace MMDress.UI
{
    /// <summary>
    /// Tetap aktif di scene. 'panelRoot' yang diaktif/nonaktif.
    /// Dengar event CustomerSelected lalu buka panel & bind customer.
    /// </summary>
    public class FittingRoomUI : MonoBehaviour
    {
        [Header("Assign ke panel (GameObject) yang ingin ditampilkan/sembunyikan")]
        [SerializeField] private GameObject panelRoot;

        private Customer.CustomerController _current;

        void Awake()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
            ServiceLocator.Events.Subscribe<CustomerSelected>(OnSelected);
        }

        void OnDestroy()
        {
            ServiceLocator.Events.Unsubscribe<CustomerSelected>(OnSelected);
        }

        private void OnSelected(CustomerSelected e)
        {
            _current = e.customer;
            if (panelRoot != null) panelRoot.SetActive(true);
            // TODO Day-4: bind grid item Top/Bottom di sini
        }

        // Dipanggil tombol UI "Close"
        public void Close()
        {
            if (_current != null) _current.Outfit.RevertPreview();
            _current = null;
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        // Dipanggil tombol UI "Preview/Equip" saat grid diimplementasi (Day-4)
        public void Preview(ItemSO item) { _current?.Outfit.TryOn(item); }
        public void Equip(ItemSO item) { _current?.Outfit.Equip(item); }
    }
}
