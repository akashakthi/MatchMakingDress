using System;
using System.Collections.Generic;

namespace MMDress.Core
{
    public interface IEventBus
    {
        void Subscribe<T>(Action<T> handler);
        void Unsubscribe<T>(Action<T> handler);
        void Publish<T>(T evt);
    }

    public sealed class SimpleEventBus : IEventBus
    {
        private readonly Dictionary<Type, List<Delegate>> _map = new();

        public void Subscribe<T>(Action<T> handler)
        {
            var t = typeof(T);
            if (!_map.TryGetValue(t, out var list))
            {
                list = new List<Delegate>();
                _map[t] = list;
            }
            list.Add(handler);
        }

        public void Unsubscribe<T>(Action<T> handler)
        {
            var t = typeof(T);
            if (_map.TryGetValue(t, out var list)) list.Remove(handler);
        }

        public void Publish<T>(T evt)
        {
            var t = typeof(T);
            if (_map.TryGetValue(t, out var list))
            {
                var copy = list.ToArray(); // aman jika handler mengubah list
                for (int i = 0; i < copy.Length; i++)
                    ((Action<T>)copy[i])?.Invoke(evt);
            }
        }
    }

    public interface IClickable { void OnClick(); }
}
