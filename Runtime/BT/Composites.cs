namespace UyiCore.BT
{
    /// <summary>
    /// Chạy child theo thứ tự. Fail nếu bất kỳ child fail. Success khi tất cả success.
    /// Khi child Running → return Running, lần Tick sau tiếp tục từ child đó.
    /// </summary>
    public class SequenceNode<TOwner> : CompositeNode<TOwner>
    {
        private int _current;

        public override NodeStatus Tick(TOwner owner, float deltaTime)
        {
            while (_current < Children.Count)
            {
                var s = Children[_current].Tick(owner, deltaTime);
                if (s == NodeStatus.Running) return NodeStatus.Running;
                if (s == NodeStatus.Failure)
                {
                    ResetState(owner);
                    return NodeStatus.Failure;
                }
                _current++;
            }
            ResetState(owner);
            return NodeStatus.Success;
        }

        public override void Reset(TOwner owner)
        {
            base.Reset(owner);
            _current = 0;
        }

        void ResetState(TOwner owner) => Reset(owner);
    }

    /// <summary>
    /// Chạy child theo thứ tự. Success nếu bất kỳ child success. Fail khi tất cả fail.
    /// = "if A else if B else if C" pattern.
    /// </summary>
    public class SelectorNode<TOwner> : CompositeNode<TOwner>
    {
        private int _current;

        public override NodeStatus Tick(TOwner owner, float deltaTime)
        {
            while (_current < Children.Count)
            {
                var s = Children[_current].Tick(owner, deltaTime);
                if (s == NodeStatus.Running) return NodeStatus.Running;
                if (s == NodeStatus.Success)
                {
                    ResetState(owner);
                    return NodeStatus.Success;
                }
                _current++;
            }
            ResetState(owner);
            return NodeStatus.Failure;
        }

        public override void Reset(TOwner owner)
        {
            base.Reset(owner);
            _current = 0;
        }

        void ResetState(TOwner owner) => Reset(owner);
    }

    /// <summary>
    /// Chạy tất cả child song song mỗi tick.
    /// Policy: Success khi >= SuccessThreshold child success; Fail khi tổng (fail+success) > Children.Count - SuccessThreshold.
    /// SuccessThreshold = -1 (default) ⇒ cần TẤT CẢ success.
    /// </summary>
    public class ParallelNode<TOwner> : CompositeNode<TOwner>
    {
        public int SuccessThreshold = -1;
        private NodeStatus[] _last;

        public override NodeStatus Tick(TOwner owner, float deltaTime)
        {
            if (_last == null || _last.Length != Children.Count) _last = new NodeStatus[Children.Count];

            int successCount = 0;
            int failCount = 0;
            int needed = SuccessThreshold > 0 ? SuccessThreshold : Children.Count;

            for (int i = 0; i < Children.Count; i++)
            {
                if (_last[i] == NodeStatus.Success) { successCount++; continue; }
                if (_last[i] == NodeStatus.Failure) { failCount++; continue; }

                var s = Children[i].Tick(owner, deltaTime);
                _last[i] = s;
                if (s == NodeStatus.Success) successCount++;
                else if (s == NodeStatus.Failure) failCount++;
            }

            if (successCount >= needed)
            {
                Reset(owner);
                return NodeStatus.Success;
            }
            if (failCount > Children.Count - needed)
            {
                Reset(owner);
                return NodeStatus.Failure;
            }
            return NodeStatus.Running;
        }

        public override void Reset(TOwner owner)
        {
            base.Reset(owner);
            if (_last != null) System.Array.Clear(_last, 0, _last.Length);
        }
    }
}
