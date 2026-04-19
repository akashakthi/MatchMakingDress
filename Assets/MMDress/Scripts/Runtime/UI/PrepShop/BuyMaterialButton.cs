using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using MMDress.Services;
using MMDress.Data;

namespace MMDress.Runtime.UI.PrepShop
{
    [DisallowMultipleComponent]
    public sealed class BuyMaterialsButton : MonoBehaviour
    {
        [System.Serializable]
        public struct MaterialOrder
        {
            public MaterialSO material;
            [Min(1)] public int quantity;
        }

        [Header("Refs")]
        [SerializeField] private Button button;
        [SerializeField] private ProcurementService procurement;

        [Header("Orders (material + qty per item)")]
        [SerializeField] private List<MaterialOrder> orders = new();

        [Header("Hold Settings")]
        [SerializeField] private float holdDelay = 0.4f;
        [SerializeField] private float holdInterval = 0.15f;

        [Header("Behaviour")]
        [SerializeField] private bool continueOnFail = false;
        [SerializeField] private bool verboseLog = true;

        Coroutine _holdRoutine;

        void Reset()
        {
            button ??= GetComponent<Button>();
#if UNITY_2023_1_OR_NEWER
            procurement ??= Object.FindAnyObjectByType<ProcurementService>(FindObjectsInactive.Include);
#else
            procurement ??= FindObjectOfType<ProcurementService>(true);
#endif
        }

        void Awake()
        {
            if (!button) return;

            // klik biasa
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(BuyOnce);

            // setup hold event
            SetupHoldEvents();
        }

        // =========================
        // HOLD SETUP
        // =========================

        void SetupHoldEvents()
        {
            var trigger = button.gameObject.GetComponent<EventTrigger>();
            if (!trigger) trigger = button.gameObject.AddComponent<EventTrigger>();

            trigger.triggers.Clear();

            var down = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            down.callback.AddListener((_) => StartHold());
            trigger.triggers.Add(down);

            var up = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
            up.callback.AddListener((_) => StopHold());
            trigger.triggers.Add(up);

            var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exit.callback.AddListener((_) => StopHold());
            trigger.triggers.Add(exit);
        }

        // =========================
        // HOLD LOGIC (TURBO)
        // =========================

        void StartHold()
        {
            StopHold();
            _holdRoutine = StartCoroutine(HoldRoutine());
        }

        void StopHold()
        {
            if (_holdRoutine != null)
            {
                StopCoroutine(_holdRoutine);
                _holdRoutine = null;
            }
        }

        IEnumerator HoldRoutine()
        {
            float holdTime = 0f;

            yield return new WaitForSeconds(holdDelay);

            while (true)
            {
                holdTime += holdInterval;

                int multiplier = 1;

                // 🔥 TURBO MODE
                if (holdTime > 4f)
                    multiplier = 10;
                else if (holdTime > 2f)
                    multiplier = 5;

                for (int i = 0; i < multiplier; i++)
                {
                    BuyOnce();
                }

                yield return new WaitForSeconds(holdInterval);
            }
        }

        // =========================
        // BUY LOGIC
        // =========================

        void BuyOnce()
        {
            if (!procurement) return;

            int ok = 0, fail = 0;

            for (int i = 0; i < orders.Count; i++)
            {
                var o = orders[i];
                if (!o.material || o.quantity <= 0) continue;

                bool bought = procurement.BuyMaterial(o.material, o.quantity);
                if (bought) ok++;
                else fail++;

                if (!bought && !continueOnFail)
                {
                    if (verboseLog)
                        Debug.LogWarning($"[BuyMaterials] Gagal pada entry ke-{i} ({o.material.displayName}). Stop.");
                    break;
                }
            }

            if (verboseLog)
                Debug.Log($"[BuyMaterials] Selesai. Sukses={ok}, Gagal={fail}");
        }
    }
}