// Assets/MMDress/Scripts/Runtime/Data/MaterialSO.cs
using UnityEngine;

namespace MMDress.Data
{
    [CreateAssetMenu(menuName = "MMDress/Material")]
    public class MaterialSO : ScriptableObject
    {
        [Header("Identity")]
        public string id;
        public string displayName;
        public Sprite icon;

        [Header("Economy")]
        [Min(0)] public int price = 100;   // <- dipakai ProcurementService
    }
}
