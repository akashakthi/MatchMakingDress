// Assets/MMDress/Scripts/Runtime/UI/PrepShop/CheatMoneyButton.cs
using UnityEngine;
using UnityEngine.UI;
using MMDress.Services;

namespace MMDress.Runtime.UI.PrepShop
{
    [DisallowMultipleComponent]
    public sealed class CheatMoneyButton : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private EconomyService economy;
        [SerializeField, Min(1)] private int amount = 1000;

        void Reset()
        {
#if UNITY_2023_1_OR_NEWER
            button ??= GetComponent<Button>();
            economy ??= Object.FindAnyObjectByType<EconomyService>(FindObjectsInactive.Include);
#else
            button ??= GetComponent<Button>();
            economy ??= FindObjectOfType<EconomyService>(true);
#endif
        }

        void Awake()
        {
            if (button) button.onClick.AddListener(() =>    
            {
                if (!economy) return;
                economy.Add(amount); // <- ini yang mem-publish MoneyChanged
                Debug.Log($"[CheatMoney] +{amount}, balance={economy.Balance}");
            });
        }
    }
}
