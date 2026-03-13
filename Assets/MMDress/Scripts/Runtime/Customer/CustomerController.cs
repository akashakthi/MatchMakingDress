using System;
using UnityEngine;
using DG.Tweening;
using MMDress.Core;
using MMDress.Gameplay;
using MMDress.UI;
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
        [Tooltip("Default timer dasar semua customer. Dipakai kalau spawner tidak override.")]
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
        [SerializeField] private WaitTimerReputationBridge waitTimerBridge;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLog = true;
        [SerializeField] private bool logTimerEverySecond = false;

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
        private Vector3 _seatPos;
        private int _seatIndex = -1;
        private Action<int> _onSeatFreed;

        private Vector3 _queuePos;
        private int _queueIndex = -1;
        private Action<int> _onQueueFreed;

        // Path
        private Vector3[] _path;
        private int _pathIndex;

        // Timer
        private WaitTimer _timer;
        private float _pendingWaitSec;
        private bool _timeoutHandled;
        private float _lastResolvedWaitDuration = -1f;
        private float _debugTimerLogAccumulator;

        // Despawn callback
        private Action<CustomerController> _onDespawn;

        // FX
        private Vector3 _originalScale;
        private Tween _despawnTween;
        private Tween _walkTween;
        private bool _wasWalking;

        // Events
        public event Action<CustomerController> OnWaitingStarted;
        public event Action<CustomerController, float> OnWaitProgress; // 1 -> 0
        public event Action<CustomerController> OnTimedOut;
        public event Action<CustomerController> OnFittingStarted;
        public event Action<CustomerController> OnLeavingStarted;

        public bool IsWalking =>
            _state == State.PathToQueue ||
            _state == State.EnteringQueue ||
            _state == State.PathToSeat ||
            _state == State.EnteringSeat;

        private void Awake()
        {
            _col = GetComponent<Collider2D>();
            _originalScale = transform.localScale;

            if (!audioSource)
                audioSource = GetComponent<AudioSource>();

            if (enableDebugLog)
            {
                Debug.Log(
                    $"[CustomerController:{name}] Awake | defaultWaitDurationSec={defaultWaitDurationSec}",
                    this);
            }
        }

        private void OnDisable()
        {
            _despawnTween?.Kill();
            _walkTween?.Kill();
            _despawnTween = null;
            _walkTween = null;
            transform.localScale = _originalScale;
        }

        // ---------- INIT ----------

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
            _debugTimerLogAccumulator = 0f;

            float resolvedWait = ResolveWaitDuration(waitSec);
            _lastResolvedWaitDuration = resolvedWait;

            if (enableDebugLog)
            {
                Debug.Log(
                    $"[CustomerController:{name}] InitSeat | waitSecArg={waitSec} | defaultWaitDurationSec={defaultWaitDurationSec} | resolvedWait={resolvedWait}",
                    this);
            }

            _timer = new WaitTimer(resolvedWait);

            if (waitTimerBridge)
                waitTimerBridge.Bind(_timer);

            _path = path;
            _pathIndex = 0;

            _state = (_path != null && _path.Length > 0)
                ? State.PathToSeat
                : State.EnteringSeat;

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

            _pendingWaitSec = ResolveWaitDuration(futureWaitSec);
            _onDespawn = onDespawn;

            _timeoutHandled = false;
            _debugTimerLogAccumulator = 0f;
            _timer = null;

            if (enableDebugLog)
            {
                Debug.Log(
                    $"[CustomerController:{name}] InitQueue | futureWaitSecArg={futureWaitSec} | defaultWaitDurationSec={defaultWaitDurationSec} | pendingWaitSec={_pendingWaitSec}",
                    this);
            }

            _path = path;
            _pathIndex = 0;

            _state = (_path != null && _path.Length > 0)
                ? State.PathToQueue
                : State.EnteringQueue;

            if (_col) _col.enabled = true;
            transform.localScale = _originalScale;
        }

        public void PromoteToSeat(Vector3 seatPos, int seatIndex, Action<int> onSeatFreed)
        {
            FreeQueue();

            _seatPos = seatPos;
            _seatIndex = seatIndex;
            _onSeatFreed = onSeatFreed;

            _timeoutHandled = false;
            _debugTimerLogAccumulator = 0f;

            float resolvedWait = ResolveWaitDuration(_pendingWaitSec);
            _lastResolvedWaitDuration = resolvedWait;

            if (enableDebugLog)
            {
                Debug.Log(
                    $"[CustomerController:{name}] PromoteToSeat | pendingWaitSec={_pendingWaitSec} | defaultWaitDurationSec={defaultWaitDurationSec} | resolvedWait={resolvedWait}",
                    this);
            }

            _timer = new WaitTimer(resolvedWait);

            if (waitTimerBridge)
                waitTimerBridge.Bind(_timer);

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
                    if (MoveTo(_queuePos))
                        _state = State.Queued;
                    break;

                case State.EnteringSeat:
                    if (MoveTo(_seatPos))
                    {
                        _state = State.Waiting;

                        if (enableDebugLog)
                        {
                            Debug.Log(
                                $"[CustomerController:{name}] Enter Waiting | resolvedWait={_lastResolvedWaitDuration}",
                                this);
                        }

                        OnWaitingStarted?.Invoke(this);
                        OnWaitProgress?.Invoke(this, 1f);
                    }
                    break;

                case State.Waiting:
                    TickTimerAndMaybeTimeout();
                    break;

                case State.Fitting:
                    // timer tetap jalan saat fitting
                    TickTimerAndMaybeTimeout();
                    break;

                case State.Leaving:
                    break;
            }

            HandleWalkFx();
        }

        // ---------- TIMER ----------

        private float ResolveWaitDuration(float incoming)
        {
            return incoming > 0f ? incoming : defaultWaitDurationSec;
        }

        private void TickTimerAndMaybeTimeout()
        {
            if (_timer == null || _timeoutHandled)
                return;

            _timer.Tick(Time.deltaTime);
            OnWaitProgress?.Invoke(this, _timer.Fraction);

            if (enableDebugLog && logTimerEverySecond)
            {
                _debugTimerLogAccumulator += Time.deltaTime;
                if (_debugTimerLogAccumulator >= 1f)
                {
                    _debugTimerLogAccumulator = 0f;
                    Debug.Log(
                        $"[CustomerController:{name}] TickTimer | state={_state} | fraction={_timer.Fraction:0.000} | deltaTime={Time.deltaTime:0.000}",
                        this);
                }
            }

            if (_timer.IsDone)
            {
                if (enableDebugLog)
                {
                    Debug.Log(
                        $"[CustomerController:{name}] Timer DONE | state={_state} | resolvedWait={_lastResolvedWaitDuration}",
                        this);
                }

                HandleTimeout();
            }
        }

        private void HandleTimeout()
        {
            if (_timeoutHandled) return;
            _timeoutHandled = true;

            if (enableDebugLog)
            {
                Debug.Log(
                    $"[CustomerController:{name}] HandleTimeout triggered",
                    this);
            }

            OnTimedOut?.Invoke(this);

            FreeSeat();
            ServiceLocator.Events?.Publish(new CheckoutEvt(this, 0, false));
            ServiceLocator.Events?.Publish(new CustomerTimedOut(this));

            BeginLeaving();
        }

        // ---------- WALK FX ----------

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

            Vector3 delta = newPos - before;
            if (Mathf.Abs(delta.x) > 0.0001f)
            {
                float sign = Mathf.Sign(delta.x);
                var s = transform.localScale;
                s.x = Mathf.Abs(s.x) * (sign > 0 ? 1f : -1f);
                transform.localScale = s;
            }

            return (newPos - target).sqrMagnitude <= (arriveThreshold * arriveThreshold);
        }

        // ---------- AUDIO ----------

        private void PlaySfx(AudioClip clip)
        {
            if (!clip) return;

            if (audioSource != null)
                audioSource.PlayOneShot(clip);
            else
                AudioSource.PlayClipAtPoint(clip, transform.position);
        }

        // ---------- INPUT ----------

        public void OnClick()
        {
            if (_state != State.Waiting) return;

            if (enableDebugLog)
            {
                Debug.Log(
                    $"[CustomerController:{name}] Clicked -> enter Fitting",
                    this);
            }

            PlaySfx(clickSfx);

            _state = State.Fitting;
            OnFittingStarted?.Invoke(this);
            ServiceLocator.Events?.Publish(new CustomerSelected(this));
        }

        // ---------- FITTING RESULT ----------

        public void FinishFitting(int equippedCount, bool isCorrectOrder)
        {
            int items = Mathf.Clamp(equippedCount, 0, 2);

            if (enableDebugLog)
            {
                Debug.Log(
                    $"[CustomerController:{name}] FinishFitting | items={items} | isCorrectOrder={isCorrectOrder}",
                    this);
            }

            ServiceLocator.Events?.Publish(new CheckoutEvt(this, items, isCorrectOrder));

            if (_state != State.Fitting) return;

            if (items > 0 && isCorrectOrder)
                PlaySfx(servedCorrectSfx);

            FreeSeat();
            BeginLeaving();

            ServiceLocator.Events?.Publish(new CustomerServed(this, 0));
        }

        public void FinishFitting(int equippedCount) => FinishFitting(equippedCount, false);
        public void FinishFitting() => FinishFitting(0, false);

        private void BeginLeaving()
        {
            if (_state == State.Leaving) return;

            if (enableDebugLog)
            {
                Debug.Log(
                    $"[CustomerController:{name}] BeginLeaving",
                    this);
            }

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