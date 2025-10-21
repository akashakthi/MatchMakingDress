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

        [Header("Anchor Offset (per item)")]
        public Vector3 localPos;
        public Vector3 localScale = Vector3.one;
        public float localRotZ;

        [Header("Crafting Requirements")]
        public bool requiresMaterials = true;
        public List<MaterialCost> materialCosts = new();
        // public int price; // ❌ dihapus: baju tidak dibeli pakai uang
    }

    [Serializable]
    public class MaterialCost
    {
        public MaterialSO material;
        [Min(1)] public int qty = 1;
    }
}
