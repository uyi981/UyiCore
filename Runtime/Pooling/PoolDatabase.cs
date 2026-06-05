using System.Collections.Generic;
using UnityEngine;

namespace UyiCore.Pooling
{
    /// <summary>
    /// Base for ScriptableObject databases that hold pool entries. Concrete classes
    /// declare a [SerializeField] List&lt;TEntry&gt; field and expose it through Entries.
    /// </summary>
    public abstract class PoolDatabase<TEntry> : ScriptableObject where TEntry : class, IPoolEntry
    {
        public abstract IReadOnlyList<TEntry> Entries { get; }

        public TEntry Get(string id)
        {
            var list = Entries;
            if (list == null) return null;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null && list[i].Id == id) return list[i];
            }
            return null;
        }
    }
}
