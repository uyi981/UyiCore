using System.Collections.Generic;
using UyiCore.Patterns;
using UnityEngine;

namespace UyiCore.UI
{
    /// <summary>
    /// Quản lý popup/panel UI theo id. Pooled: Show/Hide = SetActive true/false,
    /// instance được cache lại nên GIỮ NGUYÊN state (input, scroll...) và không GC churn.
    /// Muốn giải phóng bộ nhớ thật sự thì gọi <see cref="Free"/> / <see cref="FreeAll"/>.
    /// </summary>
    public class PopupManager : SingletonBehaviour<PopupManager>
    {
        [SerializeField] private PopupDatabase _database;
        [SerializeField] private Transform _parent;

        // Cache mọi popup đã tạo (kể cả đang ẩn).
        private readonly Dictionary<string, GameObject> _instances = new Dictionary<string, GameObject>();

        protected override void OnAwake()
        {
            base.OnAwake();
            if (_parent == null) _parent = transform;
        }

        public GameObject Show(string id)
        {
            var go = GetOrCreate(id);
            if (go == null) return null;
            go.SetActive(true);
            go.transform.SetAsLastSibling();   // đưa lên trên cùng
            return go;
        }

        public void Hide(string id)
        {
            if (_instances.TryGetValue(id, out var go) && go != null) go.SetActive(false);
        }

        public void HideAll()
        {
            foreach (var kv in _instances)
                if (kv.Value != null) kv.Value.SetActive(false);
        }

        public bool IsOpen(string id)
        {
            return _instances.TryGetValue(id, out var go) && go != null && go.activeSelf;
        }

        /// <summary>Huỷ hẳn 1 popup (giải phóng bộ nhớ, mất state). Show sau sẽ tạo mới.</summary>
        public void Free(string id)
        {
            if (_instances.TryGetValue(id, out var go))
            {
                if (go != null) Destroy(go);
                _instances.Remove(id);
            }
        }

        /// <summary>Huỷ hẳn tất cả popup đã tạo.</summary>
        public void FreeAll()
        {
            foreach (var kv in _instances)
                if (kv.Value != null) Destroy(kv.Value);
            _instances.Clear();
        }

        // Lấy instance đã cache, chưa có thì Instantiate (ẩn sẵn, Show sẽ bật).
        GameObject GetOrCreate(string id)
        {
            if (_instances.TryGetValue(id, out var cached) && cached != null) return cached;

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

            var go = Instantiate(entry.prefab, _parent);
            go.name = entry.prefab.name;
            go.SetActive(false);
            _instances[id] = go;
            return go;
        }
    }
}
