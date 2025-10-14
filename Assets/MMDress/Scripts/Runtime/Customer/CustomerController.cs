using System;
using UnityEngine;
using MMDress.Core;
using MMDress.Gameplay;
using MMDress.UI;
using MMDress.Runtime.Reputation;
using MMDress.Runtime.Integration;

namespace MMDress.Customer
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class CustomerController : MonoBehaviour, IClickable
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private float arriveThreshold = 0.05f;

        [Header("Waiting")]
        [SerializeField] private float defaultWaitDurationSec = 15f;

        [Header("Services & Bridges")]
        [SerializeField] private ReputationService reputation;                 // optional
        [SerializeField] private WaitTimerReputationBridge waitTimerBridge;    // optional

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

        // Events (untuk HUD/UI lain)
        public event Action<CustomerController> OnWaitingStarted;
        public event Action<CustomerController, float> OnWaitProgress; // frac 1→0
        public event Action<CustomerController> OnTimedOut;
        public event Action<CustomerController> OnFittingStarted;
        public event Action<CustomerController> OnLeavingStarted;

        private void Awake()
        {
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
            if (waitTimerBridge) waitTimerBridge.Bind(_timer);

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
            if (waitTimerBridge) waitTimerBridge.Bind(_timer);

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
                        OnTimedOut?.Invoke(this);
                        FreeSeat();

                        ServiceLocator.Events?.Publish(new CustomerCheckout(this, 0));
                        ServiceLocator.Events?.Publish(new CustomerTimedOut(this));

                        BeginLeaving();
                    }
                    break;

                case State.Leaving:
                    if (MoveTo(_exitPos))
                        _onDespawn?.Invoke(this);
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

        // === API dipanggil UI ===
        // Versi baru: UI langsung kasih jumlah item yang benar-benar equip (0–2).
        public void FinishFitting(int equippedCount)
        {
            int items = Mathf.Clamp(equippedCount, 0, 2);

            ServiceLocator.Events?.Publish(new CustomerCheckout(this, items));

            if (_state != State.Fitting) return;

            FreeSeat();
            BeginLeaving();

            ServiceLocator.Events?.Publish(new CustomerServed(this, 0));
        }

        // Versi lama (fallback): diasumsikan 0 item.
        public void FinishFitting() => FinishFitting(0);

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
