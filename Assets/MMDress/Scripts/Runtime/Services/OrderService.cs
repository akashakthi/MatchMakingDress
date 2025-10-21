using System.Collections.Generic;
using UnityEngine;
using MMDress.Data;

namespace MMDress.Services
{
    [DisallowMultipleComponent]
    public sealed class OrderService : MonoBehaviour
    {
        [Header("Pool / Library")]
        [SerializeField] private OrderLibrarySO library;

        [Header("Debug")]
        [SerializeField] private bool verbose = false;

        // buffer internal untuk seleksi berbobot
        private readonly List<OrderLibrarySO.Entry> _buf = new();

        /// Overload default (stage 1) agar pemanggilan lama tetap jalan.
        public OrderSO GetRandomOrder() => GetRandomOrder(1);

        /// Ambil order acak berdasarkan stage reputasi (1..3) + bobot.
        public OrderSO GetRandomOrder(int stage)
        {
            if (!library || library.entries == null || library.entries.Count == 0)
            {
                if (verbose) Debug.LogWarning("[OrderService] Library kosong / belum di-assign.");
                return null;
            }

            _buf.Clear();
            foreach (var e in library.entries)
            {
                if (e == null || e.order == null) continue;
                int min = Mathf.Clamp(e.minStage, 1, 3);
                int max = Mathf.Clamp(e.maxStage, 1, 3);
                if (max < min) (min, max) = (max, min); // jaga-jaga
                if (stage >= min && stage <= max && e.weight > 0)
                    _buf.Add(e);
            }

            int totalW = SumWeight(_buf);
            if (totalW <= 0)
            {
                if (verbose) Debug.LogWarning("[OrderService] Tidak ada entri cocok untuk stage ini.");
                return null;
            }

            int r = Random.Range(0, totalW);
            foreach (var e in _buf)
            {
                r -= Mathf.Max(0, e.weight);
                if (r < 0)
                {
                    if (verbose) Debug.Log($"[OrderService] Pick: {(e.order ? e.order.name : "(null)")} (stage={stage})");
                    return e.order;
                }
            }

            // fallback—harusnya tidak sampai sini
            var last = _buf[_buf.Count - 1].order;
            if (verbose) Debug.Log($"[OrderService] Fallback pick last: {(last ? last.name : "(null)")}.");
            return last;
        }

        private static int SumWeight(List<OrderLibrarySO.Entry> list)
        {
            int t = 0;
            for (int i = 0; i < list.Count; i++)
                t += Mathf.Max(0, list[i].weight);
            return t;
        }
    }
}
