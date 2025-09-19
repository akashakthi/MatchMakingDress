using System;
using UnityEngine;

namespace MMDress.Runtime.Inventory
{
    [CreateAssetMenu(fileName = "ItemIconMap", menuName = "MMDress/Item Icon Map")]
    public sealed class ItemIconMapSO : ScriptableObject
    {
        [Serializable]
        public struct Entry { public string key; public Sprite icon; }

        [Header("Key contoh: Cloth, Thread, Top1..Top5, Bottom1..Bottom5")]
        public Entry[] entries;

        public Sprite GetIcon(string key)
        {
            if (entries == null) return null;
            for (int i = 0; i < entries.Length; i++)
                if (entries[i].key == key) return entries[i].icon;
            return null;
        }
    }
}
