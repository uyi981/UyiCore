using UyiCore.Observer;
using UnityEngine;

/// <summary>
/// Tất cả event toàn cục của game đi qua Observer&lt;GameEvent&gt;.
/// Payload đa tham số dùng struct implement IEventData.
/// </summary>
public enum GameEvent
{
    StateChanged,
    PlayerHpChanged,
    PlayerExpChanged,
    PlayerLevelUp,
    PlayerDied,
    AnyEnemyDied,
    AllWavesCompleted,
    SceneLoadStarted,
    SceneLoadProgress,
    SceneLoadCompleted,
}

public readonly struct StateChangedData : IEventData
{
    public readonly GameState oldState;
    public readonly GameState newState;
    public StateChangedData(GameState oldState, GameState newState)
    {
        this.oldState = oldState;
        this.newState = newState;
    }
}

public readonly struct PlayerHpChangedData : IEventData
{
    public readonly int current;
    public readonly int max;
    public PlayerHpChangedData(int current, int max) { this.current = current; this.max = max; }
}

public readonly struct PlayerExpChangedData : IEventData
{
    public readonly int current;
    public readonly int toNext;
    public PlayerExpChangedData(int current, int toNext) { this.current = current; this.toNext = toNext; }
}

public readonly struct PlayerLevelUpData : IEventData
{
    public readonly int newLevel;
    public PlayerLevelUpData(int newLevel) { this.newLevel = newLevel; }
}

public readonly struct EnemyDiedData : IEventData
{
    public readonly EnemyHealth enemy;
    public readonly BulletPayload lastHit;
    public EnemyDiedData(EnemyHealth enemy, BulletPayload lastHit)
    {
        this.enemy = enemy;
        this.lastHit = lastHit;
    }
}

/// <summary>
/// Observer table là static — clear khi vào play mode để không giữ listener stale
/// giữa các lần play trong editor (khi Domain Reload bị tắt).
/// </summary>
internal static class GameEventBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        Observer<GameEvent>.ClearAll();
    }
}
