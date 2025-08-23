// Assets/MMDress/Scripts/Runtime/Customer/CustomerSpawner.cs
using System.Collections.Generic;
using UnityEngine;

namespace MMDress.Customer
{
    public class CustomerSpawner : MonoBehaviour
    {
        [Header("Points")]
        [SerializeField] Transform spawnPoint;
        [SerializeField] Transform exitPoint;
        [SerializeField] Transform seatsRoot;          // parent yg berisi children seat
        [SerializeField] GameObject customerPrefab;

        [Header("Spawn Rules")]
        [SerializeField] float spawnInterval = 3f;     // jeda antar attempt spawn
        [SerializeField] int maxInScene = 5;      // batas maksimum customer aktif
        [SerializeField] Vector2 waitSecondsRange = new Vector2(10, 20);

        float _timer;
        List<Transform> _seats = new();
        bool[] _occupied;
        readonly List<CustomerController> _active = new();

        void Awake()
        {
            // kumpulkan seat dari children seatsRoot
            if (seatsRoot)
            {
                _seats.Clear();
                for (int i = 0; i < seatsRoot.childCount; i++)
                    _seats.Add(seatsRoot.GetChild(i));
                _occupied = new bool[_seats.Count];
            }
        }

        void Start()
        {
            // spawn pertama langsung dicoba
            _timer = 0f;
        }

        void Update()
        {
            if (!customerPrefab || !spawnPoint || !exitPoint || _seats.Count == 0) return;

            _timer -= Time.deltaTime;
            if (_timer > 0f) return;

            _timer = spawnInterval;      // reset timer

            // cek kapasitas
            if (_active.Count >= Mathf.Min(maxInScene, _seats.Count)) return;

            // cari seat kosong
            int seatIndex = FindFreeSeat();
            if (seatIndex < 0) return;

            SpawnAtSeat(seatIndex);
        }

        int FindFreeSeat()
        {
            for (int i = 0; i < _seats.Count; i++)
                if (!_occupied[i]) return i;
            return -1;
        }

        void SpawnAtSeat(int seatIndex)
        {
            var go = Instantiate(customerPrefab, spawnPoint.position, Quaternion.identity);
            var c = go.GetComponent<CustomerController>();

            _occupied[seatIndex] = true;
            _active.Add(c);

            // durasi tunggu acak dalam range
            float waitSec = Random.Range(waitSecondsRange.x, waitSecondsRange.y);

            // callback untuk membebaskan seat
            c.Init(_seats[seatIndex].position, exitPoint.position, seatIndex, FreeSeat, waitSec);

            // cleanup saat destroy
            go.AddComponent<OnDestroyCallback>().Init(() =>
            {
                _active.Remove(c);
            });
        }

        void FreeSeat(int seatIndex)
        {
            if (seatIndex >= 0 && seatIndex < _occupied.Length)
                _occupied[seatIndex] = false;
        }

        // helper mini untuk cleanup list saat object Destroy
        class OnDestroyCallback : MonoBehaviour
        {
            System.Action _cb;
            public void Init(System.Action cb) => _cb = cb;
            void OnDestroy() => _cb?.Invoke();
        }
    }
}
