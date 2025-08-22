using System.Collections.Generic;
using UnityEngine;

namespace MMDress.Data
{
    [CreateAssetMenu(menuName = "MMDress/Inventory")]
    public class InventorySO : ScriptableObject
    {
        public List<string> ownedItemIds = new();
    }
}
