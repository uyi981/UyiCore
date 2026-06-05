using System;
using System.Collections.Generic;
using UnityEngine;

namespace UyiCore.Timing
{
    /// <summary>Token tham chiếu 1 timer. Có thể Cancel hoặc check IsActive.</summary>
    public readonly struct TimerHandle : IEquatable<TimerHandle>
    {
        internal readonly int Id;
        internal TimerHandle(int id) { Id = id; }
        public bool IsActive => Timer.IsActive(this);
        public void Cancel() => Timer.Cancel(this);
        public bool Equals(TimerHandle other) => Id == other.Id;
        public override bool Equals(object obj) => obj is TimerHandle h && Equals(h);
        public override int GetHashCode() => Id;
        public static bool operator ==(TimerHandle a, TimerHandle b) => a.Id == b.Id;
        public static bool operator !=(TimerHandle a, TimerHandle b) => a.Id != b.Id;
    }

    /// <summary>
    /// Schedule callback theo thời gian. Static API, runner singleton tự spawn runtime.
    /// One-shot: <see cref="After"/>. Repeating: <see cref="Every"/>.
    /// </summary>
    public static class Timer
    {
        class Entry
        {
            public int id;
            public float remaining;
            public float interval;     // 0 = one-shot
            public int repeatLeft;     // -1 = infinite, > 0 = số lần còn lại
            public Action callback;
            public bool unscaled;
            public bool watchOwner;
            public UnityEngine.Object owner;
            public bool cancelled;
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
            if (TimerRunner.Instance != null) return;
            var go = new GameObject("[TimerRunner]");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<TimerRunner>();
        }

        // ---- Public API ----

        public static TimerHandle After(float seconds, Action callback, bool unscaled = false, UnityEngine.Object owner = null)
        {
            if (callback == null) return default;
            var e = new Entry
            {
                id = _nextId++,
                remaining = seconds,
                interval = 0f,
                repeatLeft = 0,
                callback = callback,
                unscaled = unscaled,
                watchOwner = owner != null,
                owner = owner,
            };
            _entries.Add(e);
            return new TimerHandle(e.id);
        }

        public static TimerHandle Every(float interval, Action callback, int repeatCount = -1, bool unscaled = false, UnityEngine.Object owner = null)
        {
            if (callback == null || interval <= 0f) return default;
            var e = new Entry
            {
                id = _nextId++,
                remaining = interval,
                interval = interval,
                repeatLeft = repeatCount,
                callback = callback,
                unscaled = unscaled,
                watchOwner = owner != null,
                owner = owner,
            };
            _entries.Add(e);
            return new TimerHandle(e.id);
        }

        public static bool Cancel(TimerHandle h)
        {
            if (h.Id == 0) return false;
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].id == h.Id)
                {
                    _entries[i].cancelled = true;
                    return true;
                }
            }
            return false;
        }

        public static bool IsActive(TimerHandle h)
        {
            if (h.Id == 0) return false;
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].id == h.Id) return !_entries[i].cancelled;
            }
            return false;
        }

        public static void CancelAll()
        {
            for (int i = 0; i < _entries.Count; i++) _entries[i].cancelled = true;
        }

        public static int ActiveCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < _entries.Count; i++) if (!_entries[i].cancelled) n++;
                return n;
            }
        }

        // ---- Runner tick (gọi từ TimerRunner) ----

        internal static void Tick(float scaledDt, float unscaledDt)
        {
            // Chỉ xử lý timer đã tồn tại đầu frame — timer mới add trong callback đợi frame sau.
            int snapshot = _entries.Count;
            for (int i = 0; i < snapshot; i++)
            {
                var e = _entries[i];
                if (e.cancelled) continue;
                if (e.watchOwner && e.owner == null) { e.cancelled = true; continue; }

                e.remaining -= e.unscaled ? unscaledDt : scaledDt;
                if (e.remaining > 0f) continue;

                try { e.callback?.Invoke(); }
                catch (Exception ex) { Debug.LogException(ex); }

                if (e.cancelled) continue;

                if (e.interval > 0f)
                {
                    if (e.repeatLeft > 0)
                    {
                        e.repeatLeft--;
                        if (e.repeatLeft == 0) { e.cancelled = true; continue; }
                    }
                    e.remaining += e.interval;
                }
                else
                {
                    e.cancelled = true;
                }
            }

            // Dọn timer cancelled.
            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                if (_entries[i].cancelled) _entries.RemoveAt(i);
            }
        }
    }

    /// <summary>MonoBehaviour runner. Auto-spawn từ <see cref="Timer.EnsureRunner"/>.</summary>
    internal class TimerRunner : MonoBehaviour
    {
        internal static TimerRunner Instance;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            Timer.Tick(Time.deltaTime, Time.unscaledDeltaTime);
        }
    }
}
