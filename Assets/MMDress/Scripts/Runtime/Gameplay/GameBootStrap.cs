// Assets/MMDress/Scripts/Runtime/Gameplay/GameBootstrap.cs
using UnityEngine;
using MMDress.Core;
using MMDress.Services;
using MMDress.Data;

namespace MMDress.Gameplay
{
    [DefaultExecutionOrder(-1000)] // penting agar EventBus & services siap lebih dulu
    public class GameBootstrap : MonoBehaviour
    {
        public InventorySO preloadInventory;

        private void Awake()
        {
            ServiceLocator.Events = new SimpleEventBus();
            ServiceLocator.Inventory = new DevInventoryService(preloadInventory);
            ServiceLocator.Wallet = new DevWalletService();
            ServiceLocator.Save = new PlayerPrefsSaveService();
            ServiceLocator.Score = new DevScoreService();                 // <-- baru
            Debug.Log("[MMDress] Bootstrap ready.");
        }
    }
}
