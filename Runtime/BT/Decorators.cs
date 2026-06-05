namespace UyiCore.BT
{
    /// <summary>Đảo Success ↔ Failure. Running giữ nguyên.</summary>
    public class InverterNode<TOwner> : DecoratorNode<TOwner>
    {
        public override NodeStatus Tick(TOwner owner, float deltaTime)
        {
            if (Child == null) return NodeStatus.Failure;
            var s = Child.Tick(owner, deltaTime);
            if (s == NodeStatus.Success) return NodeStatus.Failure;
            if (s == NodeStatus.Failure) return NodeStatus.Success;
            return NodeStatus.Running;
        }
    }

    /// <summary>
    /// Lặp child Count lần. Count = -1 → vô hạn.
    /// Mỗi lần child kết thúc (Success/Failure) tính 1 lần lặp.
    /// </summary>
    public class RepeaterNode<TOwner> : DecoratorNode<TOwner>
    {
        public int Count = -1;
        private int _done;

        public override NodeStatus Tick(TOwner owner, float deltaTime)
        {
            if (Child == null) return NodeStatus.Success;
            var s = Child.Tick(owner, deltaTime);
            if (s == NodeStatus.Running) return NodeStatus.Running;

            Child.Reset(owner);
            _done++;
            if (Count > 0 && _done >= Count)
            {
                _done = 0;
                return NodeStatus.Success;
            }
            return NodeStatus.Running;
        }

        public override void Reset(TOwner owner)
        {
            base.Reset(owner);
            _done = 0;
        }
    }

    /// <summary>
    /// Chặn child chạy nếu chưa hết cooldown. Trả Failure trong thời gian cooldown.
    /// Cooldown bắt đầu khi child trả Success (hoặc Failure nếu ResetOnFailure=true).
    /// </summary>
    public class CooldownNode<TOwner> : DecoratorNode<TOwner>
    {
        public float Duration;
        public bool ResetOnFailure = true;
        private float _remaining;

        public override NodeStatus Tick(TOwner owner, float deltaTime)
        {
            if (_remaining > 0f)
            {
                _remaining -= deltaTime;
                return NodeStatus.Failure;
            }
            if (Child == null) return NodeStatus.Failure;

            var s = Child.Tick(owner, deltaTime);
            if (s == NodeStatus.Success || (s == NodeStatus.Failure && ResetOnFailure))
            {
                _remaining = Duration;
            }
            return s;
        }

        public override void Reset(TOwner owner)
        {
            base.Reset(owner);
            _remaining = 0f;
        }
    }

    /// <summary>Lặp child tới khi Success. Trong lúc đó luôn return Running.</summary>
    public class UntilSuccessNode<TOwner> : DecoratorNode<TOwner>
    {
        public override NodeStatus Tick(TOwner owner, float deltaTime)
        {
            if (Child == null) return NodeStatus.Success;
            var s = Child.Tick(owner, deltaTime);
            if (s == NodeStatus.Success) return NodeStatus.Success;
            if (s == NodeStatus.Failure) Child.Reset(owner);
            return NodeStatus.Running;
        }
    }

    /// <summary>Lặp child tới khi Failure. Trong lúc đó luôn return Running.</summary>
    public class UntilFailureNode<TOwner> : DecoratorNode<TOwner>
    {
        public override NodeStatus Tick(TOwner owner, float deltaTime)
        {
            if (Child == null) return NodeStatus.Failure;
            var s = Child.Tick(owner, deltaTime);
            if (s == NodeStatus.Failure) return NodeStatus.Success;
            if (s == NodeStatus.Success) Child.Reset(owner);
            return NodeStatus.Running;
        }
    }
}
