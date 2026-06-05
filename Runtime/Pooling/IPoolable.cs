namespace UyiCore.Pooling
{
    /// <summary>Optional callbacks for components that want spawn/despawn hooks.</summary>
    public interface IPoolable
    {
        string PoolId { get; }
        void OnSpawned();
        void OnDespawned();
    }
}
