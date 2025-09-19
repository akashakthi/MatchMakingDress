namespace MMDress.UI
{
    // dipublish oleh StockService setiap stok berubah
    public struct InventoryChanged { }

    // dipublish oleh ProcurementService saat belanja/craft
    public struct PurchaseSucceeded
    {
        public string material; public int qty; public int cost;
        public PurchaseSucceeded(MMDress.Runtime.Inventory.MaterialType t, int q, int c)
        { material = t.ToString(); qty = q; cost = c; }
    }
    public struct PurchaseFailed { public string reason; public PurchaseFailed(string r) { reason = r; } }

    public struct CraftSucceeded
    {
        public string slot; public int typeIndex; public int qty;
        public CraftSucceeded(MMDress.Runtime.Inventory.GarmentSlot s, int idx, int q)
        { slot = s.ToString(); typeIndex = idx; qty = q; }
    }
    public struct CraftFailed { public string reason; public CraftFailed(string r) { reason = r; } }

    // Ringkasan akhir hari (dipublish saat ShopClosed oleh ProcurementService)
    public struct EndOfDayArrived { }
}
