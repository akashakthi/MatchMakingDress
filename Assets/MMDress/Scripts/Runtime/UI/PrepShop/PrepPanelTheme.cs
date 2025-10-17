// Assets/MMDress/Scripts/Runtime/UI/PrepShop/PrepPanelTheme.cs
using UnityEngine;

namespace MMDress.Runtime.UI.PrepShop
{
    [CreateAssetMenu(fileName = "PrepPanelTheme", menuName = "MMDress/Prep Panel Theme")]
    public sealed class PrepPanelTheme : ScriptableObject
    {
        [System.Serializable] public struct Entry { public Sprite icon; public int typeIndex; }

        public Entry[] tops = new Entry[5];
        public Entry[] bottoms = new Entry[5];

        public Entry GetTop(int i) => (i >= 0 && i < tops.Length) ? tops[i] : default;
        public Entry GetBottom(int i) => (i >= 0 && i < bottoms.Length) ? bottoms[i] : default;
    }
}
