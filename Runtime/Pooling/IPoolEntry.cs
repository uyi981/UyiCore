using UnityEngine;

namespace UyiCore.Pooling
{
    public interface IPoolEntry
    {
        string Id { get; }
        GameObject Prefab { get; }
        int PrewarmCount { get; }
        int MaxConcurrent { get; }
    }
}
