using System;
using System.Collections.Generic;
using UnityEngine;

namespace UyiCore.Pooling
{
    /// <summary>
    /// Pool for a single entry. Idle queue + active list with oldest-eviction
    /// when MaxConcurrent is reached. Callers control transform setup after Get().
    /// </summary>
    public class GenericPool<TItem> where TItem : Component
    {
        private readonly IPoolEntry _entry;
        private readonly Transform _parent;
        private readonly Action<TItem, IPoolEntry> _onCreate;
        private readonly Queue<TItem> _idle = new Queue<TItem>();
        private readonly LinkedList<TItem> _active = new LinkedList<TItem>();

        public IPoolEntry Entry => _entry;
        public int ActiveCount => _active.Count;
        public int IdleCount => _idle.Count;

        public GenericPool(IPoolEntry entry, Transform parent, Action<TItem, IPoolEntry> onCreate = null)
        {
            _entry = entry;
            _parent = parent;
            _onCreate = onCreate;
        }

        public void Prewarm()
        {
            for (int i = 0; i < _entry.PrewarmCount; i++)
            {
                var inst = CreateInstance();
                inst.gameObject.SetActive(false);
                _idle.Enqueue(inst);
            }
        }

        /// <summary>Acquire an item. Active + enabled, but caller sets transform/state.</summary>
        public TItem Get()
        {
            if (_active.Count >= _entry.MaxConcurrent)
            {
                var oldest = _active.First.Value;
                _active.RemoveFirst();
                ReturnToIdle(oldest);
            }

            TItem item = _idle.Count > 0 ? _idle.Dequeue() : CreateInstance();
            item.gameObject.SetActive(true);
            _active.AddLast(item);
            if (item is IPoolable p) p.OnSpawned();
            return item;
        }

        public void Release(TItem item)
        {
            if (item == null) return;
            _active.Remove(item);
            ReturnToIdle(item);
        }

        void ReturnToIdle(TItem item)
        {
            if (item is IPoolable p) p.OnDespawned();
            item.gameObject.SetActive(false);
            _idle.Enqueue(item);
        }

        TItem CreateInstance()
        {
            var go = UnityEngine.Object.Instantiate(_entry.Prefab, _parent);
            var item = go.GetComponent<TItem>();
            if (item == null) item = go.AddComponent<TItem>();
            _onCreate?.Invoke(item, _entry);
            return item;
        }
    }
}
