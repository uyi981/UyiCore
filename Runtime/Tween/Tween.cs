using System;
using System.Collections.Generic;
using UnityEngine;

namespace UyiCore.Tweening
{
    /// <summary>Token 1 tween. Cancel / Complete / check IsActive.</summary>
    public readonly struct TweenHandle : IEquatable<TweenHandle>
    {
        internal readonly int Id;
        internal TweenHandle(int id) { Id = id; }
        public bool IsActive => Tween.IsActive(this);
        public void Cancel() => Tween.Cancel(this);
        public void Complete() => Tween.Complete(this);
        public bool Equals(TweenHandle other) => Id == other.Id;
        public override bool Equals(object obj) => obj is TweenHandle h && Equals(h);
        public override int GetHashCode() => Id;
        public static bool operator ==(TweenHandle a, TweenHandle b) => a.Id == b.Id;
        public static bool operator !=(TweenHandle a, TweenHandle b) => a.Id != b.Id;
    }

    /// <summary>
    /// Tween engine tối giản, generic. Static API + runner tự spawn (kiểu Timer).
    /// <see cref="To"/> gọi <c>onUpdate(easedT)</c> mỗi frame (easedT có thể &gt;1 với ease overshoot →
    /// dùng LerpUnclamped). Tự cancel khi <c>owner</c> bị destroy. Không loop (one-shot).
    /// UI helper (<see cref="UITween"/>) build trên đây; đừng khoá cứng vào UI.
    /// </summary>
    public static class Tween
    {
        class Entry
        {
            public int id;
            public float elapsed, duration, delay;
            public Ease ease;
            public bool unscaled, watchOwner, cancelled;
            public UnityEngine.Object owner;
            public Action<float> onUpdate;
            public Action onComplete;
        }

        private static readonly List<Entry> _entries = new List<Entry>();
        private static int _nextId = 1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _entries.Clear();
            _nextId = 1;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void EnsureRunner()
        {
            if (TweenRunner.Instance != null) return;
            var go = new GameObject("[TweenRunner]");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<TweenRunner>();
        }

        /// <summary>Tween tổng quát: mỗi frame gọi onUpdate(easedT). Trả handle để cancel.</summary>
        public static TweenHandle To(float duration, Action<float> onUpdate, Ease ease = Ease.OutQuad,
            float delay = 0f, bool unscaled = false, UnityEngine.Object owner = null, Action onComplete = null)
        {
            if (onUpdate == null) return default;
            if (duration <= 0f && delay <= 0f)
            {
                Safe(() => onUpdate(Easing.Evaluate(ease, 1f)));
                Safe(onComplete);
                return default;
            }
            var e = new Entry
            {
                id = _nextId++,
                duration = Mathf.Max(0f, duration),
                delay = Mathf.Max(0f, delay),
                ease = ease,
                unscaled = unscaled,
                watchOwner = owner != null,
                owner = owner,
                onUpdate = onUpdate,
                onComplete = onComplete,
            };
            _entries.Add(e);
            return new TweenHandle(e.id);
        }

        public static bool Cancel(TweenHandle h)
        {
            if (h.Id == 0) return false;
            for (int i = 0; i < _entries.Count; i++)
                if (_entries[i].id == h.Id) { _entries[i].cancelled = true; return true; }
            return false;
        }

        public static bool IsActive(TweenHandle h)
        {
            if (h.Id == 0) return false;
            for (int i = 0; i < _entries.Count; i++)
                if (_entries[i].id == h.Id) return !_entries[i].cancelled;
            return false;
        }

        /// <summary>Nhảy tween tới cuối ngay (set eased(1) + onComplete) rồi kết thúc.</summary>
        public static void Complete(TweenHandle h)
        {
            if (h.Id == 0) return;
            for (int i = 0; i < _entries.Count; i++)
            {
                var e = _entries[i];
                if (e.id != h.Id || e.cancelled) continue;
                Safe(() => e.onUpdate(Easing.Evaluate(e.ease, 1f)));
                Safe(e.onComplete);
                e.cancelled = true;
                return;
            }
        }

        public static void CancelAll()
        {
            for (int i = 0; i < _entries.Count; i++) _entries[i].cancelled = true;
        }

        public static int ActiveCount
        {
            get { int n = 0; for (int i = 0; i < _entries.Count; i++) if (!_entries[i].cancelled) n++; return n; }
        }

        internal static void Tick(float scaledDt, float unscaledDt)
        {
            int snapshot = _entries.Count;
            for (int i = 0; i < snapshot; i++)
            {
                var e = _entries[i];
                if (e.cancelled) continue;
                if (e.watchOwner && e.owner == null) { e.cancelled = true; continue; }

                float d = e.unscaled ? unscaledDt : scaledDt;
                if (e.delay > 0f)
                {
                    e.delay -= d;
                    if (e.delay > 0f) continue;
                    d = -e.delay;   // phần dư đổ vào elapsed
                    e.delay = 0f;
                }

                e.elapsed += d;
                float raw = e.duration <= 0f ? 1f : Mathf.Clamp01(e.elapsed / e.duration);
                try { e.onUpdate(Easing.Evaluate(e.ease, raw)); }
                catch (Exception ex) { Debug.LogException(ex); }

                if (raw >= 1f)
                {
                    Safe(e.onComplete);
                    e.cancelled = true;
                }
            }

            for (int i = _entries.Count - 1; i >= 0; i--)
                if (_entries[i].cancelled) _entries.RemoveAt(i);
        }

        static void Safe(Action a)
        {
            if (a == null) return;
            try { a(); }
            catch (Exception ex) { Debug.LogException(ex); }
        }
    }

    internal class TweenRunner : MonoBehaviour
    {
        internal static TweenRunner Instance;
        void Awake() { if (Instance != null && Instance != this) { Destroy(gameObject); return; } Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; }
        void Update() { Tween.Tick(Time.deltaTime, Time.unscaledDeltaTime); }
    }
}
