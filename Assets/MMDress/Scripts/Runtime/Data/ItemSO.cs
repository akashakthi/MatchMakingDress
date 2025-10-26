// Assets/MMDress/Scripts/Data/ItemSO.cs
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MMDress.Data
{
    public enum OutfitSlot { Top, Bottom }

    [CreateAssetMenu(menuName = "MMDress/Item")]
    public class ItemSO : ScriptableObject
    {
        [Header("Identity")]
        public string id;
        public string displayName;

        [Header("Slot & Visual")]
        public OutfitSlot slot;
        public Sprite sprite;

        [Header("Crafting Requirements")]
        public bool requiresMaterials = true;
        public List<MaterialCost> materialCosts = new();
    }

    [Serializable]
    public class MaterialCost
    {
        public MaterialSO material;
        [Min(1)] public int qty = 1;
    }
}
