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

        [Header("Fallback")]
        [Tooltip("Kalau stage saat ini kosong, coba fallback ke stage lebih rendah dulu.")]
        [SerializeField] private bool fallbackToLowerStage = true;

        [Tooltip("Kalau semua stage target kosong, ambil order apapun dari library.")]
        [SerializeField] private bool fallbackToAnyOrder = true;

        [Header("Debug")]
        [SerializeField] private bool verbose = false;

        private readonly List<OrderLibrarySO.Entry> _buf = new();

        public OrderSO GetRandomOrder() => GetRandomOrder(1);

        public OrderSO GetRandomOrder(int stage)
        {
            stage = Mathf.Clamp(stage, 1, 3);

            if (!library || library.entries == null || library.entries.Count == 0)
            {
                if (verbose) Debug.LogWarning("[OrderService] Library kosong / belum di-assign.");
                return null;
            }

            // 1) coba stage target
            var picked = TryPickFromStage(stage);
            if (picked != null)
            {
                if (verbose) Debug.Log($"[OrderService] Pick direct: {picked.name} (stage={stage})");
                return picked;
            }

            // 2) fallback ke stage lebih rendah
            if (fallbackToLowerStage)
            {
                for (int s = stage - 1; s >= 1; s--)
                {
                    picked = TryPickFromStage(s);
                    if (picked != null)
                    {
                        if (verbose)
                            Debug.LogWarning($"[OrderService] Stage {stage} kosong. Fallback ke stage {s}: {picked.name}");
                        return picked;
                    }
                }
            }

            // 3) fallback ke order apapun
            if (fallbackToAnyOrder)
            {
                picked = TryPickAny();
                if (picked != null)
                {
                    if (verbose)
                        Debug.LogWarning($"[OrderService] Tidak ada order valid untuk stage {stage}. Fallback ANY: {picked.name}");
                    return picked;
                }
            }

            if (verbose)
                Debug.LogWarning($"[OrderService] Gagal pick order untuk stage {stage}. Semua fallback kosong.");

            return null;
        }

        private OrderSO TryPickFromStage(int stage)
        {
            _buf.Clear();

            foreach (var e in library.entries)
            {
                if (e == null || e.order == null) continue;

                int min = Mathf.Clamp(e.minStage, 1, 3);
                int max = Mathf.Clamp(e.maxStage, 1, 3);
                if (max < min) (min, max) = (max, min);

                if (stage >= min && stage <= max && e.weight > 0)
                    _buf.Add(e);
            }

            return PickWeighted(_buf);
        }

        private OrderSO TryPickAny()
        {
            _buf.Clear();

            foreach (var e in library.entries)
            {
                if (e == null || e.order == null) continue;
                if (e.weight > 0) _buf.Add(e);
            }

            return PickWeighted(_buf);
        }

        private OrderSO PickWeighted(List<OrderLibrarySO.Entry> list)
        {
            int totalW = SumWeight(list);
            if (totalW <= 0) return null;

            int r = Random.Range(0, totalW);
            foreach (var e in list)
            {
                r -= Mathf.Max(0, e.weight);
                if (r < 0)
                    return e.order;
            }

            return list.Count > 0 ? list[list.Count - 1].order : null;
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