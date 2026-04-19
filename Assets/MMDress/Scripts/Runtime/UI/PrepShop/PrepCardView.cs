using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using MMDress.Data;

namespace MMDress.Runtime.UI.PrepShop
{
    [DisallowMultipleComponent]
    public sealed class PrepCardView : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text qtyText;
        [SerializeField] private Button plusButton;
        [SerializeField] private Button minusButton;

        [Header("Manual Mapping (optional - 5 slots)")]
        [SerializeField] private ItemSO[] manualItems = new ItemSO[5];

        // Default item (biar kompatibel dengan sistem lama)
        public ItemSO Item => (manualItems != null && manualItems.Length > 0) ? manualItems[0] : null;

        public ItemSO GetItem(int index)
        {
            if (manualItems == null || index < 0 || index >= manualItems.Length)
                return null;

            return manualItems[index];
        }

        [Header("Hold Settings")]
        [SerializeField] private float holdDelay = 0.4f;
        [SerializeField] private float holdInterval = 0.1f;

        int _planned;
        Coroutine _holdRoutine;

        public event Action<PrepCardView, int> OnDelta;

        void Awake()
        {
            SetupButton(plusButton, +1);
            SetupButton(minusButton, -1);

            if (qtyText) qtyText.raycastTarget = false;
            if (iconImage) iconImage.raycastTarget = false;

            SetPlanned(0);
        }

        // =========================
        // SETUP BUTTON
        // =========================

        void SetupButton(Button btn, int delta)
        {
            if (!btn) return;

            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => OnDelta?.Invoke(this, delta));

            var trigger = btn.gameObject.GetComponent<EventTrigger>();
            if (!trigger) trigger = btn.gameObject.AddComponent<EventTrigger>();

            trigger.triggers.Clear();

            // Pointer Down
            var down = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            down.callback.AddListener((_) => StartHold(delta));
            trigger.triggers.Add(down);

            // Pointer Up
            var up = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
            up.callback.AddListener((_) => StopHold());
            trigger.triggers.Add(up);

            // Pointer Exit
            var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exit.callback.AddListener((_) => StopHold());
            trigger.triggers.Add(exit);
        }

        // =========================
        // HOLD LOGIC
        // =========================

        void StartHold(int delta)
        {
            StopHold();
            _holdRoutine = StartCoroutine(HoldRoutine(delta));
        }

        void StopHold()
        {
            if (_holdRoutine != null)
            {
                StopCoroutine(_holdRoutine);
                _holdRoutine = null;
            }
        }

        IEnumerator HoldRoutine(int delta)
        {
            yield return new WaitForSeconds(holdDelay);

            while (true)
            {
                OnDelta?.Invoke(this, delta);
                yield return new WaitForSeconds(holdInterval);
            }
        }

        // =========================
        // API
        // =========================

        public void BindItem(ItemSO item, Sprite icon = null)
        {
            if (manualItems == null || manualItems.Length == 0)
                manualItems = new ItemSO[5];

            manualItems[0] = item;

            if (icon != null) SetIcon(icon);
        }

        public void SetIcon(Sprite icon)
        {
            if (!iconImage) return;
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }

        public void SetPlanned(int qty)
        {
            _planned = Mathf.Max(0, qty);
            if (qtyText) qtyText.text = _planned.ToString();
        }
    }
}