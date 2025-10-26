// Assets/MMDress/Scripts/Runtime/Gameplay/Customer/CustomerOrder.cs
using System;
using UnityEngine;
using MMDress.Data;
using MMDress.Services;
// alias biar jelas reputasi versi runtime
using RepService = MMDress.Runtime.Reputation.ReputationService;

namespace MMDress.Customer
{
    [DisallowMultipleComponent]
    [AddComponentMenu("MMDress/Customer/Customer Order Holder")]
    public sealed class CustomerOrder : MonoBehaviour
    {
        [Header("State")]
        [SerializeField] private OrderSO currentOrder;
        public OrderSO CurrentOrder => currentOrder;
        public bool HasOrder => currentOrder != null;

        // Convenience getter (bisa null)
        public ItemSO RequiredTop => currentOrder ? currentOrder.requiredTop : null;
        public ItemSO RequiredBottom => currentOrder ? currentOrder.requiredBottom : null;

        [Header("Debug")]
        [SerializeField] private bool verbose = false;

        /// Dipanggil saat order berubah (oldOrder, newOrder)
        public event Action<OrderSO, OrderSO> OnOrderChanged;

        /// Set order (boleh null). Menembak event bila berubah.
        public void SetOrder(OrderSO o)
        {
            if (ReferenceEquals(o, currentOrder)) return;
            var old = currentOrder;
            currentOrder = o;

            if (verbose)
                Debug.Log($"[CustomerOrder] {(name)} -> {(o ? o.name : "(none)")} | {GetDebugString()}", this);

            OnOrderChanged?.Invoke(old, currentOrder);
        }

        public void ClearOrder() => SetOrder(null);

        /// Assign random order dari OrderService. Stage opsional (kalau -1, ambil dari ReputationService jika ada).
        public void AssignRandom(OrderService service, int stage = -1, RepService rep = null)
        {
            if (!service)
            {
                if (verbose) Debug.LogWarning("[CustomerOrder] AssignRandom gagal: OrderService null.", this);
                return;
            }

            int st = stage > 0 ? stage : (rep ? Mathf.Clamp(rep.Stage, 1, 3) : 1);
            var order = service.GetRandomOrder(st);
            SetOrder(order);
        }

        /// String ringkas buat debugging/log
        public string GetDebugString()
        {
            string top = RequiredTop ? (RequiredTop.displayName ?? RequiredTop.name) : "Bebas";
            string bot = RequiredBottom ? (RequiredBottom.displayName ?? RequiredBottom.name) : "Bebas";
            int payout = currentOrder ? currentOrder.payout : 0;
            return $"Top={top}, Bottom={bot}, Payout={payout}";
        }

        // ───────────── Context Menu: handy saat debug dari Inspector ─────────────
        [ContextMenu("Debug/Log Order")]
        private void ContextLog() => Debug.Log($"[CustomerOrder] {name}: {GetDebugString()}", this);

        [ContextMenu("Debug/Clear Order")]
        private void ContextClear() => ClearOrder();

        [ContextMenu("Debug/Assign Random (Stage=1)")]
        private void ContextAssignS1() => TryContextAssign(1);

        [ContextMenu("Debug/Assign Random (Stage=2)")]
        private void ContextAssignS2() => TryContextAssign(2);

        [ContextMenu("Debug/Assign Random (Stage=3)")]
        private void ContextAssignS3() => TryContextAssign(3);

        private void TryContextAssign(int stage)
        {
            var svc = FindObjectOfType<OrderService>(true);
            if (!svc) { Debug.LogWarning("[CustomerOrder] OrderService tidak ditemukan di scene.", this); return; }
            AssignRandom(svc, stage);
        }

#if UNITY_EDITOR
        // Label kecil di Scene View kalau object dipilih — enak buat cek cepat
        private void OnDrawGizmosSelected()
        {
            if (!currentOrder) return;
            var pos = transform.position;
            pos.y += 0.25f;

            // Hindari pakai Handles warna custom agar tidak noisy
            UnityEditor.Handles.Label(pos, $"Order: {GetDebugString()}");
        }
#endif
    }
}
