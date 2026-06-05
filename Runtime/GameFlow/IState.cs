namespace UyiCore.GameFlow
{
    /// <summary>
    /// State trong <see cref="StateMachine{TOwner}"/>. TOwner = kiểu của object sở hữu FSM
    /// (vd GameStateMachine, EnemyController, CharacterRoot) — state truy cập owner type-safe.
    /// </summary>
    public interface IState<TOwner>
    {
        void OnEnter(TOwner owner);
        void OnExit(TOwner owner);
        void OnTick(TOwner owner, float deltaTime);
        void OnFixedTick(TOwner owner, float fixedDeltaTime);
    }

    /// <summary>Base rỗng để subclass chỉ override cái cần.</summary>
    public abstract class State<TOwner> : IState<TOwner>
    {
        public virtual void OnEnter(TOwner owner) { }
        public virtual void OnExit(TOwner owner) { }
        public virtual void OnTick(TOwner owner, float deltaTime) { }
        public virtual void OnFixedTick(TOwner owner, float fixedDeltaTime) { }
    }
}
