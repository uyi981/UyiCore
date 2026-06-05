using System.Collections.Generic;
using UyiCore.Patterns;
using UnityEngine;

namespace UyiCore.Pooling
{
    /// <summary>
    /// Base singleton MonoBehaviour that owns a Dictionary&lt;id, GenericPool&gt;. Concrete subclasses
    /// supply the entry list (usually via a PoolDatabase&lt;TEntry&gt;) and optionally hook
    /// OnInstanceCreated to do per-prefab configuration.
    /// Self-type pattern: <c>class FooManager : PooledManager&lt;FooManager, FooEntry, Foo&gt;</c>.
    /// </summary>
    public abstract class PooledManager<TSelf, TEntry, TItem> : SingletonBehaviour<TSelf>
        where TSelf : PooledManager<TSelf, TEntry, TItem>
        where TEntry : class, IPoolEntry
        where TItem : Component
    {
        protected readonly Dictionary<string, GenericPool<TItem>> Pools = new Dictionary<string, GenericPool<TItem>>();

        protected abstract IReadOnlyList<TEntry> GetEntries();

        protected override void OnAwake()
        {
            base.OnAwake();
            BuildPools();
        }

        protected virtual void BuildPools()
        {
            var entries = GetEntries();
            if (entries == null)
            {
                Debug.LogError($"[{GetType().Name}] No entries provided.");
                return;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry == null || entry.Prefab == null || string.IsNullOrEmpty(entry.Id)) continue;
                var pool = new GenericPool<TItem>(entry, transform, OnInstanceCreatedInternal);
                pool.Prewarm();
                Pools[entry.Id] = pool;
            }
        }

        void OnInstanceCreatedInternal(TItem item, IPoolEntry entry)
        {
            OnInstanceCreated(item, (TEntry)entry);
        }

        /// <summary>Hook called once per newly-instantiated item, e.g. to push entry config into the component.</summary>
        protected virtual void OnInstanceCreated(TItem item, TEntry entry) { }

        public TItem Get(string id)
        {
            if (!Pools.TryGetValue(id, out var pool))
            {
                Debug.LogWarning($"[{GetType().Name}] No pool for id '{id}'.");
                return null;
            }
            return pool.Get();
        }

        public void Return(string id, TItem item)
        {
            if (item == null) return;
            if (Pools.TryGetValue(id, out var pool)) pool.Release(item);
            else Destroy(item.gameObject);
        }

        public int ActiveCount
        {
            get
            {
                int total = 0;
                foreach (var kv in Pools) total += kv.Value.ActiveCount;
                return total;
            }
        }
    }
}
