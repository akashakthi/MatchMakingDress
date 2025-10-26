// Assets/MMDress/Scripts/Runtime/Data/OrderSO.cs
using UnityEngine;

namespace MMDress.Data
{
    [CreateAssetMenu(menuName = "MMDress/Order")]
    public class OrderSO : ScriptableObject
    {
        [Header("Permintaan (slot opsional)")]
        public ItemSO requiredTop;       // null = bebas
        public ItemSO requiredBottom;    // null = bebas

        [Header("Ekonomi")]
        [Min(0)] public int payout = 100;
    }
}
