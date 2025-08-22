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
        public Sprite sprite;                 // MVP: anchor-based

        [Header("Anchor Offset (per item)")]
        public Vector3 localPos;
        public Vector3 localScale = Vector3.one;
        public float localRotZ;

        [Header("Economy Hooks (next sprint)")]
        public bool requiresMaterials;
        public List<MaterialCost> materialCosts;
        public int price;
    }

    [Serializable]
    public class MaterialCost
    {
        public MaterialSO material;
        public int qty = 1;
    }
}
