using MMDress.Services;

namespace MMDress.Core
{
    public static class ServiceLocator
    {
        public static IEventBus Events { get; set; }
        public static IInventoryService Inventory { get; set; }
        public static IWalletService Wallet { get; set; }
        public static ISaveService Save { get; set; }
    }
}
