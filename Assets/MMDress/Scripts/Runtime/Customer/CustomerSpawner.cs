using System.Collections.Generic;
using UnityEngine;
using MMDress.Core;

namespace MMDress.Customer
{
    public class CustomerSpawner : MonoBehaviour
    {
        [Header("Points")]
        [SerializeField] Transform spawnPoint;
        [SerializeField] Transform exitPoint;
        [SerializeField] Transform seatsRoot;   // children = seat pos
        [SerializeField] Transform queueRoot;   // children = queue pos
        [SerializeField] GameObject customerPrefab;

        [Header("Rules")]
        [SerializeField] float spawnInterval = 3f;
        [SerializeField] int maxInScene = 5;
        [SerializeField] Vector2 waitSecondsRange = new(10, 20);

        [Header("Pooling")]
        [SerializeField] bool usePooling = true;
        [SerializeField] int prewarm = 5;

        float _timer;
        readonly List<Transform> _seats = new();
        readonly List<Transform> _queue = new();

        bool[] _seatOccupied;
        CustomerController[] _queueOcc;

        readonly List<CustomerController> _active = new();
        SimplePool<CustomerController> _pool;

        void Awake()
        {
            // Seats & Queue
            if (seatsRoot)
            {
                for (int i = 0; i < seatsRoot.childCount; i++) _seats.Add(seatsRoot.GetChild(i));
                _seatOccupied = new bool[_seats.Count];
            }
            if (queueRoot)
            {
                for (int i = 0; i < queueRoot.childCount; i++) _queue.Add(queueRoot.GetChild(i));
                _queueOcc = new CustomerController[_queue.Count];
            }

            // Pool
            if (usePooling)
            {
                _pool = new SimplePool<CustomerController>(
                    factory: CreateNew,
                    onGet: c => { c.transform.position = spawnPoint.position; c.transform.rotation = Quaternion.identity; c.gameObject.SetActive(true); },
                    onRelease: c => { c.gameObject.SetActive(false); }
                );
                int cnt = Mathf.Max(prewarm, maxInScene);
                for (int i = 0; i < cnt; i++) _pool.Release(CreateNew());
            }
        }

        void Start() { _timer = 0f; }

        void Update()
        {
            if (!customerPrefab || !spawnPoint || !exitPoint || _seats.Count == 0) return;

            _timer -= Time.deltaTime;
            if (_timer > 0f) return;
            _timer = spawnInterval;

            int hardCap = Mathf.Min(maxInScene, _seats.Count + _queue.Count);
            if (_active.Count >= hardCap) return;

            int si = FindFreeSeat();
            if (si >= 0) { SpawnToSeat(si); return; }

            int qi = FindFreeQueue();
            if (qi >= 0) { SpawnToQueue(qi); }
        }

        // ===== helpers =====
        CustomerController CreateNew()
        {
            var go = Instantiate(customerPrefab);
            return go.GetComponent<CustomerController>();
        }
        CustomerController Get() => usePooling ? _pool.Get() : CreateNew();
        void Release(CustomerController c)
        {
            _active.Remove(c);
            if (usePooling) _pool.Release(c);
            else Destroy(c.gameObject);
        }

        int FindFreeSeat() { for (int i = 0; i < _seats.Count; i++) if (!_seatOccupied[i]) return i; return -1; }
        int FindFreeQueue() { if (_queue.Count == 0) return -1; for (int i = 0; i < _queue.Count; i++) if (_queueOcc[i] == null) return i; return -1; }

        void SpawnToSeat(int seatIndex)
        {
            var c = Get();
            _active.Add(c);
            _seatOccupied[seatIndex] = true;

            float waitSec = Random.Range(waitSecondsRange.x, waitSecondsRange.y);
            c.InitSeat(_seats[seatIndex].position, exitPoint.position, seatIndex, FreeSeat, waitSec, OnDespawn);
        }

        void SpawnToQueue(int queueIndex)
        {
            var c = Get();
            _active.Add(c);
            _queueOcc[queueIndex] = c;

            float waitSec = Random.Range(waitSecondsRange.x, waitSecondsRange.y);
            c.InitQueue(_queue[queueIndex].position, exitPoint.position, queueIndex, FreeQueue, waitSec, OnDespawn);
        }

        void OnDespawn(CustomerController c) => Release(c);

        void FreeSeat(int seatIndex)
        {
            if (seatIndex < 0 || seatIndex >= _seatOccupied.Length) return;
            _seatOccupied[seatIndex] = false;

            // Promosikan antrian depan
            for (int qi = 0; qi < _queueOcc.Length; qi++)
            {
                var waiting = _queueOcc[qi];
                if (waiting == null) continue;

                _queueOcc[qi] = null;               // kosongkan slot antrian
                _seatOccupied[seatIndex] = true;    // seat langsung terisi
                waiting.PromoteToSeat(_seats[seatIndex].position, seatIndex, FreeSeat);
                break;
            }
        }

        void FreeQueue(int queueIndex)
        {
            if (_queueOcc == null) return;
            if (queueIndex < 0 || queueIndex >= _queueOcc.Length) return;
            _queueOcc[queueIndex] = null;
        }
    }
}
