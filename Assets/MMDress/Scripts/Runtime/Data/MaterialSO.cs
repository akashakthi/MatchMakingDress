using UnityEngine;

namespace MMDress.Data
{
    [CreateAssetMenu(menuName = "MMDress/Material")]
    public class MaterialSO : ScriptableObject
    {
        public string id;
        public string displayName;
        public Sprite icon;
    }
}
