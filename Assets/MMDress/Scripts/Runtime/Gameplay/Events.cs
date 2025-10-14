// Assets/MMDress/Scripts/Runtime/Gameplay/Events.cs
using System;
using MMDress.Customer;
using MMDress.Data;

namespace MMDress.Gameplay
{
    // ————————————————————————————————————————
    // CUSTOMER FLOW (tetap)
    // ————————————————————————————————————————
    public readonly struct CustomerSpawned
    {
        public readonly CustomerController customer;
        public CustomerSpawned(CustomerController c) { customer = c; }
    }

    public readonly struct CustomerSelected
    {
        public readonly CustomerController customer;
        public CustomerSelected(CustomerController c) { customer = c; }
    }

    public readonly struct CustomerServed
    {
        public readonly CustomerController customer;
        public readonly int points;
        public CustomerServed(CustomerController c, int p) { customer = c; points = p; }
    }

    public readonly struct CustomerTimedOut
    {
        public readonly CustomerController customer;
        public CustomerTimedOut(CustomerController c) { customer = c; }
    }

    public readonly struct FittingUIOpened { }
    public readonly struct FittingUIClosed { }

    // ————————————————————————————————————————
    // OUTFIT FLOW (baru, dipisah tegas)
    // ————————————————————————————————————————

    /// Dipublish SETIAP user klik item di list (preview saja, non-permanen).
    public readonly struct OutfitPreviewChanged
    {
        public readonly CustomerController customer; // boleh null kalau free-mode
        public readonly OutfitSlot slot;
        public readonly ItemSO item;

        public OutfitPreviewChanged(CustomerController c, OutfitSlot s, ItemSO i)
        { customer = c; slot = s; item = i; }
    }

    /// Dipublish SAAT dikonfirmasi (tombol Equip, atau auto-commit saat Close).
    public readonly struct OutfitEquippedCommitted
    {
        public readonly CustomerController customer; // boleh null kalau free-mode
        public readonly OutfitSlot slot;
        public readonly ItemSO item;

        public OutfitEquippedCommitted(CustomerController c, OutfitSlot s, ItemSO i)
        { customer = c; slot = s; item = i; }

        // Helper publik untuk publish per-slot secara aman
        public static void Publish(MMDress.Core.SimpleEventBus bus, CustomerController c, OutfitSlot s, ItemSO i)
        {
            if (bus != null && i != null) bus.Publish(new OutfitEquippedCommitted(c, s, i));
        }
    }

    // ————————————————————————————————————————
    // LEGACY (untuk backward-compat sementara)
    // ————————————————————————————————————————

    [Obsolete("Gunakan OutfitPreviewChanged(customer, slot, item).")]
    public readonly struct ItemPreviewed
    {
        public readonly ItemSO item;
        public ItemPreviewed(ItemSO i) { item = i; }
    }

    [Obsolete("Gunakan OutfitEquippedCommitted(customer, slot, item).")]
    public readonly struct ItemEquipped
    {
        public readonly ItemSO item;
        public ItemEquipped(ItemSO i) { item = i; }
    }
}
