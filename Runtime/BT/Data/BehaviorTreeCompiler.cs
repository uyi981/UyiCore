using System.Collections.Generic;
using UnityEngine;

namespace UyiCore.BT
{
    /// <summary>
    /// Dịch <see cref="BehaviorTreeAsset"/> (data) → cây <see cref="BehaviorTree{TOwner}"/> runtime.
    /// Composite/decorator resolve theo <c>type</c>; leaf (Action/Condition/Do) resolve <c>ref</c>
    /// qua <see cref="BTRegistry{TOwner}"/>. Gọi lúc spawn để mỗi owner có 1 cây state riêng.
    ///
    /// <code>
    /// _bt = BehaviorTreeCompiler.Compile(asset, this, reg);
    /// void Update() =&gt; _bt.Tick(Time.deltaTime);
    /// </code>
    /// </summary>
    public static class BehaviorTreeCompiler
    {
        /// <param name="blackboard">Kho state chia sẻ; null → tự tạo. Truyền vào nếu muốn set
        /// giá trị ban đầu / dùng chung 1 blackboard đã có. Leaf (registry) và cây dùng chung instance này.</param>
        /// <param name="debug">true → bọc mỗi node để ghi status live + đăng ký vào
        /// <see cref="BTDebug"/> cho cửa sổ Behavior Tree Debugger. Tắt (mặc định) = zero cost.</param>
        public static BehaviorTree<TOwner> Compile<TOwner>(BehaviorTreeAsset asset, TOwner owner, BTRegistry<TOwner> registry, Blackboard blackboard = null, bool debug = false)
        {
            if (asset == null) { Debug.LogError("[BT.Compile] asset null."); return null; }
            if (registry == null) Debug.LogWarning("[BT.Compile] registry null — mọi leaf Action/Condition/Do sẽ fail.");

            var map = new Dictionary<string, BTNodeData>();
            if (asset.nodes != null)
                foreach (var n in asset.nodes)
                    if (n != null && !string.IsNullOrEmpty(n.id)) map[n.id] = n;

            WarnInfiniteRepeaters(map);

            var bb = blackboard ?? new Blackboard();   // 1 blackboard/owner, dùng chung leaf + tree
            var trace = debug ? new BTDebugTrace() : null;
            var root = Build(asset.rootId, map, registry, bb, trace, new HashSet<string>());
            if (root == null)
            {
                Debug.LogError($"[BT.Compile] '{asset.treeName}' build root fail (rootId '{asset.rootId}').");
                return null;
            }
            if (trace != null) BTDebug.Register(DebugLabel(owner, asset), asset, trace, owner);
            return new BehaviorTree<TOwner>(owner, root, bb);
        }

        static string DebugLabel<TOwner>(TOwner owner, BehaviorTreeAsset asset)
        {
            string who = owner is UnityEngine.Object uo ? uo.name : (owner != null ? owner.ToString() : "?");
            return $"{who} · {(asset != null ? asset.name : "?")}";
        }

        static void WarnInfiniteRepeaters(Dictionary<string, BTNodeData> map)
        {
            foreach (var n in map.Values)
            {
                if (n?.children == null) continue;
                if (n.type != "Sequence" && n.type != "ReactiveSequence") continue;
                for (int i = 0; i < n.children.Count - 1; i++)   // trừ node cuối
                {
                    if (map.TryGetValue(n.children[i], out var c) && c != null && c.type == "Repeater"
                        && new BTParams(c.@params).GetInt("count", -1) <= 0)
                        Debug.LogWarning($"[BT.Compile] Repeater vô hạn '{c.id}' nằm GIỮA Sequence '{n.id}' — các node sau nó sẽ không bao giờ chạy.");
                }
            }
        }

        static INode<TOwner> Build<TOwner>(string id, Dictionary<string, BTNodeData> map, BTRegistry<TOwner> reg, Blackboard bb, BTDebugTrace trace, HashSet<string> path)
        {
            if (string.IsNullOrEmpty(id) || !map.TryGetValue(id, out var data))
            {
                Debug.LogError($"[BT.Compile] node id '{id}' không tồn tại.");
                return null;
            }
            if (!path.Add(id))
            {
                Debug.LogError($"[BT.Compile] phát hiện vòng lặp tại node '{id}'.");
                return null;
            }

            var p = new BTParams(data.@params);
            INode<TOwner> node;

            switch (data.type)
            {
                // ---- Composites ----
                case "Selector": node = Composite(new SelectorNode<TOwner>(), data, map, reg, bb, trace, path); break;
                case "Sequence": node = Composite(new SequenceNode<TOwner>(), data, map, reg, bb, trace, path); break;
                case "ReactiveSelector": node = Composite(new ReactiveSelectorNode<TOwner>(), data, map, reg, bb, trace, path); break;
                case "ReactiveSequence": node = Composite(new ReactiveSequenceNode<TOwner>(), data, map, reg, bb, trace, path); break;
                case "Parallel":
                    node = Composite(new ParallelNode<TOwner> { SuccessThreshold = p.GetInt("successThreshold", -1) }, data, map, reg, bb, trace, path);
                    break;

                // ---- Decorators ----
                case "Inverter": node = Decorate(new InverterNode<TOwner>(), data, map, reg, bb, trace, path); break;
                case "Repeater": node = Decorate(new RepeaterNode<TOwner> { Count = p.GetInt("count", -1) }, data, map, reg, bb, trace, path); break;
                case "Cooldown":
                    node = Decorate(new CooldownNode<TOwner> { Duration = p.GetFloat("duration", 1f), ResetOnFailure = p.GetBool("resetOnFailure", true) }, data, map, reg, bb, trace, path);
                    break;
                case "UntilSuccess": node = Decorate(new UntilSuccessNode<TOwner>(), data, map, reg, bb, trace, path); break;
                case "UntilFailure": node = Decorate(new UntilFailureNode<TOwner>(), data, map, reg, bb, trace, path); break;

                // ---- Leaves không cần registry ----
                case "Wait": node = new WaitNode<TOwner>(p.GetFloat("seconds", 1f)); break;
                case "Succeed": node = new SucceedNode<TOwner>(); break;
                case "Fail": node = new FailNode<TOwner>(); break;

                // ---- Leaves bind registry ----
                case "Action": node = BuildAction(data, p, reg, bb); break;
                case "Condition": node = BuildCondition(data, p, reg, bb); break;
                case "Do": node = BuildDo(data, p, reg, bb); break;

                default:
                    Debug.LogError($"[BT.Compile] type lạ '{data.type}' (node '{id}').");
                    node = new FailNode<TOwner>();
                    break;
            }

            if (node != null)
                node.Name = !string.IsNullOrEmpty(data.name) ? data.name : (data.@ref ?? data.type);

            path.Remove(id);

            if (trace != null && node != null) node = new DebugNode<TOwner>(id, node, trace);
            return node;
        }

        static INode<TOwner> Composite<TOwner>(CompositeNode<TOwner> c, BTNodeData data, Dictionary<string, BTNodeData> map, BTRegistry<TOwner> reg, Blackboard bb, BTDebugTrace trace, HashSet<string> path)
        {
            if (data.children != null)
                foreach (var childId in data.children)
                {
                    var child = Build(childId, map, reg, bb, trace, path);
                    if (child != null) c.AddChild(child);
                }
            if (c.ChildCount == 0)
                Debug.LogWarning($"[BT.Compile] composite '{data.id}' ({data.type}) không có child.");
            return c;
        }

        static INode<TOwner> Decorate<TOwner>(DecoratorNode<TOwner> d, BTNodeData data, Dictionary<string, BTNodeData> map, BTRegistry<TOwner> reg, Blackboard bb, BTDebugTrace trace, HashSet<string> path)
        {
            string childId = (data.children != null && data.children.Count > 0) ? data.children[0] : null;
            var child = Build(childId, map, reg, bb, trace, path);
            if (child != null) d.SetChild(child);
            else Debug.LogWarning($"[BT.Compile] decorator '{data.id}' ({data.type}) không có child.");
            return d;
        }

        static INode<TOwner> BuildAction<TOwner>(BTNodeData data, BTParams p, BTRegistry<TOwner> reg, Blackboard bb)
        {
            if (reg != null && reg.TryAction(data.@ref, out var fn))
                return new ActionNode<TOwner>(o => fn(o, bb, p));
            Debug.LogError($"[BT.Compile] Action ref '{data.@ref}' chưa đăng ký trong registry (node '{data.id}').");
            return new FailNode<TOwner>();
        }

        static INode<TOwner> BuildCondition<TOwner>(BTNodeData data, BTParams p, BTRegistry<TOwner> reg, Blackboard bb)
        {
            if (reg != null && reg.TryCondition(data.@ref, out var fn))
                return new ConditionNode<TOwner>(o => fn(o, bb, p));
            Debug.LogError($"[BT.Compile] Condition ref '{data.@ref}' chưa đăng ký (node '{data.id}').");
            return new FailNode<TOwner>();
        }

        static INode<TOwner> BuildDo<TOwner>(BTNodeData data, BTParams p, BTRegistry<TOwner> reg, Blackboard bb)
        {
            if (reg != null && reg.TryDo(data.@ref, out var fn))
                return new SimpleActionNode<TOwner>(o => fn(o, bb, p));
            Debug.LogError($"[BT.Compile] Do ref '{data.@ref}' chưa đăng ký (node '{data.id}').");
            return new FailNode<TOwner>();
        }
    }
}
