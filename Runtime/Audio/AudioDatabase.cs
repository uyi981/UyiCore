using System;
using System.Collections.Generic;
using UnityEngine;

namespace UyiCore.Audio
{
    [CreateAssetMenu(fileName = "AudioDatabase", menuName = "UyiCore/Audio Database")]
    public class AudioDatabase : ScriptableObject
    {
        [SerializeField] private List<AudioEntry> _sfx = new List<AudioEntry>();
        [SerializeField] private List<AudioEntry> _bgm = new List<AudioEntry>();

        public IReadOnlyList<AudioEntry> Sfx => _sfx;
        public IReadOnlyList<AudioEntry> Bgm => _bgm;

        public AudioEntry GetSfx(string id) => Find(_sfx, id);
        public AudioEntry GetBgm(string id) => Find(_bgm, id);

        static AudioEntry Find(List<AudioEntry> list, string id)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null && list[i].id == id) return list[i];
            }
            return null;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            AutoFillIds(_sfx);
            AutoFillIds(_bgm);
        }

        static void AutoFillIds(List<AudioEntry> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                var e = list[i];
                if (e == null || e.clip == null) continue;
                if (string.IsNullOrEmpty(e.id) || e.id != e.clip.name) e.id = e.clip.name;
            }
        }
#endif
    }

    [Serializable]
    public class AudioEntry
    {
        public string id;
        public AudioClip clip;

        [Range(0f, 1f)] public float volume = 1f;

        [Tooltip("Random pitch range. min == max == 1 để tắt random.")]
        public float pitchMin = 1f;
        public float pitchMax = 1f;

        [Tooltip("Loop (chủ yếu cho BGM).")]
        public bool loop = false;
    }
}
