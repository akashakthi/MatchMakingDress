// Assets/MMDress/Scripts/Runtime/Customer/CustomerController.cs
using System;
using UnityEngine;
using DG.Tweening;
using MMDress.Core;
using MMDress.Gameplay;
using MMDress.UI;
using MMDress.Runtime.Reputation;
using MMDress.Runtime.Integration;
using MMDress.Data;
using CheckoutEvt = MMDress.Gameplay.CustomerCheckout;

namespace MMDress.Customer
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class CustomerController : MonoBehaviour, IClickable
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private float arriveThreshold = 0.05f;

        [Header("Waiting")]
        [SerializeField] private float defaultWaitDurationSec = 90f;

        [Header("Despawn FX")]
        [SerializeField] private float despawnDuration = 0.25f;
        [SerializeField] private Ease despawnEase = Ease.InBack;

        [Header("Walk FX")]
        [Tooltip("Besaran kedut saat berjalan (1 = tidak kedut).")]
        [SerializeField] private float walkSquashY = 0.9f;
        [SerializeField] private float walkSquashDuration = 0.15f;

        [Header("Audio")]
        [Tooltip("AudioSource untuk memainkan SFX customer (optional).")]
        [SerializeField] private AudioSource audioSource;
        [Tooltip("SFX saat customer diklik (masuk fitting room).")]
        [SerializeField] private AudioClip clickSfx;
        [Tooltip("SFX saat customer selesai dilayani dengan outfit benar.")]
        [SerializeField] private AudioClip servedCorrectSfx;

        [Header("Order (Requested Outfit)")]
        [SerializeField] private ItemSO requestedTop;
        [SerializeField] private ItemSO requestedBottom;

        public ItemSO RequestedTop => requestedTop;
        public ItemSO RequestedBottom => requestedBottom;

        [Header("Services & Bridges")]
        [SerializeField] private ReputationService reputation;                 // optional
        [SerializeField] private WaitTimerReputationBridge waitTimerBridge;    // optional

        private enum State
        {
            Idle,
            PathToQueue,
            EnteringQueue,
            Queued,
            PathToSeat,
            EnteringSeat,
            Waiting,
            Fitting,
            Leaving
        }

        private State _state = State.Idle;
        private Collider2D _col;

        // Seat / Queue
        private Vector3 _seatPos; private int _seatIndex = -1; private Action<int> _onSeatFreed;
        private Vector3 _queuePos; private int _queueIndex = -1; private Action<int> _onQueueFreed;

        // Path (dari pintu ke area antre/duduk)
        private Vector3[] _path;
        private int _pathIndex;

        // Timer logic
        private WaitTimer _timer;
        private float _pendingWaitSec;   // waktu dasar saat nanti naik seat (dari queue)
        private bool _timeoutHandled;    // supaya timeout cuma sekali

        // Despawn callback (pooling)
        private Action<CustomerController> _onDespawn;

        // FX (scale + tween)
        private Vector3 _originalScale;
        private Tween _despawnTween;
        private Tween _walkTween;
        private bool _wasWalking;

        // Events (untuk HUD/UI lain)
        public event Action<CustomerController> OnWaitingStarted;
        public event Action<CustomerController, float> OnWaitProgress; // frac 1→0
        public event Action<CustomerController> OnTimedOut;
        public event Action<CustomerController> OnFittingStarted;
        public event Action<CustomerController> OnLeavingStarted;

        // helper untuk efek kedut (kalau mau dipakai UI)
        public bool IsWalking =>
            _state == State.PathToQueue ||
            _state == State.EnteringQueue ||
            _state == State.PathToSeat ||
            _state == State.EnteringSeat;

        private void Awake()
        {
            _col = GetComponent<Collider2D>();
            _originalScale = transform.localScale;

            // optional auto find AudioSource di prefab
            if (!audioSource)
                audioSource = GetComponent<AudioSource>();
        }

        private void OnDisable()
        {
            _despawnTween?.Kill();
            _walkTween?.Kill();
            _despawnTween = null;
            _walkTween = null;
            transform.localScale = _originalScale;
        }

        // ---------- INIT dari Spawner ----------

        public void InitSeat(
            Vector3 seatPos,
            int seatIndex,
            Action<int> onSeatFreed,
            float waitSec,
            Action<CustomerController> onDespawn,
            Vector3[] path = null)
        {
            _seatPos = seatPos;
            _seatIndex = seatIndex;
            _onSeatFreed = onSeatFreed;
            _onDespawn = onDespawn;

            _timeoutHandled = false;
            _timer = new WaitTimer(waitSec > 0 ? waitSec : defaultWaitDurationSec);
            if (waitTimerBridge) waitTimerBridge.Bind(_timer);

            _path = path;
            _pathIndex = 0;

            _state = (_path != null && _path.Length > 0) ? State.PathToSeat : State.EnteringSeat;

            if (_col) _col.enabled = true;
            transform.localScale = _originalScale;
        }

        public void InitQueue(
            Vector3 queuePos,
            int queueIndex,
            Action<int> onQueueFreed,
            float futureWaitSec,
            Action<CustomerController> onDespawn,
            Vector3[] path = null)
        {
            _queuePos = queuePos;
            _queueIndex = queueIndex;
            _onQueueFreed = onQueueFreed;
            _pendingWaitSec = futureWaitSec > 0 ? futureWaitSec : defaultWaitDurationSec;
            _onDespawn = onDespawn;

            _timeoutHandled = false;
            _timer = null; // timer baru akan dibuat saat duduk

            _path = path;
            _pathIndex = 0;

            _state = (_path != null && _path.Length > 0) ? State.PathToQueue : State.EnteringQueue;

            if (_col) _col.enabled = true;
            transform.localScale = _originalScale;
        }

        // Dipanggil spawner saat ada kursi kosong
        public void PromoteToSeat(Vector3 seatPos, int seatIndex, Action<int> onSeatFreed)
        {
            FreeQueue();

            _seatPos = seatPos;
            _seatIndex = seatIndex;
            _onSeatFreed = onSeatFreed;

            _timeoutHandled = false;
            _timer = new WaitTimer(_pendingWaitSec > 0 ? _pendingWaitSec : defaultWaitDurationSec);
            if (waitTimerBridge) waitTimerBridge.Bind(_timer);

            _path = null;
            _pathIndex = 0;

            _state = State.EnteringSeat;
        }

        private void Update()
        {
            switch (_state)
            {
                case State.PathToQueue:
                    MoveAlongPath(State.EnteringQueue);
                    break;

                case State.PathToSeat:
                    MoveAlongPath(State.EnteringSeat);
                    break;

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
                    TickTimerAndMaybeTimeout();
                    break;

                case State.Fitting:
                    // timer tetap berjalan saat fitting → kalau habis, dianggap timeout
                    TickTimerAndMaybeTimeout();
                    break;

                case State.Leaving:
                    // Tidak perlu MoveTo exit; tinggal tunggu tween selesai
                    break;
            }

            HandleWalkFx();
        }

        // ---------- TIMER + TIMEOUT ----------

        private void TickTimerAndMaybeTimeout()
        {
            if (_timer == null || _timeoutHandled)
                return;

            _timer.Tick(Time.deltaTime);
            OnWaitProgress?.Invoke(this, _timer.Fraction);

            if (_timer.IsDone)
            {
                HandleTimeout();
            }
        }

        private void HandleTimeout()
        {
            if (_timeoutHandled) return;
            _timeoutHandled = true;

            // beritahu listener instance (UI, dsb.)
            OnTimedOut?.Invoke(this);

            // checkout gagal (0 item, salah)
            FreeSeat();
            ServiceLocator.Events?.Publish(new CheckoutEvt(this, 0, false));
            ServiceLocator.Events?.Publish(new CustomerTimedOut(this));

            // mulai keluar (fade & despawn)
            BeginLeaving();
        }

        // ---------- WALK FX (flip + kedut) ----------

        private void HandleWalkFx()
        {
            bool walkingNow = IsWalking;
            if (walkingNow == _wasWalking) return;
            _wasWalking = walkingNow;

            if (walkingNow)
                StartWalkTween();
            else
                StopWalkTween();
        }

        private void StartWalkTween()
        {
            _walkTween?.Kill();
            transform.localScale = _originalScale;

            float targetY = _originalScale.y * walkSquashY;
            _walkTween = transform
                .DOScaleY(targetY, walkSquashDuration)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }

        private void StopWalkTween()
        {
            if (_walkTween != null)
            {
                _walkTween.Kill();
                _walkTween = null;
            }
            transform.localScale = _originalScale;
        }

        private void MoveAlongPath(State nextState)
        {
            if (_path == null || _path.Length == 0)
            {
                _state = nextState;
                return;
            }

            Vector3 target = _path[_pathIndex];
            if (MoveTo(target))
            {
                _pathIndex++;
                if (_pathIndex >= _path.Length)
                {
                    _path = null;
                    _state = nextState;
                }
            }
        }

        private bool MoveTo(Vector3 target)
        {
            Vector3 before = transform.position;

            Vector3 newPos = Vector3.MoveTowards(before, target, moveSpeed * Time.deltaTime);
            transform.position = newPos;

            // Flip kanan/kiri berdasarkan arah gerak X
            Vector3 delta = newPos - before;
            if (Mathf.Abs(delta.x) > 0.0001f)
            {
                float sign = Mathf.Sign(delta.x); // kanan = +1, kiri = -1
                var s = transform.localScale;
                s.x = Mathf.Abs(s.x) * (sign > 0 ? 1f : -1f);
                transform.localScale = s;
            }

            return (newPos - target).sqrMagnitude <= (arriveThreshold * arriveThreshold);
        }

        // ---------- AUDIO HELPER ----------

        private void PlaySfx(AudioClip clip)
        {
            if (!clip) return;

            if (audioSource != null)
                audioSource.PlayOneShot(clip);
            else
                AudioSource.PlayClipAtPoint(clip, transform.position);
        }

        // ========== Input ==========

        public void OnClick()
        {
            if (_state != State.Waiting) return;

            // SFX klik customer (buka fitting)
            PlaySfx(clickSfx);

            _state = State.Fitting;
            OnFittingStarted?.Invoke(this);
            ServiceLocator.Events?.Publish(new CustomerSelected(this));
        }

        // === API dipanggil UI ===
        public void FinishFitting(int equippedCount, bool isCorrectOrder)
        {
            int items = Mathf.Clamp(equippedCount, 0, 2);

            // kirim event ekonomi & reputasi
            ServiceLocator.Events?.Publish(new CheckoutEvt(this, items, isCorrectOrder));

            if (_state != State.Fitting) return;

            // SFX kalau berhasil (ada item & benar)
            if (items > 0 && isCorrectOrder)
            {
                PlaySfx(servedCorrectSfx);
            }

            FreeSeat();
            BeginLeaving();

            ServiceLocator.Events?.Publish(new CustomerServed(this, 0));
        }

        public void FinishFitting(int equippedCount) => FinishFitting(equippedCount, false);
        public void FinishFitting() => FinishFitting(0, false);

        private void BeginLeaving()
        {
            if (_state == State.Leaving) return;

            _state = State.Leaving;
            if (_col) _col.enabled = false;
            OnLeavingStarted?.Invoke(this);

            StopWalkTween();

            _despawnTween?.Kill();
            transform.localScale = _originalScale;

            _despawnTween = transform
                .DOScale(Vector3.zero, despawnDuration)
                .SetEase(despawnEase)
                .OnComplete(() =>
                {
                    transform.localScale = _originalScale;
                    _onDespawn?.Invoke(this);
                });
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
