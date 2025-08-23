using System;
using System.Collections.Generic;
using UnityEngine;

namespace MMDress.Core
{
    public sealed class SimplePool<T> where T : Component
    {
        private readonly Queue<T> _q = new();
        private readonly Func<T> _factory;
        private readonly Action<T> _onGet, _onRelease;

        public SimplePool(Func<T> factory, Action<T> onGet = null, Action<T> onRelease = null)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _onGet = onGet; _onRelease = onRelease;
        }

        public T Get()
        {
            var t = _q.Count > 0 ? _q.Dequeue() : _factory();
            _onGet?.Invoke(t);
            return t;
        }

        public void Release(T t)
        {
            if (!t) return;
            _onRelease?.Invoke(t);
            _q.Enqueue(t);
        }
    }
}
