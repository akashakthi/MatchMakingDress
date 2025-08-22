namespace MMDress.Gameplay
{
    public readonly struct CustomerSpawned
    {
        public readonly Customer.CustomerController customer;
        public CustomerSpawned(Customer.CustomerController c) { customer = c; }
    }

    public readonly struct CustomerSelected
    {
        public readonly Customer.CustomerController customer;
        public CustomerSelected(Customer.CustomerController c) { customer = c; }
    }

    public readonly struct ItemPreviewed
    {
        public readonly Data.ItemSO item;
        public ItemPreviewed(Data.ItemSO i) { item = i; }
    }

    public readonly struct ItemEquipped
    {
        public readonly Data.ItemSO item;
        public ItemEquipped(Data.ItemSO i) { item = i; }
    }
}
