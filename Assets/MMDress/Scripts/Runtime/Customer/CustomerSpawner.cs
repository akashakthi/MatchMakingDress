using System.Collections.Generic;
using UnityEngine;
using MMDress.Core;

namespace MMDress.Customer
{
    [DisallowMultipleComponent]
    [AddComponentMenu("MMDress/Gameplay/Customer Spawner")]
    public class CustomerSpawner : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private Transform exitPoint;
        [SerializeField] private Transform seatsRoot;
        [SerializeField] private Transform queueRoot;
        [SerializeField] private GameObject customerPrefab;

        [Header("Rules")]
        [SerializeField, Min(0.1f)] private float spawnInterval = 3f;
        [SerializeField, Min(1)] private int maxInScene = 5;
        [SerializeField] private Vector2 waitSecondsRange = new(10, 20);

        [Header("Pooling")]
        [SerializeField] private bool usePooling = true;
        [SerializeField, Min(0)] private int prewarm = 5;

        readonly List<Transform> _seats = new();
        readonly List<Transform> _queue = new();
        bool[] _seatOccupied;
        CustomerController[] _queueOcc;

        readonly List<CustomerController> _active = new();
        SimplePool<CustomerController> _pool;
        float _spawnAccu;

        void OnValidate() => CollectPoints();

        void Awake()
        {
            CollectPoints();

            if (usePooling)
            {
                _pool = new SimplePool<CustomerController>(
                    factory: CreateNew,
                    onGet: (c) =>
                    {
                        c.gameObject.SetActive(true);
                        if (spawnPoint) c.transform.position = spawnPoint.position;
                        // tidak perlu reset view outfit
                    },
                    onRelease: (c) => c.gameObject.SetActive(false)
                );

                for (int i = 0; i < prewarm; i++)
                {
                    var tmp = _pool.Get();
                    _pool.Release(tmp);
                }
            }
        }

        void Start() { _spawnAccu = 0f; }

        void Update()
        {
            if (!IsReadyToSpawn()) return;
            _spawnAccu += Time.deltaTime;

            while (_spawnAccu >= spawnInterval)
            {
                _spawnAccu -= spawnInterval;
                if (_active.Count >= HardCap()) break;

                int si = FindFreeSeat();
                if (si >= 0) { SpawnToSeat(si); continue; }

                int qi = FindFreeQueue();
                if (qi >= 0) { SpawnToQueue(qi); }

                if (si < 0 && qi < 0) break;
            }
        }

        bool IsReadyToSpawn()
        {
            if (!customerPrefab || !spawnPoint || !exitPoint) return false;
            return _seats.Count > 0;
        }

        int HardCap()
        {
            int logicalMax = _seats.Count + _queue.Count;
            return Mathf.Min(maxInScene, logicalMax);
        }

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

        int FindFreeSeat()
        {
            for (int i = 0; i < _seats.Count; i++)
                if (!_seatOccupied[i]) return i;
            return -1;
        }

        int FindFreeQueue()
        {
            if (_queueOcc == null || _queueOcc.Length == 0) return -1;
            for (int i = 0; i < _queueOcc.Length; i++)
                if (_queueOcc[i] == null) return i;
            return -1;
        }

        void SpawnToSeat(int seatIndex)
        {
            var c = Get();
            _active.Add(c);
            _seatOccupied[seatIndex] = true;

            float waitSec = Random.Range(waitSecondsRange.x, waitSecondsRange.y);

            c.InitSeat(
                _seats[seatIndex].position,
                exitPoint.position,
                seatIndex,
                FreeSeat,
                waitSec,
                OnDespawn
            );
        }

        void SpawnToQueue(int queueIndex)
        {
            var c = Get();
            _active.Add(c);
            _queueOcc[queueIndex] = c;

            float waitSec = Random.Range(waitSecondsRange.x, waitSecondsRange.y);

            c.InitQueue(
                _queue[queueIndex].position,
                exitPoint.position,
                queueIndex,
                FreeQueue,
                waitSec,
                OnDespawn
            );
        }

        void OnDespawn(CustomerController c) => Release(c);

        void FreeSeat(int seatIndex)
        {
            if (_seatOccupied == null) return;
            if (seatIndex < 0 || seatIndex >= _seatOccupied.Length) return;

            _seatOccupied[seatIndex] = false;

            if (_queueOcc == null) return;

            for (int qi = 0; qi < _queueOcc.Length; qi++)
            {
                var waiting = _queueOcc[qi];
                if (waiting == null) continue;

                _queueOcc[qi] = null;
                _seatOccupied[seatIndex] = true;
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

        void CollectPoints()
        {
            _seats.Clear();
            _queue.Clear();

            if (seatsRoot)
            {
                for (int i = 0; i < seatsRoot.childCount; i++)
                    _seats.Add(seatsRoot.GetChild(i));
                _seatOccupied = new bool[_seats.Count];
            }
            else _seatOccupied = System.Array.Empty<bool>();

            if (queueRoot)
            {
                for (int i = 0; i < queueRoot.childCount; i++)
                    _queue.Add(queueRoot.GetChild(i));
                _queueOcc = new CustomerController[_queue.Count];
            }
            else _queueOcc = System.Array.Empty<CustomerController>();
        }
    }
}
