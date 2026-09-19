using System;
using System.Collections.Generic;
using UnityEngine;

namespace UyiCore.BT
{
    /// <summary>
    /// Wrapper sở hữu root node + blackboard + tick logic. Owner gọi Tick mỗi frame.
    /// </summary>
    public class BehaviorTree<TOwner>
    {
        public TOwner Owner { get; }
        public INode<TOwner> Root { get; }
        public Blackboard Blackboard { get; }
        public NodeStatus LastStatus { get; private set; } = NodeStatus.Running;

        /// <summary>Chạy tree mỗi N giây (0 = mỗi frame). Useful cho enemy xa player.</summary>
        public float TickInterval = 0f;
        private float _elapsed;

        /// <summary>true = xong (Success/Failure) thì Reset chạy lại (mặc định). false = one-shot: dừng hẳn.</summary>
        public bool Loop = true;
        /// <summary>Đã dừng (one-shot xong, hoặc gọi <see cref="Stop"/>). Tick sẽ no-op tới khi <see cref="Reset"/>.</summary>
        public bool IsFinished { get; private set; }

        public event Action<NodeStatus> OnTreeCompleted;

        /// <summary>Dừng cây (Tick thành no-op). Gọi <see cref="Reset"/> để chạy lại.</summary>
        public void Stop() => IsFinished = true;

        public BehaviorTree(TOwner owner, INode<TOwner> root) : this(owner, root, null) { }

        /// <summary>
        /// <paramref name="blackboard"/> = kho state chia sẻ cho mọi node. Truyền vào để
        /// leaf (registry lambda) và code ngoài dùng CHUNG 1 instance; null → tự tạo mới.
        /// </summary>
        public BehaviorTree(TOwner owner, INode<TOwner> root, Blackboard blackboard)
        {
            Owner = owner;
            Root = root;
            Blackboard = blackboard ?? new Blackboard();
        }

        public void Tick(float deltaTime)
        {
            if (Root == null || IsFinished) return;

            if (TickInterval > 0f)
            {
                _elapsed += deltaTime;
                if (_elapsed < TickInterval) return;
                deltaTime = _elapsed;
                _elapsed = 0f;
            }

            LastStatus = Root.Tick(Owner, deltaTime);
            if (LastStatus != NodeStatus.Running)
            {
                OnTreeCompleted?.Invoke(LastStatus);
                if (Loop) Root.Reset(Owner);
                else IsFinished = true;
            }
        }

        public void Reset()
        {
            Root?.Reset(Owner);
            _elapsed = 0f;
            LastStatus = NodeStatus.Running;
            IsFinished = false;
        }
    }

    /// <summary>Entry point cho fluent builder. <c>BT.Build&lt;EnemyController&gt;(this).Selector()...Build()</c></summary>
    public static class BT
    {
        public static BTBuilder<TOwner> Build<TOwner>(TOwner owner) => new BTBuilder<TOwner>(owner);
    }

    /// <summary>
    /// Fluent builder. Composite/Decorator push lên stack, .End() pop.
    /// Khi pop về root → cuối cùng .Build() trả BehaviorTree.
    /// </summary>
    public class BTBuilder<TOwner>
    {
        private readonly TOwner _owner;
        private readonly Stack<INode<TOwner>> _stack = new Stack<INode<TOwner>>();
        private INode<TOwner> _root;

        public BTBuilder(TOwner owner) { _owner = owner; }

        // ---- Composites ----

        public BTBuilder<TOwner> Sequence(string name = null) => PushComposite(new SequenceNode<TOwner>(), name);
        public BTBuilder<TOwner> Selector(string name = null) => PushComposite(new SelectorNode<TOwner>(), name);
        public BTBuilder<TOwner> ReactiveSelector(string name = null) => PushComposite(new ReactiveSelectorNode<TOwner>(), name);
        public BTBuilder<TOwner> ReactiveSequence(string name = null) => PushComposite(new ReactiveSequenceNode<TOwner>(), name);

        public BTBuilder<TOwner> Parallel(int successThreshold = -1, string name = null)
        {
            var n = new ParallelNode<TOwner> { SuccessThreshold = successThreshold };
            return PushComposite(n, name);
        }

        // ---- Decorators ----

        public BTBuilder<TOwner> Inverter(string name = null) => PushDecorator(new InverterNode<TOwner>(), name);

        public BTBuilder<TOwner> Repeater(int count = -1, string name = null)
        {
            return PushDecorator(new RepeaterNode<TOwner> { Count = count }, name);
        }

        public BTBuilder<TOwner> Cooldown(float duration, string name = null)
        {
            return PushDecorator(new CooldownNode<TOwner> { Duration = duration }, name);
        }

        public BTBuilder<TOwner> UntilSuccess(string name = null) => PushDecorator(new UntilSuccessNode<TOwner>(), name);
        public BTBuilder<TOwner> UntilFailure(string name = null) => PushDecorator(new UntilFailureNode<TOwner>(), name);

        // ---- Leaves ----

        public BTBuilder<TOwner> Action(Func<TOwner, NodeStatus> fn, string name = null) => AddLeaf(new ActionNode<TOwner>(fn), name);
        public BTBuilder<TOwner> Do(Action<TOwner> fn, string name = null) => AddLeaf(new SimpleActionNode<TOwner>(fn), name);
        public BTBuilder<TOwner> Condition(Func<TOwner, bool> fn, string name = null) => AddLeaf(new ConditionNode<TOwner>(fn), name);
        public BTBuilder<TOwner> Wait(float seconds, string name = null) => AddLeaf(new WaitNode<TOwner>(seconds), name);
        public BTBuilder<TOwner> Succeed(string name = null) => AddLeaf(new SucceedNode<TOwner>(), name);
        public BTBuilder<TOwner> Fail(string name = null) => AddLeaf(new FailNode<TOwner>(), name);

        /// <summary>Add 1 node tự tạo (dùng cho custom node).</summary>
        public BTBuilder<TOwner> Custom(INode<TOwner> node)
        {
            AttachToParent(node);
            return this;
        }

        // ---- End / Build ----

        public BTBuilder<TOwner> End()
        {
            if (_stack.Count == 0)
            {
                Debug.LogError("[BTBuilder] End() gọi quá nhiều — stack đã rỗng.");
                return this;
            }
            _stack.Pop();
            return this;
        }

        public BehaviorTree<TOwner> Build()
        {
            if (_stack.Count > 0)
                Debug.LogWarning($"[BTBuilder] Build() khi stack còn {_stack.Count} composite/decorator chưa End().");
            if (_root == null)
            {
                Debug.LogError("[BTBuilder] Build() không có node nào.");
                return null;
            }
            return new BehaviorTree<TOwner>(_owner, _root);
        }

        // ---- Internal ----

        BTBuilder<TOwner> PushComposite(CompositeNode<TOwner> n, string name)
        {
            n.Name = name;
            AttachToParent(n);
            _stack.Push(n);
            return this;
        }

        BTBuilder<TOwner> PushDecorator(DecoratorNode<TOwner> n, string name)
        {
            n.Name = name;
            AttachToParent(n);
            _stack.Push(n);
            return this;
        }

        BTBuilder<TOwner> AddLeaf(INode<TOwner> n, string name)
        {
            n.Name = name;
            AttachToParent(n);
            return this;
        }

        void AttachToParent(INode<TOwner> n)
        {
            if (_stack.Count == 0)
            {
                if (_root != null)
                {
                    Debug.LogError("[BTBuilder] Root đã có rồi — không thể add node ở top-level lần nữa.");
                    return;
                }
                _root = n;
                return;
            }

            var parent = _stack.Peek();
            if (parent is CompositeNode<TOwner> c) c.AddChild(n);
            else if (parent is DecoratorNode<TOwner> d)
            {
                if (d.Child != null)
                {
                    Debug.LogError($"[BTBuilder] Decorator '{parent.Name ?? parent.GetType().Name}' chỉ nhận 1 child.");
                    return;
                }
                d.SetChild(n);
            }
        }

    }
}
