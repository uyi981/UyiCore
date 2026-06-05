using System.Collections;
using System.Collections.Generic;
using UyiCore.Patterns;
using UnityEngine;

namespace UyiCore.Audio
{
    public class AudioManager : SingletonBehaviour<AudioManager>
    {
        [SerializeField] private AudioDatabase _database;

        [Header("Mix")]
        [Range(0f, 1f)] [SerializeField] private float _masterVolume = 1f;
        [Range(0f, 1f)] [SerializeField] private float _sfxVolume = 1f;
        [Range(0f, 1f)] [SerializeField] private float _bgmVolume = 1f;

        [Header("SFX Pool")]
        [SerializeField] private int _sfxSourceCount = 12;

        [Header("BGM")]
        [SerializeField] private float _bgmCrossfade = 0.5f;

        private readonly List<AudioSource> _sfxSources = new List<AudioSource>();
        private AudioSource _bgmA;
        private AudioSource _bgmB;
        private bool _bgmUsingA = true;
        private string _currentBgmId;
        private Coroutine _crossfadeRoutine;

        protected override void OnAwake()
        {
            base.OnAwake();
            BuildSfxPool();
            BuildBgmSources();
        }

        void BuildSfxPool()
        {
            for (int i = 0; i < _sfxSourceCount; i++)
            {
                var go = new GameObject($"SFX_{i}");
                go.transform.SetParent(transform);
                var src = go.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.spatialBlend = 0f;
                _sfxSources.Add(src);
            }
        }

        void BuildBgmSources()
        {
            _bgmA = CreateBgmSource("BGM_A");
            _bgmB = CreateBgmSource("BGM_B");
        }

        AudioSource CreateBgmSource(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = true;
            src.spatialBlend = 0f;
            src.volume = 0f;
            return src;
        }

        // ---- SFX ----

        public AudioSource PlaySfx(string id)
        {
            var entry = _database != null ? _database.GetSfx(id) : null;
            if (entry == null || entry.clip == null)
            {
                Debug.LogWarning($"[AudioManager] SFX id '{id}' không tìm thấy.");
                return null;
            }

            var src = GetFreeSfxSource();
            src.clip = entry.clip;
            src.volume = entry.volume * _sfxVolume * _masterVolume;
            src.pitch = entry.pitchMin >= entry.pitchMax ? entry.pitchMin : Random.Range(entry.pitchMin, entry.pitchMax);
            src.loop = entry.loop;
            src.spatialBlend = 0f;
            src.Play();
            return src;
        }

        public AudioSource PlaySfxAt(string id, Vector3 worldPos)
        {
            var src = PlaySfx(id);
            if (src != null)
            {
                src.transform.position = worldPos;
                src.spatialBlend = 1f;
            }
            return src;
        }

        AudioSource GetFreeSfxSource()
        {
            for (int i = 0; i < _sfxSources.Count; i++)
            {
                if (!_sfxSources[i].isPlaying) return _sfxSources[i];
            }
            return _sfxSources[0];
        }

        // ---- BGM ----

        public void PlayBgm(string id)
        {
            if (_currentBgmId == id) return;

            var entry = _database != null ? _database.GetBgm(id) : null;
            if (entry == null || entry.clip == null)
            {
                Debug.LogWarning($"[AudioManager] BGM id '{id}' không tìm thấy.");
                return;
            }

            _currentBgmId = id;
            var next = _bgmUsingA ? _bgmB : _bgmA;
            var prev = _bgmUsingA ? _bgmA : _bgmB;
            _bgmUsingA = !_bgmUsingA;

            next.clip = entry.clip;
            next.loop = entry.loop;
            next.pitch = entry.pitchMin >= entry.pitchMax ? entry.pitchMin : Random.Range(entry.pitchMin, entry.pitchMax);
            next.volume = 0f;
            next.Play();

            float targetVol = entry.volume * _bgmVolume * _masterVolume;
            if (_crossfadeRoutine != null) StopCoroutine(_crossfadeRoutine);
            _crossfadeRoutine = StartCoroutine(CrossfadeRoutine(prev, next, targetVol, _bgmCrossfade));
        }

        public void StopBgm(float fade = -1f)
        {
            _currentBgmId = null;
            float t = fade < 0f ? _bgmCrossfade : fade;
            if (_crossfadeRoutine != null) StopCoroutine(_crossfadeRoutine);
            _crossfadeRoutine = StartCoroutine(FadeOutRoutine(_bgmA, t));
            StartCoroutine(FadeOutRoutine(_bgmB, t));
        }

        IEnumerator CrossfadeRoutine(AudioSource from, AudioSource to, float toVolume, float duration)
        {
            float fromStart = from.volume;
            if (duration <= 0f)
            {
                from.Stop(); from.volume = 0f;
                to.volume = toVolume;
                yield break;
            }
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / duration);
                from.volume = Mathf.Lerp(fromStart, 0f, k);
                to.volume = Mathf.Lerp(0f, toVolume, k);
                yield return null;
            }
            from.Stop();
            from.volume = 0f;
            to.volume = toVolume;
            _crossfadeRoutine = null;
        }

        IEnumerator FadeOutRoutine(AudioSource src, float duration)
        {
            if (!src.isPlaying) yield break;
            float start = src.volume;
            if (duration <= 0f) { src.Stop(); src.volume = 0f; yield break; }
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                src.volume = Mathf.Lerp(start, 0f, t / duration);
                yield return null;
            }
            src.Stop();
            src.volume = 0f;
        }

        // ---- Volume control ----

        public void SetMasterVolume(float v) { _masterVolume = Mathf.Clamp01(v); RefreshBgmVolume(); }
        public void SetSfxVolume(float v) { _sfxVolume = Mathf.Clamp01(v); }
        public void SetBgmVolume(float v) { _bgmVolume = Mathf.Clamp01(v); RefreshBgmVolume(); }

        void RefreshBgmVolume()
        {
            if (string.IsNullOrEmpty(_currentBgmId)) return;
            var entry = _database.GetBgm(_currentBgmId);
            if (entry == null) return;
            float target = entry.volume * _bgmVolume * _masterVolume;
            var active = _bgmUsingA ? _bgmB : _bgmA;
            if (active.isPlaying) active.volume = target;
        }
    }
}
