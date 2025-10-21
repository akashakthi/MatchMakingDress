// Assets/MMDress/Scripts/Runtime/Customer/AssignOrderOnSpawn.cs
using UnityEngine;
using MMDress.Services;
using MMDress.Customer;

// ⬇️ add this alias to disambiguate which ReputationService you want:
using RepService = MMDress.Runtime.Reputation.ReputationService;

namespace MMDress.Customer
{
    [DisallowMultipleComponent]
    public sealed class AssignOrderOnSpawn : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private OrderService orderService;
        [SerializeField] private RepService reputation;   // ← use the alias here

        [Header("Debug")]
        [SerializeField] private bool verbose = false;
        // AssignOrderOnSpawn.cs (tambahan kualitas hidup)
        void Awake()
        {
            if (!orderService) orderService = FindObjectOfType<OrderService>(true);
            if (!reputation) reputation = FindObjectOfType<MMDress.Runtime.Reputation.ReputationService>(true);
        }

        public void AssignTo(CustomerController customer)
        {
            if (!customer)
            {
                if (verbose) Debug.LogWarning("[AssignOrder] customer null");
                return;
            }
            if (!orderService)
            {
                Debug.LogWarning("[AssignOrder] OrderService belum di-assign.");
                return;
            }

            int stage = reputation ? Mathf.Clamp(reputation.Stage, 1, 3) : 1;
            var order = orderService.GetRandomOrder(stage);

            var holder = customer.GetComponent<CustomerOrder>();
            if (holder)
            {
                holder.SetOrder(order);
                if (verbose)
                    Debug.Log($"[AssignOrder] stage={stage} -> {(order ? order.name : "(null)")} | {holder.GetDebugString()}",
                        customer);
            }
            else
            {
                Debug.LogWarning("[AssignOrder] CustomerOrder tidak ditemukan di prefab customer.");
            }
        }

       //
    }
}
