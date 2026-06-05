using System;
using System.Collections.Generic;

namespace UyiCore.GameFlow
{
    /// <summary>
    /// FSM generic, non-Mono. Owner gọi Tick/FixedTick từ Update/FixedUpdate.
    /// State instance được cache theo Type — không alloc khi đổi state.
    /// Free-form transition: dùng <see cref="CanTransition"/> để chặn nếu cần.
    /// </summary>
    public class StateMachine<TOwner>
    {
        private readonly TOwner _owner;
        private readonly Dictionary<Type, IState<TOwner>> _cache = new Dictionary<Type, IState<TOwner>>();

        public TOwner Owner => _owner;
        public IState<TOwner> Current { get; private set; }
        public IState<TOwner> Previous { get; private set; }

        /// <summary>Fire sau khi đã OnEnter state mới. (prev, next) — prev có thể null lần đầu.</summary>
        public event Action<IState<TOwner>, IState<TOwner>> OnStateChanged;

        /// <summary>Optional guard: trả false để chặn transition. Null = cho phép tất cả.</summary>
        public Func<IState<TOwner>, IState<TOwner>, bool> CanTransition;

        public StateMachine(TOwner owner)
        {
            _owner = owner;
        }

        /// <summary>Đăng ký state instance có sẵn (dùng khi state cần constructor args).</summary>
        public void Register(IState<TOwner> state)
        {
            if (state == null) return;
            _cache[state.GetType()] = state;
        }

        /// <summary>Lấy hoặc tạo (nếu chưa có) state theo type. Yêu cầu parameterless ctor.</summary>
        public T Get<T>() where T : class, IState<TOwner>, new()
        {
            var t = typeof(T);
            if (!_cache.TryGetValue(t, out var s))
            {
                s = new T();
                _cache[t] = s;
            }
            return (T)s;
        }

        public void ChangeState<T>(bool force = false) where T : class, IState<TOwner>, new()
        {
            ChangeState(Get<T>(), force);
        }

        public void ChangeState(IState<TOwner> next, bool force = false)
        {
            if (next == null) return;
            if (!force && ReferenceEquals(Current, next)) return;
            if (CanTransition != null && !CanTransition(Current, next)) return;

            var prev = Current;
            if (prev != null) prev.OnExit(_owner);
            Current = next;
            Previous = prev;
            next.OnEnter(_owner);
            OnStateChanged?.Invoke(prev, next);
        }

        /// <summary>Quay về state trước đó (vd Paused → Playing). No-op nếu chưa có.</summary>
        public void RevertToPrevious()
        {
            if (Previous != null) ChangeState(Previous);
        }

        public bool IsIn<T>() where T : IState<TOwner>
        {
            return Current is T;
        }

        public void Tick(float deltaTime)
        {
            if (Current != null) Current.OnTick(_owner, deltaTime);
        }

        public void FixedTick(float fixedDeltaTime)
        {
            if (Current != null) Current.OnFixedTick(_owner, fixedDeltaTime);
        }

        /// <summary>Clear current + previous + cache. Gọi khi destroy owner.</summary>
        public void Clear()
        {
            if (Current != null) Current.OnExit(_owner);
            Current = null;
            Previous = null;
            _cache.Clear();
            OnStateChanged = null;
            CanTransition = null;
        }
    }
}
