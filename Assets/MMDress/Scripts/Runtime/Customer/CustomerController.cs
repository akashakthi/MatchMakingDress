using System;
using UnityEngine;
using MMDress.Core;
using MMDress.Gameplay;

namespace MMDress.Customer
{
    [RequireComponent(typeof(Collider2D))]
    public class CustomerController : MonoBehaviour, IClickable
    {
        // ======= Config movement =======
        [Header("Movement")]
        [SerializeField] float moveSpeed = 2f;
        [SerializeField] float arriveThreshold = 0.05f;

        [Header("Waiting")]
        [SerializeField] float defaultWaitDurationSec = 15f;

        // ======= Public refs =======
        public Character.CharacterOutfitController Outfit { get; private set; }

        // ======= Internal state =======
        enum State { Idle, EnteringQueue, Queued, EnteringSeat, Waiting, Fitting, Leaving }
        State _state = State.Idle;
        Collider2D _col;

        // Seat / Queue / Exit
        Vector3 _seatPos; int _seatIndex = -1; Action<int> _onSeatFreed;
        Vector3 _queuePos; int _queueIndex = -1; Action<int> _onQueueFreed;
        Vector3 _exitPos;

        // Timer logic
        WaitTimer _timer;
        float _pendingWaitSec; // waktu yang akan dipakai saat naik seat

        // Despawn callback (pooling)
        Action<CustomerController> _onDespawn;

        // ======= Events (untuk View/UI) =======
        public event Action<CustomerController> OnWaitingStarted;
        public event Action<CustomerController, float> OnWaitProgress; // frac 1→0
        public event Action<CustomerController> OnTimedOut;
        public event Action<CustomerController> OnFittingStarted;
        public event Action<CustomerController> OnLeavingStarted;

        void Awake()
        {
            Outfit = GetComponentInChildren<Character.CharacterOutfitController>();
            _col = GetComponent<Collider2D>();
        }

        // ---------- INIT dari Spawner ----------
        public void InitSeat(Vector3 seatPos, Vector3 exitPos, int seatIndex, Action<int> onSeatFreed,
                             float waitSec, Action<CustomerController> onDespawn)
        {
            _seatPos = seatPos; _exitPos = exitPos;
            _seatIndex = seatIndex; _onSeatFreed = onSeatFreed;
            _timer = new WaitTimer(waitSec > 0 ? waitSec : defaultWaitDurationSec);
            _onDespawn = onDespawn;

            _state = State.EnteringSeat;
            if (_col) _col.enabled = true;
        }

        public void InitQueue(Vector3 queuePos, Vector3 exitPos, int queueIndex, Action<int> onQueueFreed,
                              float futureWaitSec, Action<CustomerController> onDespawn)
        {
            _queuePos = queuePos; _exitPos = exitPos;
            _queueIndex = queueIndex; _onQueueFreed = onQueueFreed;
            _pendingWaitSec = futureWaitSec > 0 ? futureWaitSec : defaultWaitDurationSec;
            _onDespawn = onDespawn;

            _state = State.EnteringQueue;
            if (_col) _col.enabled = true;
        }

        // Dipanggil spawner saat ada kursi kosong
        public void PromoteToSeat(Vector3 seatPos, int seatIndex, Action<int> onSeatFreed)
        {
            FreeQueue();
            _seatPos = seatPos; _seatIndex = seatIndex; _onSeatFreed = onSeatFreed;
            _timer = new WaitTimer(_pendingWaitSec > 0 ? _pendingWaitSec : defaultWaitDurationSec);
            _state = State.EnteringSeat;
        }

        void Update()
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
                    _timer.Tick(Time.deltaTime);
                    OnWaitProgress?.Invoke(this, _timer.Fraction);
                    if (_timer.IsDone)
                    {
                        OnTimedOut?.Invoke(this);
                        FreeSeat();
                        BeginLeaving();
                        ServiceLocator.Events?.Publish(new CustomerTimedOut(this));
                    }
                    break;

                case State.Leaving:
                    if (MoveTo(_exitPos))
                    {
                        _onDespawn?.Invoke(this);
                    }
                    break;
            }
        }

        bool MoveTo(Vector3 target)
        {
            transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
            return (transform.position - target).sqrMagnitude <= arriveThreshold * arriveThreshold;
        }

        // ========== Input ==========
        public void OnClick()
        {
            if (_state != State.Waiting) return;
            _state = State.Fitting;
            OnFittingStarted?.Invoke(this);
            ServiceLocator.Events.Publish(new CustomerSelected(this));
        }

        // Dipanggil UI saat panel Close
        public void FinishFitting()
        {
            if (_state != State.Fitting) return;
            // skor di-skip sampai mekanik ganti baju siap
            FreeSeat();
            BeginLeaving();
            ServiceLocator.Events?.Publish(new CustomerServed(this, 0));
        }

        void BeginLeaving()
        {
            _state = State.Leaving;
            if (_col) _col.enabled = false;
            OnLeavingStarted?.Invoke(this);
        }

        void FreeSeat()
        {
            if (_seatIndex >= 0) { _onSeatFreed?.Invoke(_seatIndex); _seatIndex = -1; }
        }

        void FreeQueue()
        {
            if (_queueIndex >= 0) { _onQueueFreed?.Invoke(_queueIndex); _queueIndex = -1; }
        }
    }
}
