// Assets/MMDress/Scripts/Runtime/UI/HUD/MoneyHudView.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MMDress.Core;

namespace MMDress.UI
{
    [DisallowMultipleComponent]
    public class MoneyHudView : MonoBehaviour
    {
        [SerializeField] private Text legacyText;
        [SerializeField] private TMP_Text tmpText;
        [SerializeField] private bool autoFind = true;

        System.Action<MoneyChanged> _onMoney;

        void Awake()
        {
            if (autoFind)
            {
                if (!legacyText) legacyText = GetComponentInChildren<Text>(true);
                if (!tmpText) tmpText = GetComponentInChildren<TMP_Text>(true);
            }
            Render(0);
        }

        void OnEnable()
        {
            _onMoney = e => Render(e.balance);
            ServiceLocator.Events.Subscribe(_onMoney);
        }
        void OnDisable()
        {
            if (_onMoney != null) ServiceLocator.Events.Unsubscribe(_onMoney);
        }

        void Render(int balance)
        {
            string s = $"Rp {balance:N0}";
            if (legacyText) legacyText.text = s;
            if (tmpText) tmpText.text = s;
        }
    }
}
