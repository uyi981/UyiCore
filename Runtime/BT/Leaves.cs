using System;

namespace UyiCore.BT
{
    /// <summary>
    /// Leaf chạy 1 hàm trả NodeStatus. Hàm có thể return Running để giữ control nhiều tick.
    /// </summary>
    public class ActionNode<TOwner> : LeafNode<TOwner>
    {
        private readonly Func<TOwner, NodeStatus> _fn;
        public ActionNode(Func<TOwner, NodeStatus> fn) { _fn = fn; }
        public override NodeStatus Tick(TOwner owner, float deltaTime)
        {
            return _fn != null ? _fn(owner) : NodeStatus.Failure;
        }
    }

    /// <summary>Variant không trả Status — luôn return Success sau khi gọi.</summary>
    public class SimpleActionNode<TOwner> : LeafNode<TOwner>
    {
        private readonly Action<TOwner> _fn;
        public SimpleActionNode(Action<TOwner> fn) { _fn = fn; }
        public override NodeStatus Tick(TOwner owner, float deltaTime)
        {
            _fn?.Invoke(owner);
            return NodeStatus.Success;
        }
    }

    /// <summary>Predicate. True = Success, False = Failure.</summary>
    public class ConditionNode<TOwner> : LeafNode<TOwner>
    {
        private readonly Func<TOwner, bool> _fn;
        public ConditionNode(Func<TOwner, bool> fn) { _fn = fn; }
        public override NodeStatus Tick(TOwner owner, float deltaTime)
        {
            return _fn != null && _fn(owner) ? NodeStatus.Success : NodeStatus.Failure;
        }
    }

    /// <summary>Đợi N giây rồi return Success.</summary>
    public class WaitNode<TOwner> : LeafNode<TOwner>
    {
        public float Duration;
        private float _elapsed;

        public WaitNode(float duration) { Duration = duration; }

        public override NodeStatus Tick(TOwner owner, float deltaTime)
        {
            _elapsed += deltaTime;
            if (_elapsed >= Duration)
            {
                _elapsed = 0f;
                return NodeStatus.Success;
            }
            return NodeStatus.Running;
        }

        public override void Reset(TOwner owner) { _elapsed = 0f; }
    }

    /// <summary>Luôn return Success — dùng để force parent advance.</summary>
    public class SucceedNode<TOwner> : LeafNode<TOwner>
    {
        public override NodeStatus Tick(TOwner owner, float deltaTime) => NodeStatus.Success;
    }

    /// <summary>Luôn return Failure — dùng để force Selector thử nhánh khác.</summary>
    public class FailNode<TOwner> : LeafNode<TOwner>
    {
        public override NodeStatus Tick(TOwner owner, float deltaTime) => NodeStatus.Failure;
    }
}
