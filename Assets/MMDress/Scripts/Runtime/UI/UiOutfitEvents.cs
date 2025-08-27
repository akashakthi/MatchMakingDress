using MMDress.Customer;
using MMDress.Data;

namespace MMDress.UI
{
    /// Dipublish setiap kali pemain menekan Equip (slot apa pun).
    /// Disarankan untuk UI/FX: badge, package icon, sfx, dsb.
    public readonly struct ItemEquipped
    {
        public readonly CustomerController customer;
        public readonly OutfitSlot slot;
        public readonly ItemSO item;

        public ItemEquipped(CustomerController customer, OutfitSlot slot, ItemSO item)
        {
            this.customer = customer;
            this.slot = slot;
            this.item = item;
        }
    }
}
