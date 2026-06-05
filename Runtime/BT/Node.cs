namespace UyiCore.BT
{
    public enum NodeStatus
    {
        Running,
        Success,
        Failure,
    }

    /// <summary>
    /// Node trong behavior tree. TOwner = kiểu object sở hữu tree (vd EnemyController).
    /// Tick được gọi mỗi tick — trả về Running để giữ control, Success/Failure để parent advance.
    /// Reset gọi khi tree restart hoặc parent abandon node này.
    /// </summary>
    public interface INode<TOwner>
    {
        NodeStatus Tick(TOwner owner, float deltaTime);
        void Reset(TOwner owner);
        string Name { get; set; }
    }

    /// <summary>Base rỗng cho leaf node — chỉ implement Tick.</summary>
    public abstract class LeafNode<TOwner> : INode<TOwner>
    {
        public string Name { get; set; }
        public abstract NodeStatus Tick(TOwner owner, float deltaTime);
        public virtual void Reset(TOwner owner) { }
    }

    /// <summary>Base cho composite có nhiều child.</summary>
    public abstract class CompositeNode<TOwner> : INode<TOwner>
    {
        public string Name { get; set; }
        protected readonly System.Collections.Generic.List<INode<TOwner>> Children = new System.Collections.Generic.List<INode<TOwner>>();

        public void AddChild(INode<TOwner> child) => Children.Add(child);

        public abstract NodeStatus Tick(TOwner owner, float deltaTime);

        public virtual void Reset(TOwner owner)
        {
            for (int i = 0; i < Children.Count; i++) Children[i].Reset(owner);
        }
    }

    /// <summary>Base cho decorator có 1 child.</summary>
    public abstract class DecoratorNode<TOwner> : INode<TOwner>
    {
        public string Name { get; set; }
        public INode<TOwner> Child { get; private set; }

        public void SetChild(INode<TOwner> child) => Child = child;

        public abstract NodeStatus Tick(TOwner owner, float deltaTime);

        public virtual void Reset(TOwner owner)
        {
            if (Child != null) Child.Reset(owner);
        }
    }
}
