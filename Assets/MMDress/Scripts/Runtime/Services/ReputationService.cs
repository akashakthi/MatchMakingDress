// Assets/MMDress/Scripts/Runtime/Services/ReputationService.cs
using UnityEngine;
using MMDress.Core;

namespace MMDress.Services
{
    [DisallowMultipleComponent]
    public sealed class ReputationService : MonoBehaviour
    {
        const string Key = "rep_value";
        [SerializeField] private int value = 0;
        public int Value => value;

        void Awake()
        {
            if (PlayerPrefs.HasKey(Key)) value = PlayerPrefs.GetInt(Key, value);
        }

        public void Add(int delta)
        {
            if (delta == 0) return;
            value += delta;
            PlayerPrefs.SetInt(Key, value);
            PlayerPrefs.Save();
            // optionally publish event kalau HUD-mu butuh
            // ServiceLocator.Events?.Publish(new ReputationChanged(value));
        }
    }
}
