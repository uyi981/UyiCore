using System.Collections.Generic;
using UnityEngine;

namespace UyiCore.BT
{
    /// <summary>
    /// Typed key-value store cho behavior tree. Node chia sẻ state qua đây
    /// thay vì nhồi vào owner class. Không thread-safe.
    /// </summary>
    public class Blackboard
    {
        private readonly Dictionary<string, object> _data = new Dictionary<string, object>();

        public void Set<T>(string key, T value) => _data[key] = value;

        public T Get<T>(string key)
        {
            if (_data.TryGetValue(key, out var v) && v is T t) return t;
            return default;
        }

        public bool TryGet<T>(string key, out T value)
        {
            if (_data.TryGetValue(key, out var v) && v is T t)
            {
                value = t;
                return true;
            }
            value = default;
            return false;
        }

        public bool Has(string key) => _data.ContainsKey(key);
        public bool Remove(string key) => _data.Remove(key);
        public void Clear() => _data.Clear();
        public int Count => _data.Count;
    }
}
