using System;
using System.Collections.Generic;

namespace UyiCore.BT
{
    /// <summary>
    /// Bảng ánh xạ id (string) → hành vi thật trong C#. Đây là "cầu nối" giữa
    /// cây data (SO/JSON) và code game — vì lambda không serialize được.
    ///
    /// Khai 1 lần cho từng loại owner, vd:
    /// <code>
    /// var reg = new BTRegistry&lt;Enemy&gt;()
    ///     .Do("Patrol",       e =&gt; e.Patrol())
    ///     .Do("Flee",         e =&gt; e.Flee())
    ///     .Action("Chase",    e =&gt; e.MoveTo(e.Player.position)
    ///                                ? NodeStatus.Success : NodeStatus.Running)
    ///     .Condition("CanSeePlayer", e =&gt; e.CanSeePlayer())
    ///     .Condition("HpBelow", (e, p) =&gt; e.HpPercent &lt; p.GetFloat("threshold", 0.2f));
    /// </code>
    /// Node <c>Action</c> → <see cref="ActionNode{TOwner}"/> (trả NodeStatus),
    /// <c>Condition</c> → <see cref="ConditionNode{TOwner}"/> (bool),
    /// <c>Do</c> → <see cref="SimpleActionNode{TOwner}"/> (chạy rồi luôn Success).
    /// </summary>
    public class BTRegistry<TOwner>
    {
        private readonly Dictionary<string, Func<TOwner, BTParams, NodeStatus>> _actions
            = new Dictionary<string, Func<TOwner, BTParams, NodeStatus>>();
        private readonly Dictionary<string, Func<TOwner, BTParams, bool>> _conditions
            = new Dictionary<string, Func<TOwner, BTParams, bool>>();
        private readonly Dictionary<string, Action<TOwner, BTParams>> _dos
            = new Dictionary<string, Action<TOwner, BTParams>>();

        // ---- Action (trả NodeStatus) ----
        public BTRegistry<TOwner> Action(string id, Func<TOwner, BTParams, NodeStatus> fn) { _actions[id] = fn; return this; }
        public BTRegistry<TOwner> Action(string id, Func<TOwner, NodeStatus> fn) { _actions[id] = (o, _) => fn(o); return this; }

        // ---- Condition (trả bool) ----
        public BTRegistry<TOwner> Condition(string id, Func<TOwner, BTParams, bool> fn) { _conditions[id] = fn; return this; }
        public BTRegistry<TOwner> Condition(string id, Func<TOwner, bool> fn) { _conditions[id] = (o, _) => fn(o); return this; }

        // ---- Do (không trả gì → luôn Success) ----
        public BTRegistry<TOwner> Do(string id, Action<TOwner, BTParams> fn) { _dos[id] = fn; return this; }
        public BTRegistry<TOwner> Do(string id, Action<TOwner> fn) { _dos[id] = (o, _) => fn(o); return this; }

        // ---- Lookup (compiler dùng) ----
        public bool TryAction(string id, out Func<TOwner, BTParams, NodeStatus> fn) => _actions.TryGetValue(id ?? "", out fn);
        public bool TryCondition(string id, out Func<TOwner, BTParams, bool> fn) => _conditions.TryGetValue(id ?? "", out fn);
        public bool TryDo(string id, out Action<TOwner, BTParams> fn) => _dos.TryGetValue(id ?? "", out fn);

        /// <summary>Danh sách id đã đăng ký (tiện export cho tool HTML làm catalog).</summary>
        public IEnumerable<string> ActionIds => _actions.Keys;
        public IEnumerable<string> ConditionIds => _conditions.Keys;
        public IEnumerable<string> DoIds => _dos.Keys;
    }
}
