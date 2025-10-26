using MMDress.Services;

namespace MMDress.Core
{
    /// <summary>
    /// Service registry sederhana (runtime-only). Set di Bootstrap.
    /// </summary>
    public static class ServiceLocator
    {
        /// <summary>Event bus global. Wajib diisi saat bootstrap.</summary>
        public static IEventBus Events { get; set; }

        // Interface di bawah opsional sesuai modulmu.
        public static IInventoryService Inventory { get; set; }
        public static IWalletService Wallet { get; set; }
        public static ISaveService Save { get; set; }

        /// <summary>Skoring permainan / metrik performa.</summary>
        public static IScoreService Score { get; set; }
    }
}
