// Assets/MMDress/Scripts/Runtime/UI/PrepShop/BuyMaterialsButton.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
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

        [Header("Behaviour")]
        [Tooltip("Jika true, tetap lanjut belanja entry berikutnya walau ada satu yang gagal.")]
        [SerializeField] private bool continueOnFail = false;

        [Tooltip("Tampilkan log ringkas hasil belanja di Console.")]
        [SerializeField] private bool verboseLog = true;

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
                    if (verboseLog) Debug.LogWarning($"[BuyMaterials] Gagal pada entry ke-{i} ({o.material.displayName}). Stop.");
                    break;
                }
            }

            if (verboseLog)
                Debug.Log($"[BuyMaterials] Selesai. Sukses={ok}, Gagal={fail}");
        }
    }
}
