using UnityEngine;
using MMDress.Runtime.Reputation;
using MMDress.Customer;

namespace MMDress.Runtime.Integration
{
    [DisallowMultipleComponent]
    public sealed class WaitTimerReputationBridge : MonoBehaviour
    {
        [Header("Reference")]
        [SerializeField] private ReputationService reputation;
        [SerializeField] private bool autoFindReputation = true;

        [Header("Fallback")]
        [SerializeField] private float fallbackSpeedFactor = 1f;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLog = true;

        private WaitTimer _timer;
        private bool _subscribed;

        private void Awake()
        {
            ResolveReputationReference();
        }

        private void OnEnable()
        {
            ResolveReputationReference();
            SubscribeIfPossible();
        }

        private void OnDisable()
        {
            UnsubscribeIfNeeded();
        }

        public void Bind(WaitTimer timer)
        {
            _timer = timer;

            ResolveReputationReference();

            if (_timer == null)
            {
                if (enableDebugLog)
                {
                    Debug.LogWarning(
                        $"[WaitTimerReputationBridge:{name}] Bind failed | timer is NULL",
                        this);
                }
                return;
            }

            float factor = fallbackSpeedFactor;
            if (reputation != null)
                factor = reputation.CurrentSpeedFactor;

            _timer.SetExternalSpeedFactor(factor);

            if (enableDebugLog)
            {
                Debug.Log(
                    $"[WaitTimerReputationBridge:{name}] Bind | rep={(reputation ? reputation.name : "NULL")} | stage={(reputation ? reputation.Stage.ToString() : "-")} | speedFactor={factor}",
                    this);
            }
        }

        private void ResolveReputationReference()
        {
            if (reputation != null)
                return;

            if (!autoFindReputation)
                return;

#if UNITY_2023_1_OR_NEWER
            reputation = FindFirstObjectByType<ReputationService>(FindObjectsInactive.Include);
#else
            reputation = FindObjectOfType<ReputationService>(true);
#endif

            if (enableDebugLog)
            {
                if (reputation != null)
                {
                    Debug.Log(
                        $"[WaitTimerReputationBridge:{name}] Auto-found ReputationService -> {reputation.name}",
                        this);
                }
                else
                {
                    Debug.LogWarning(
                        $"[WaitTimerReputationBridge:{name}] ResolveReputationReference failed | ReputationService not found in scene",
                        this);
                }
            }
        }

        private void SubscribeIfPossible()
        {
            if (reputation == null || _subscribed)
                return;

            reputation.ReputationStageChanged += OnStageChanged;
            _subscribed = true;

            if (enableDebugLog)
            {
                Debug.Log(
                    $"[WaitTimerReputationBridge:{name}] Subscribed | stage={reputation.Stage} | speedFactor={reputation.CurrentSpeedFactor}",
                    this);
            }
        }

        private void UnsubscribeIfNeeded()
        {
            if (reputation == null || !_subscribed)
                return;

            reputation.ReputationStageChanged -= OnStageChanged;
            _subscribed = false;
        }

        private void OnStageChanged(int prev, int next, int dir)
        {
            if (_timer == null)
                return;

            float factor = fallbackSpeedFactor;
            if (reputation != null)
                factor = reputation.CurrentSpeedFactor;

            _timer.SetExternalSpeedFactor(factor);

            if (enableDebugLog)
            {
                Debug.Log(
                    $"[WaitTimerReputationBridge:{name}] OnStageChanged | prev={prev} | next={next} | dir={dir} | newSpeedFactor={factor}",
                    this);
            }
        }
    }
}