// Assets/MMDress/Scripts/Runtime/Gameplay/Events/CustomerEvents.cs
using System;
using MMDress.Core;       // IEventBus
using MMDress.Customer;   // CustomerController
using MMDress.Data;       // ItemSO, OutfitSlot

namespace MMDress.Gameplay
{
    // ─────────────────────────────────────────────────────────────────────
    // CUSTOMER FLOW
    // ─────────────────────────────────────────────────────────────────────
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

    /// <summary>
    /// Checkout customer dari fitting.
    /// itemsEquipped 0–2, isCorrectOrder = true kalau outfit sesuai pesanan.
    /// </summary>
    public readonly struct CustomerCheckout
    {
        public readonly CustomerController customer;
        public readonly int itemsEquipped;
        public readonly bool isCorrectOrder;

        public CustomerCheckout(CustomerController customer, int itemsEquipped, bool isCorrectOrder)
        {
            this.customer = customer;
            this.itemsEquipped = itemsEquipped;
            this.isCorrectOrder = isCorrectOrder;
        }

        // Backward compat untuk kode lama yang belum pakai flag benar/salah.
        public CustomerCheckout(CustomerController customer, int itemsEquipped)
            : this(customer, itemsEquipped, false)
        {
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // OUTFIT FLOW (baru, tegas per-slot & per-item)
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Dipublish setiap kali user memilih item di list → sekadar preview (belum permanen).
    /// </summary>
    public readonly struct OutfitPreviewChanged
    {
        public readonly CustomerController customer; // boleh null jika free-mode
        public readonly OutfitSlot slot;
        public readonly ItemSO item;

        public OutfitPreviewChanged(CustomerController c, OutfitSlot s, ItemSO i)
        { customer = c; slot = s; item = i; }

        public static void Publish(IEventBus bus, CustomerController c, OutfitSlot s, ItemSO i)
        {
            if (bus == null || i == null) return;
            bus.Publish(new OutfitPreviewChanged(c, s, i));
        }
    }

    /// <summary>
    /// Dipublish saat user menekan tombol Equip (atau auto-commit saat menutup panel).
    /// </summary>
    public readonly struct OutfitEquippedCommitted
    {
        public readonly CustomerController customer; // boleh null jika free-mode
        public readonly OutfitSlot slot;
        public readonly ItemSO item;

        public OutfitEquippedCommitted(CustomerController c, OutfitSlot s, ItemSO i)
        { customer = c; slot = s; item = i; }

        public static void Publish(IEventBus bus, CustomerController c, OutfitSlot s, ItemSO i)
        {
            if (bus == null || i == null) return;
            bus.Publish(new OutfitEquippedCommitted(c, s, i));
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // LEGACY (sementara, untuk backward compatibility)
    // ─────────────────────────────────────────────────────────────────────
    [Obsolete("Gunakan OutfitPreviewChanged(customer, slot, item).")]
    public readonly struct ItemPreviewed
    {
        public readonly ItemSO item;
        public ItemPreviewed(ItemSO i) { item = i; }

        public static void Publish(IEventBus bus, ItemSO i)
        {
            if (bus == null || i == null) return;
            bus.Publish(new ItemPreviewed(i));
        }
    }

    [Obsolete("Gunakan OutfitEquippedCommitted(customer, slot, item).")]
    public readonly struct ItemEquipped
    {
        public readonly ItemSO item;
        public ItemEquipped(ItemSO i) { item = i; }

        public static void Publish(IEventBus bus, ItemSO i)
        {
            if (bus == null || i == null) return;
            bus.Publish(new ItemEquipped(i));
        }
    }
}
