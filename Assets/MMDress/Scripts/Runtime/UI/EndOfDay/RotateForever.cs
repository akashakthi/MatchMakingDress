// Assets/MMDress/Scripts/Runtime/UI/EndOfDay/RotateForever.cs
using UnityEngine;

namespace MMDress.Runtime.UI.EndOfDay
{
    [DisallowMultipleComponent]
    public sealed class RotateForever : MonoBehaviour
    {
        [SerializeField] private float speed = 20f;
        [SerializeField] private bool useUnscaledTime = true;

        void Update()
        {
            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            transform.Rotate(0f, 0f, speed * dt);
        }
    }
}
