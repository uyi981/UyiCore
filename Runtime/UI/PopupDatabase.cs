using System;
using System.Collections.Generic;
using UnityEngine;

namespace UyiCore.UI
{
    [CreateAssetMenu(fileName = "PopupDatabase", menuName = "UyiCore/Popup Database")]
    public class PopupDatabase : ScriptableObject
    {
        [SerializeField] private List<PopupEntry> _entries = new List<PopupEntry>();

        public IReadOnlyList<PopupEntry> Entries => _entries;

        public PopupEntry Get(string id)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].id == id) return _entries[i];
            }
            return null;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                var entry = _entries[i];
                if (entry == null || entry.prefab == null) continue;
                if (string.IsNullOrEmpty(entry.id) || entry.id != entry.prefab.name)
                {
                    entry.id = entry.prefab.name;
                }
            }
        }
#endif
    }

    [Serializable]
    public class PopupEntry
    {
        public string id;
        public GameObject prefab;
    }
}
