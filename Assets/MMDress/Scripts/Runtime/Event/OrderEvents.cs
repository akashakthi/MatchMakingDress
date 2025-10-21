// Assets/MMDress/Scripts/Runtime/Gameplay/Events/OrderEvents.cs
using MMDress.Customer;
using MMDress.Data;

namespace MMDress.Gameplay
{
    public readonly struct OrderAssigned
    {
        public readonly CustomerController customer;
        public readonly OrderSO order;
        public OrderAssigned(CustomerController c, OrderSO o) { customer = c; order = o; }
    }

    public readonly struct OrderResolved
    {
        public readonly CustomerController customer;
        public readonly OrderSO order;
        public readonly bool topOk;
        public readonly bool bottomOk;
        public readonly bool allOk;
        public readonly int payout;
        public OrderResolved(CustomerController c, OrderSO o, bool top, bool bottom, bool all, int pay)
        { customer = c; order = o; topOk = top; bottomOk = bottom; allOk = all; payout = pay; }
    }
}
