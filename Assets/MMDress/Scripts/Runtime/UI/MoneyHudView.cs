using UnityEngine;
using UnityEngine.UI;
using MMDress.Core;
using MMDress.UI;

namespace MMDress.UI
{
    [DisallowMultipleComponent]
    [AddComponentMenu("MMDress/UI/Money HUD View")]
    public class MoneyHudView : MonoBehaviour
    {
        [SerializeField] private Text moneyText;
        [SerializeField] private bool autoFind = true;

        System.Action<MoneyChanged> _onMoney;

        void Awake()
        {
            if (autoFind)
                moneyText ??= GetComponentInChildren<Text>(true);

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
            if (moneyText) moneyText.text = $"$ {balance}";
        }
    }
}
