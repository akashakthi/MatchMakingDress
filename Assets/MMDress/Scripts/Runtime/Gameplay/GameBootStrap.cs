using UnityEngine;
using MMDress.Core;
using MMDress.Services;
using MMDress.Data;

namespace MMDress.Gameplay
{
    public class GameBootstrap : MonoBehaviour
    {
        [Header("Data References")]
        public InventorySO preloadInventory;

        private void Awake()
        {
            ServiceLocator.Events = new SimpleEventBus();
            ServiceLocator.Inventory = new DevInventoryService(preloadInventory);
            ServiceLocator.Wallet = new DevWalletService();
            ServiceLocator.Save = new PlayerPrefsSaveService();
            Debug.Log("[MMDress] Bootstrap ready.");
        }
    }
}
