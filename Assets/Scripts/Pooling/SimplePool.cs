using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arena.Pooling
{
    public class SimplePool<T> where T : Component, IPoolable
    {
        private readonly Func<T> _factory;
        private readonly Stack<T> _idle = new Stack<T>();

        public int CreatedCount { get; private set; }

        public int IdleCount => _idle.Count;

        public SimplePool(Func<T> factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        public T Get()
        {
            T item;

            if (_idle.Count > 0)
            {
                item = _idle.Pop();
            }
            else
            {
                item = _factory();
                CreatedCount++;
            }

            item.gameObject.SetActive(true);
            item.OnSpawn();
            return item;
        }

        public void Release(T item)
        {
            if (item == null)
            {
                return;
            }

            item.OnDespawn();
            item.gameObject.SetActive(false);
            _idle.Push(item);
        }
    }
}
