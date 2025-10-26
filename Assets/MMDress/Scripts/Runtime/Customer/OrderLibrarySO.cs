using System;
using System.Collections.Generic;
using UnityEngine;

namespace MMDress.Data
{
    [CreateAssetMenu(menuName = "MMDress/Orders/Order Library", fileName = "OrderLibrary")]
    public class OrderLibrarySO : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public OrderSO order;
            [Min(0)] public int weight = 1;
            [Range(1, 3)] public int minStage = 1;
            [Range(1, 3)] public int maxStage = 3;
        }

        public List<Entry> entries = new();
    }
}
