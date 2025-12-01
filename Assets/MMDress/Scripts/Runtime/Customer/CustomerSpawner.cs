using System.Collections.Generic;
using UnityEngine;
using MMDress.Services;
using RepService = MMDress.Runtime.Reputation.ReputationService;
using MMDress.Core;

namespace MMDress.Customer
{
    [DisallowMultipleComponent]
    [AddComponentMenu("MMDress/Gameplay/Customer Spawner (Path Based)")]
    public sealed class CustomerSpawner : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform spawnPoint;

        [Header("Customer Prefabs (Random Pick)")]
        [Tooltip("Drag Customer1..Customer8 ke sini.")]
        [SerializeField] private List<GameObject> customerPrefabs = new();

        [System.Serializable]
        public sealed class WalkPath
        {
            public string name;
            [Tooltip("Waypoint dari pintu ke posisi akhir (kursi / spot tunggu). Urut!")]
            public Transform[] waypoints;
        }

        [Header("Walk Paths (multi waypoint)")]
        [Tooltip("Setiap element = 1 jalur. Satu customer hanya memakai 1 jalur sekali jalan.")]
        [SerializeField] private WalkPath[] paths;

        [Header("Rules")]
        [SerializeField, Min(0.1f)] private float spawnInterval = 3f;
        [SerializeField, Min(1)] private int maxInScene = 3;
        [SerializeField] private Vector2 waitSecondsRange = new Vector2(10, 20);

        [Header("Pooling")]
        [SerializeField] private bool usePooling = true;
        [SerializeField, Min(0)] private int prewarm = 5;

        [Header("Orders")]
        [SerializeField] private OrderService orderService;
        [SerializeField] private RepService reputation;
        [SerializeField] private bool autoFindServices = true;
        [SerializeField] private bool verboseOrderLog = false;

        // ---- runtime state ----
        private float _spawnAccu;
        private SimplePool<CustomerController> _pool;
        private readonly List<CustomerController> _active = new();
        // mapping customer -> index path yang dia pakai, supaya path tidak dipakai dobel
        private readonly Dictionary<CustomerController, int> _customerPath
            = new Dictionary<CustomerController, int>();

        private void Reset()
        {
            autoFindServices = true;
        }

        private void Awake()
        {
            if (autoFindServices)
            {
#if UNITY_2023_1_OR_NEWER
                orderService ??= UnityEngine.Object.FindAnyObjectByType<OrderService>(FindObjectsInactive.Include);
                reputation ??= UnityEngine.Object.FindAnyObjectByType<RepService>(FindObjectsInactive.Include);
#else
                orderService ??= FindObjectOfType<OrderService>(true);
                reputation   ??= FindObjectOfType<RepService>(true);
#endif
            }

            if (usePooling)
            {
                _pool = new SimplePool<CustomerController>(
                    factory: CreateNew,
                    onGet: c =>
                    {
                        if (!c) return;
                        c.gameObject.SetActive(true);
                        if (spawnPoint) c.transform.position = spawnPoint.position;
                    },
                    onRelease: c =>
                    {
                        if (c) c.gameObject.SetActive(false);
                    });

                // prewarm
                for (int i = 0; i < prewarm; i++)
                {
                    var tmp = _pool.Get();
                    _pool.Release(tmp);
                }
            }
        }

        private void Update()
        {
            if (!CanSpawn()) return;

            _spawnAccu += Time.deltaTime;

            while (_spawnAccu >= spawnInterval)
            {
                _spawnAccu -= spawnInterval;
                if (_active.Count >= maxInScene) break;

                int pathIndex = FindRandomFreePath();
                if (pathIndex < 0) break; // semua path dipakai

                SpawnToPath(pathIndex);
            }
        }

        private bool CanSpawn()
        {
            if (!spawnPoint) return false;
            if (customerPrefabs == null || customerPrefabs.Count == 0) return false;
            if (paths == null || paths.Length == 0) return false;
            return true;
        }

        /// <summary>Cari satu index path yang belum dipakai customer manapun.</summary>
        private int FindRandomFreePath()
        {
            if (paths == null || paths.Length == 0) return -1;

            List<int> free = null;

            for (int i = 0; i < paths.Length; i++)
            {
                bool occupied = false;
                foreach (var kv in _customerPath)
                {
                    if (kv.Value == i) { occupied = true; break; }
                }

                if (!occupied)
                {
                    free ??= new List<int>();
                    free.Add(i);
                }
            }

            if (free == null || free.Count == 0)
                return -1;

            int pick = free[Random.Range(0, free.Count)];
            return pick;
        }

        private CustomerController CreateNew()
        {
            if (customerPrefabs == null || customerPrefabs.Count == 0)
            {
                Debug.LogError("[CustomerSpawner] customerPrefabs kosong.", this);
                return null;
            }

            var pf = customerPrefabs[Random.Range(0, customerPrefabs.Count)];
            var go = Instantiate(pf, spawnPoint ? spawnPoint.position : Vector3.zero, Quaternion.identity);
            var ctrl = go.GetComponent<CustomerController>();

            if (!ctrl)
                Debug.LogError("[CustomerSpawner] Prefab tidak punya CustomerController.", pf);

            return ctrl;
        }

        private CustomerController Get()
        {
            if (!usePooling)
                return CreateNew();

            var c = _pool.Get();
            if (!c) c = CreateNew();
            return c;
        }

        private void Release(CustomerController c)
        {
            _active.Remove(c);
            _customerPath.Remove(c);

            if (usePooling && c)
                _pool.Release(c);
            else if (c)
                Destroy(c.gameObject);
        }

        private void SpawnToPath(int pathIndex)
        {
            var c = Get();
            if (!c) return;

            _active.Add(c);
            _customerPath[c] = pathIndex;

            // --- build path dari Transform[] ke Vector3[] ---
            Vector3[] pathPositions = null;
            var wp = paths[pathIndex];

            if (wp != null && wp.waypoints != null && wp.waypoints.Length > 0)
            {
                pathPositions = new Vector3[wp.waypoints.Length];
                for (int i = 0; i < wp.waypoints.Length; i++)
                    pathPositions[i] = wp.waypoints[i].position;
            }

            // titik akhir = waypoint terakhir (spot tunggu / “seat” imajiner)
            Vector3 seatPos = (pathPositions != null && pathPositions.Length > 0)
                ? pathPositions[pathPositions.Length - 1]
                : c.transform.position;

            float waitSec = Random.Range(waitSecondsRange.x, waitSecondsRange.y);

            // panggil InitSeat versi baru (tanpa exitPos)
            c.InitSeat(
                seatPos,
                -1,                 // seatIndex tidak dipakai (dummy)
                _ => { },           // onSeatFreed dummy (tidak ada kursi fisik)
                waitSec,
                OnCustomerDespawn,
                pathPositions
            );

            // ==== Assign order via CustomerOrder ====
            if (orderService)
            {
                var holder = c.GetComponent<CustomerOrder>();
                if (holder)
                {
                    holder.AssignRandom(orderService, -1, reputation);

                    if (verboseOrderLog)
                        Debug.Log($"[Spawner] Assigned order: {holder.GetDebugString()} ke {c.name}", this);
                }
            }
        }

        private void OnCustomerDespawn(CustomerController c)
        {
            Release(c);
        }
    }
}
