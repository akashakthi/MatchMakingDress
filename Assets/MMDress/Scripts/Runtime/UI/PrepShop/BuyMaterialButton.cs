// Assets/MMDress/Scripts/Runtime/UI/PrepShop/BuyMaterialButton.cs
using UnityEngine;
using UnityEngine.UI;
using MMDress.Services;
using MMDress.Data;

namespace MMDress.Runtime.UI.PrepShop
{
    [DisallowMultipleComponent]
    public sealed class BuyMaterialButton : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private ProcurementService procurement;
        [SerializeField] private MaterialSO material;
        [SerializeField, Min(1)] private int quantity = 1;

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
            if (button) button.onClick.AddListener(OnClickBuy);
        }

        void OnClickBuy()
        {
            if (!procurement || !material || quantity <= 0) return;
            procurement.BuyMaterial(material, quantity);
        }
    }
}
