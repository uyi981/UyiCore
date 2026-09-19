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
    ///     .Condition("HpBelow", (e, p) =&gt; e.HpPercent &lt; p.GetFloat("threshold", 0.2f))
    ///     // dùng Blackboard chia sẻ state giữa các node:
    ///     .Do("PickTarget",  (e, bb, p) =&gt; bb.Set("target", e.FindNearestEnemy()))
    ///     .Condition("HasTarget", (e, bb, p) =&gt; bb.Has("target"));
    /// </code>
    /// Blackboard = <see cref="BehaviorTree{TOwner}.Blackboard"/> của cây (mỗi owner 1 cái);
    /// code ngoài set/get cùng instance đó.
    /// Node <c>Action</c> → <see cref="ActionNode{TOwner}"/> (trả NodeStatus),
    /// <c>Condition</c> → <see cref="ConditionNode{TOwner}"/> (bool),
    /// <c>Do</c> → <see cref="SimpleActionNode{TOwner}"/> (chạy rồi luôn Success).
    /// </summary>
    public class BTRegistry<TOwner>
    {
        // Lưu ở dạng đầy đủ nhất (owner, blackboard, params); overload gọn tự adapt.
        private readonly Dictionary<string, Func<TOwner, Blackboard, BTParams, NodeStatus>> _actions
            = new Dictionary<string, Func<TOwner, Blackboard, BTParams, NodeStatus>>();
        private readonly Dictionary<string, Func<TOwner, Blackboard, BTParams, bool>> _conditions
            = new Dictionary<string, Func<TOwner, Blackboard, BTParams, bool>>();
        private readonly Dictionary<string, Action<TOwner, Blackboard, BTParams>> _dos
            = new Dictionary<string, Action<TOwner, Blackboard, BTParams>>();

        // ---- Action (trả NodeStatus) ----
        public BTRegistry<TOwner> Action(string id, Func<TOwner, Blackboard, BTParams, NodeStatus> fn) { _actions[id] = fn; return this; }
        public BTRegistry<TOwner> Action(string id, Func<TOwner, BTParams, NodeStatus> fn) { _actions[id] = (o, bb, p) => fn(o, p); return this; }
        public BTRegistry<TOwner> Action(string id, Func<TOwner, NodeStatus> fn) { _actions[id] = (o, bb, p) => fn(o); return this; }

        // ---- Condition (trả bool) ----
        public BTRegistry<TOwner> Condition(string id, Func<TOwner, Blackboard, BTParams, bool> fn) { _conditions[id] = fn; return this; }
        public BTRegistry<TOwner> Condition(string id, Func<TOwner, BTParams, bool> fn) { _conditions[id] = (o, bb, p) => fn(o, p); return this; }
        public BTRegistry<TOwner> Condition(string id, Func<TOwner, bool> fn) { _conditions[id] = (o, bb, p) => fn(o); return this; }

        // ---- Do (không trả gì → luôn Success) ----
        public BTRegistry<TOwner> Do(string id, Action<TOwner, Blackboard, BTParams> fn) { _dos[id] = fn; return this; }
        public BTRegistry<TOwner> Do(string id, Action<TOwner, BTParams> fn) { _dos[id] = (o, bb, p) => fn(o, p); return this; }
        public BTRegistry<TOwner> Do(string id, Action<TOwner> fn) { _dos[id] = (o, bb, p) => fn(o); return this; }

        // ---- Lookup (compiler dùng) ----
        public bool TryAction(string id, out Func<TOwner, Blackboard, BTParams, NodeStatus> fn) => _actions.TryGetValue(id ?? "", out fn);
        public bool TryCondition(string id, out Func<TOwner, Blackboard, BTParams, bool> fn) => _conditions.TryGetValue(id ?? "", out fn);
        public bool TryDo(string id, out Action<TOwner, Blackboard, BTParams> fn) => _dos.TryGetValue(id ?? "", out fn);

        /// <summary>Danh sách id đã đăng ký (tiện export cho tool HTML làm catalog).</summary>
        public IEnumerable<string> ActionIds => _actions.Keys;
        public IEnumerable<string> ConditionIds => _conditions.Keys;
        public IEnumerable<string> DoIds => _dos.Keys;
    }
}
