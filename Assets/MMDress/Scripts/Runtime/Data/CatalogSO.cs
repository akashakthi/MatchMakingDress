using System.Collections.Generic;
using UnityEngine;

namespace MMDress.Data
{
    [CreateAssetMenu(menuName = "MMDress/Catalog")]
    public class CatalogSO : ScriptableObject
    {
        public List<ItemSO> items = new();
    }
}
