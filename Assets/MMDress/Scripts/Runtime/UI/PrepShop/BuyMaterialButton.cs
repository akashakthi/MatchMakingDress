using UnityEngine;
using UnityEngine.UI;
using MMDress.Services;
using MMDress.Runtime.Inventory;

namespace MMDress.Runtime.UI.PrepShop
{
    [DisallowMultipleComponent]
    public sealed class BuyMaterialButton : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private ProcurementService procurement;
        [SerializeField] private MaterialType material = MaterialType.Cloth;
        [SerializeField, Min(1)] private int quantity = 1;

        void Reset()
        {
            button ??= GetComponent<Button>();
            procurement ??= FindObjectOfType<ProcurementService>(true);
        }

        void Awake()
        {
            if (button) button.onClick.AddListener(OnClickBuy);
        }

        void OnClickBuy()
        {
            procurement?.BuyMaterial(material, quantity);
        }
    }
}
