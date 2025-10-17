// Assets/MMDress/Scripts/Runtime/UI/Debug/CheatMoneyButton.cs
using UnityEngine;
using UnityEngine.UI;
using MMDress.Services;

namespace MMDress.Runtime.UI.PrepShop
{
    [DisallowMultipleComponent]
    public sealed class CheatMoneyButton : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private EconomyService moneyService;
        [SerializeField] private int amount = 1000;

        void Reset()
        {
            button ??= GetComponent<Button>();
            moneyService ??= FindObjectOfType<EconomyService>(true);
        }

        void Awake()
        {
            if (button) button.onClick.AddListener(OnClickAddMoney);
        }

        public void OnClickAddMoney()
        {
            moneyService?.Add(amount); // MoneyHudView akan update via event
        }
    }
}
