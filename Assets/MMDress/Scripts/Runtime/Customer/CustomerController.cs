using System;
using UnityEngine;
using MMDress.Core;
using MMDress.Gameplay;
using MMDress.UI;

using MMDress.Customer;                    // WaitTimer (class biasa)
using MMDress.Runtime.Reputation;         // ReputationService
using MMDress.Runtime.Integration;        // WaitTimerReputationBridge

namespace MMDress.Customer
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class CustomerController : MonoBehaviour, IClickable
    {
        // ======= Config movement =======
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private float arriveThreshold = 0.05f;

        [Header("Waiting")]
        [SerializeField] private float defaultWaitDurationSec = 15f;

        // ======= Services / Bridges =======
        [Header("Services & Bridges")]
        [SerializeField] private ReputationService reputation;                 // drag dari [_Services]
        [SerializeField] private WaitTimerReputationBridge waitTimerBridge;    // tempel komponen ini di prefab customer

        // ======= Public refs =======
        public Character.CharacterOutfitController Outfit { get; private set; }

        // ======= Internal state =======
        private enum State { Idle, EnteringQueue, Queued, EnteringSeat, Waiting, Fitting, Leaving }
        private State _state = State.Idle;
        private Collider2D _col;

        // Seat / Queue / Exit
        private Vector3 _seatPos; private int _seatIndex = -1; private Action<int> _onSeatFreed;
        private Vector3 _queuePos; private int _queueIndex = -1; private Action<int> _onQueueFreed;
        private Vector3 _exitPos;

        // Timer logic
        private WaitTimer _timer;
        private float _pendingWaitSec; // waktu dasar saat nanti naik seat

        // Despawn callback (pooling)
        private Action<CustomerController> _onDespawn;

        // ======= Events (untuk View/UI) =======
        public event Action<CustomerController> OnWaitingStarted;
        public event Action<CustomerController, float> OnWaitProgress; // frac 1→0
        public event Action<CustomerController> OnTimedOut;
        public event Action<CustomerController> OnFittingStarted;
        public event Action<CustomerController> OnLeavingStarted;

        private void Awake()
        {
            Outfit = GetComponentInChildren<Character.CharacterOutfitController>();
            _col = GetComponent<Collider2D>();
        }

        // ---------- INIT dari Spawner ----------
        public void InitSeat(
            Vector3 seatPos, Vector3 exitPos, int seatIndex, Action<int> onSeatFreed,
            float waitSec, Action<CustomerController> onDespawn)
        {
            _seatPos = seatPos;
            _exitPos = exitPos;
            _seatIndex = seatIndex;
            _onSeatFreed = onSeatFreed;
            _onDespawn = onDespawn;

            _timer = new WaitTimer(waitSec > 0 ? waitSec : defaultWaitDurationSec);
            if (waitTimerBridge) waitTimerBridge.Bind(_timer);   // ← penting: reputasi → timer

            _state = State.EnteringSeat;
            if (_col) _col.enabled = true;
        }

        public void InitQueue(
            Vector3 queuePos, Vector3 exitPos, int queueIndex, Action<int> onQueueFreed,
            float futureWaitSec, Action<CustomerController> onDespawn)
        {
            _queuePos = queuePos;
            _exitPos = exitPos;
            _queueIndex = queueIndex;
            _onQueueFreed = onQueueFreed;
            _pendingWaitSec = futureWaitSec > 0 ? futureWaitSec : defaultWaitDurationSec;
            _onDespawn = onDespawn;

            _state = State.EnteringQueue;
            if (_col) _col.enabled = true;
        }

        // Dipanggil spawner saat ada kursi kosong
        public void PromoteToSeat(Vector3 seatPos, int seatIndex, Action<int> onSeatFreed)
        {
            FreeQueue();

            _seatPos = seatPos;
            _seatIndex = seatIndex;
            _onSeatFreed = onSeatFreed;

            _timer = new WaitTimer(_pendingWaitSec > 0 ? _pendingWaitSec : defaultWaitDurationSec);
            if (waitTimerBridge) waitTimerBridge.Bind(_timer);   // ← penting

            _state = State.EnteringSeat;
        }

        private void Update()
        {
            switch (_state)
            {
                case State.EnteringQueue:
                    if (MoveTo(_queuePos)) _state = State.Queued;
                    break;

                case State.EnteringSeat:
                    if (MoveTo(_seatPos))
                    {
                        _state = State.Waiting;
                        OnWaitingStarted?.Invoke(this);
                        OnWaitProgress?.Invoke(this, 1f);
                    }
                    break;

                case State.Waiting:
                    _timer?.Tick(Time.deltaTime);
                    OnWaitProgress?.Invoke(this, _timer != null ? _timer.Fraction : 0f);

                    if (_timer != null && _timer.IsDone)
                    {
                        // Timeout → reputasi -1%
                       // reputation?.ApplyCheckout(served: false, empty: true);

                        OnTimedOut?.Invoke(this);
                        FreeSeat();

                        // broadcast kompatibel (items=0)
                        ServiceLocator.Events?.Publish(new CustomerCheckout(this, 0));
                        ServiceLocator.Events?.Publish(new CustomerTimedOut(this));

                        BeginLeaving();
                    }
                    break;

                case State.Leaving:
                    if (MoveTo(_exitPos))
                    {
                        _onDespawn?.Invoke(this); // pooling/despawn
                    }
                    break;
            }
        }

        private bool MoveTo(Vector3 target)
        {
            transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
            return (transform.position - target).sqrMagnitude <= (arriveThreshold * arriveThreshold);
        }

        // ========== Input ==========
        public void OnClick()
        {
            if (_state != State.Waiting) return;

            _state = State.Fitting;
            OnFittingStarted?.Invoke(this);
            ServiceLocator.Events?.Publish(new CustomerSelected(this));
        }

        // Dipanggil UI saat panel Close (setelah EquipAllPreview dipanggil)
        public void FinishFitting()
        {
            // Hitung berapa item yang benar-benar ter-equip
            int items = 0;
            if (Outfit != null)
            {
                if (Outfit.GetEquipped(MMDress.Data.OutfitSlot.Top) != null) items++;
                if (Outfit.GetEquipped(MMDress.Data.OutfitSlot.Bottom) != null) items++;
            }

            // Update reputasi: served = +1% jika ada item, else -1%
            bool served = items > 0;
           // reputation?.ApplyCheckout(served: served, empty: !served);

            // Broadcast untuk sistem lain (HUD/score/economy)
            ServiceLocator.Events?.Publish(new CustomerCheckout(this, items));

            if (_state != State.Fitting) return;

            FreeSeat();
            BeginLeaving();

            // (skor di-skip sesuai catatanmu)
            ServiceLocator.Events?.Publish(new CustomerServed(this, 0));
        }

        private void BeginLeaving()
        {
            _state = State.Leaving;
            if (_col) _col.enabled = false;
            OnLeavingStarted?.Invoke(this);
        }

        private void FreeSeat()
        {
            if (_seatIndex >= 0)
            {
                _onSeatFreed?.Invoke(_seatIndex);
                _seatIndex = -1;
            }
        }

        private void FreeQueue()
        {
            if (_queueIndex >= 0)
            {
                _onQueueFreed?.Invoke(_queueIndex);
                _queueIndex = -1;
            }
        }
    }
}
