using System.Collections.Generic;
using UyiCore.Patterns;
using UnityEngine;

namespace UyiCore.UI
{
    public class PopupManager : SingletonBehaviour<PopupManager>
    {
        [SerializeField] private PopupDatabase _database;
        [SerializeField] private Transform _parent;

        private readonly Dictionary<string, GameObject> _active = new Dictionary<string, GameObject>();

        protected override void OnAwake()
        {
            base.OnAwake();
            if (_parent == null) _parent = transform;
        }

        public GameObject Show(string id)
        {
            if (_database == null)
            {
                Debug.LogError("[PopupManager] Database chưa được gán.");
                return null;
            }

            var entry = _database.Get(id);
            if (entry == null || entry.prefab == null)
            {
                Debug.LogWarning($"[PopupManager] Không tìm thấy popup id '{id}'.");
                return null;
            }

            if (_active.TryGetValue(id, out var existing) && existing != null)
            {
                existing.SetActive(true);
                existing.transform.SetAsLastSibling();
                return existing;
            }

            var go = Instantiate(entry.prefab, _parent);
            go.name = entry.prefab.name;
            _active[id] = go;
            return go;
        }

        public void Hide(string id)
        {
            if (_active.TryGetValue(id, out var go))
            {
                if (go != null) Destroy(go);
                _active.Remove(id);
            }
        }

        public void HideAll()
        {
            foreach (var kv in _active)
            {
                if (kv.Value != null) Destroy(kv.Value);
            }
            _active.Clear();
        }

        public bool IsOpen(string id)
        {
            return _active.TryGetValue(id, out var go) && go != null && go.activeInHierarchy;
        }
    }
}
