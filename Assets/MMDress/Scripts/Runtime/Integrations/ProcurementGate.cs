using UnityEngine;
using MMDress.Runtime.Timer;
using MMDress.Data;

namespace MMDress.Runtime.Inventory
{
    public sealed class ProcurementGate : MonoBehaviour
    {
        [SerializeField] private TimeOfDayService timeOfDay;

        public bool CanBuy => timeOfDay != null && timeOfDay.CurrentPhase == DayPhase.Prep;

        public void TryBuy(MaterialSO item)
        {
            if (!CanBuy)
            {
                ShowLockedMessage();
                return;
            }
            DoBuy(item);
        }

        private void DoBuy(MaterialSO item)
        {
            // TODO: integrasi dengan Inventory/EconomyService kamu
            // contoh: inventory.Add(item); economy.Pay(cost);
        }

        private void ShowLockedMessage()
        {
            // TODO: tampilkan toast/popup "Belanja hanya 06:00–08:00"
            Debug.LogWarning("Belanja terkunci: hanya boleh pada fase Prep (06:00–08:00).");
        }
    }
}
