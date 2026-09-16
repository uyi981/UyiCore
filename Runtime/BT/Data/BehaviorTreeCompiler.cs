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
        public static BehaviorTree<TOwner> Compile<TOwner>(BehaviorTreeAsset asset, TOwner owner, BTRegistry<TOwner> registry)
        {
            if (asset == null) { Debug.LogError("[BT.Compile] asset null."); return null; }
            if (registry == null) Debug.LogWarning("[BT.Compile] registry null — mọi leaf Action/Condition/Do sẽ fail.");

            var map = new Dictionary<string, BTNodeData>();
            if (asset.nodes != null)
                foreach (var n in asset.nodes)
                    if (n != null && !string.IsNullOrEmpty(n.id)) map[n.id] = n;

            var root = Build(asset.rootId, map, registry, new HashSet<string>());
            if (root == null)
            {
                Debug.LogError($"[BT.Compile] '{asset.treeName}' build root fail (rootId '{asset.rootId}').");
                return null;
            }
            return new BehaviorTree<TOwner>(owner, root);
        }

        static INode<TOwner> Build<TOwner>(string id, Dictionary<string, BTNodeData> map, BTRegistry<TOwner> reg, HashSet<string> path)
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
                case "Selector": node = Composite(new SelectorNode<TOwner>(), data, map, reg, path); break;
                case "Sequence": node = Composite(new SequenceNode<TOwner>(), data, map, reg, path); break;
                case "Parallel":
                    node = Composite(new ParallelNode<TOwner> { SuccessThreshold = p.GetInt("successThreshold", -1) }, data, map, reg, path);
                    break;

                // ---- Decorators ----
                case "Inverter": node = Decorate(new InverterNode<TOwner>(), data, map, reg, path); break;
                case "Repeater": node = Decorate(new RepeaterNode<TOwner> { Count = p.GetInt("count", -1) }, data, map, reg, path); break;
                case "Cooldown":
                    node = Decorate(new CooldownNode<TOwner> { Duration = p.GetFloat("duration", 1f), ResetOnFailure = p.GetBool("resetOnFailure", true) }, data, map, reg, path);
                    break;
                case "UntilSuccess": node = Decorate(new UntilSuccessNode<TOwner>(), data, map, reg, path); break;
                case "UntilFailure": node = Decorate(new UntilFailureNode<TOwner>(), data, map, reg, path); break;

                // ---- Leaves không cần registry ----
                case "Wait": node = new WaitNode<TOwner>(p.GetFloat("seconds", 1f)); break;
                case "Succeed": node = new SucceedNode<TOwner>(); break;
                case "Fail": node = new FailNode<TOwner>(); break;

                // ---- Leaves bind registry ----
                case "Action": node = BuildAction(data, p, reg); break;
                case "Condition": node = BuildCondition(data, p, reg); break;
                case "Do": node = BuildDo(data, p, reg); break;

                default:
                    Debug.LogError($"[BT.Compile] type lạ '{data.type}' (node '{id}').");
                    node = new FailNode<TOwner>();
                    break;
            }

            if (node != null)
                node.Name = !string.IsNullOrEmpty(data.name) ? data.name : (data.@ref ?? data.type);

            path.Remove(id);
            return node;
        }

        static INode<TOwner> Composite<TOwner>(CompositeNode<TOwner> c, BTNodeData data, Dictionary<string, BTNodeData> map, BTRegistry<TOwner> reg, HashSet<string> path)
        {
            if (data.children != null)
                foreach (var childId in data.children)
                {
                    var child = Build(childId, map, reg, path);
                    if (child != null) c.AddChild(child);
                }
            if (c.ChildCount == 0)
                Debug.LogWarning($"[BT.Compile] composite '{data.id}' ({data.type}) không có child.");
            return c;
        }

        static INode<TOwner> Decorate<TOwner>(DecoratorNode<TOwner> d, BTNodeData data, Dictionary<string, BTNodeData> map, BTRegistry<TOwner> reg, HashSet<string> path)
        {
            string childId = (data.children != null && data.children.Count > 0) ? data.children[0] : null;
            var child = Build(childId, map, reg, path);
            if (child != null) d.SetChild(child);
            else Debug.LogWarning($"[BT.Compile] decorator '{data.id}' ({data.type}) không có child.");
            return d;
        }

        static INode<TOwner> BuildAction<TOwner>(BTNodeData data, BTParams p, BTRegistry<TOwner> reg)
        {
            if (reg != null && reg.TryAction(data.@ref, out var fn))
                return new ActionNode<TOwner>(o => fn(o, p));
            Debug.LogError($"[BT.Compile] Action ref '{data.@ref}' chưa đăng ký trong registry (node '{data.id}').");
            return new FailNode<TOwner>();
        }

        static INode<TOwner> BuildCondition<TOwner>(BTNodeData data, BTParams p, BTRegistry<TOwner> reg)
        {
            if (reg != null && reg.TryCondition(data.@ref, out var fn))
                return new ConditionNode<TOwner>(o => fn(o, p));
            Debug.LogError($"[BT.Compile] Condition ref '{data.@ref}' chưa đăng ký (node '{data.id}').");
            return new FailNode<TOwner>();
        }

        static INode<TOwner> BuildDo<TOwner>(BTNodeData data, BTParams p, BTRegistry<TOwner> reg)
        {
            if (reg != null && reg.TryDo(data.@ref, out var fn))
                return new SimpleActionNode<TOwner>(o => fn(o, p));
            Debug.LogError($"[BT.Compile] Do ref '{data.@ref}' chưa đăng ký (node '{data.id}').");
            return new FailNode<TOwner>();
        }
    }
}
