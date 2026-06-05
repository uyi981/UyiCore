# UyiCore

Bộ toolkit Unity 2D/3D tái sử dụng được, focus indie action/shooter/roguelite.
Cài qua Unity Package Manager — không copy file vào Assets.

**Version:** 1.0.0 · **Unity:** 2021.3+ · **Dependency:** TextMeshPro

## Install

### Option 1 — Git URL (khuyên dùng)

Mở **Window → Package Manager → + → Add package from git URL** rồi paste:

```
https://github.com/uyi981/UyiCore.git
```

Hoặc lock vào version cụ thể:

```
https://github.com/uyi981/UyiCore.git#v1.0.0
```

### Option 2 — manifest.json

Edit `Packages/manifest.json` trong project:

```json
{
  "dependencies": {
    "com.uyi.core": "https://github.com/uyi981/UyiCore.git#v1.0.0"
  }
}
```

### Option 3 — Local path (dev iteration)

```json
{
  "dependencies": {
    "com.uyi.core": "file:D:/UnityPackages/UyiCore"
  }
}
```

## Cấu trúc package

```
com.uyi.core/
├── package.json
├── README.md
├── Runtime/
│   ├── UyiCore.Runtime.asmdef
│   ├── Singleton/      UyiCore.Patterns       SingletonBehaviour base
│   ├── Observer/       UyiCore.Observer       Event bus typed
│   ├── Pooling/        UyiCore.Pooling        GenericPool + PooledManager
│   ├── Character/      UyiCore.Character      CharacterRoot + Module pattern
│   ├── Audio/          UyiCore.Audio          SFX pool + BGM crossfade
│   ├── UI/             UyiCore.UI             Popup manager + UIButton
│   ├── GameFlow/       UyiCore.GameFlow       Generic Finite State Machine
│   ├── Scene/          UyiCore.Scenes         Bootstrap + Additive scene loader
│   ├── Save/           UyiCore.Save           JSON file save với slot
│   ├── BT/             UyiCore.BT             Behavior Tree fluent builder
│   ├── Timer/          UyiCore.Timing         Schedule callbacks (static API)
│   └── FloatingText/   UyiCore.FloatingText   Damage number + notification pool
└── Editor/
    ├── UyiCore.Editor.asmdef
    └── BootstrapEditorPlayMode.cs    UyiCore.EditorTools (force Bootstrap khi Play)
```

Singleton dùng pattern self-type. Tất cả module dùng C# generic để type-safe.

---

## 1. Singleton — `UyiCore.Patterns`

Base class cho MonoBehaviour singleton. Dùng pattern `class Foo : SingletonBehaviour<Foo>`.
Tự handle duplicate detection và optional DontDestroyOnLoad.

### File
- `SingletonBehaviour.cs`

### API

```csharp
public class GameManager : SingletonBehaviour<GameManager>
{
    protected override void OnAwake()    { /* khởi tạo */ }
    protected override void OnSingletonDestroy() { /* cleanup */ }
}

// Truy cập
GameManager.Instance.DoSomething();
if (GameManager.HasInstance) ...
```

### Lưu ý
- **Override `OnAwake` / `OnSingletonDestroy`** thay vì `Awake` / `OnDestroy` để không quên `base.Awake()`.
- `_dontDestroyOnLoad` flag bật trong inspector nếu singleton sống xuyên scene (xem Scene module để biết pattern Bootstrap thay thế).

---

## 2. Observer — `UyiCore.Observer`

Event bus typed theo enum + struct payload. Static — không cần instance.
Listener register/unregister bằng `AddListener` / `RemoveListener`.

### Files
- `Observer.cs` — `Observer<TEvent>` static class + `IEventData` marker
- `GameEvent.cs` — enum `GameEvent` + payload struct

### API

```csharp
// Định nghĩa event
public enum GameEvent { PlayerHpChanged, EnemyDied, ... }

// Payload struct (implement IEventData)
public readonly struct PlayerHpChangedData : IEventData {
    public readonly int current, max;
    public PlayerHpChangedData(int c, int m) { current = c; max = m; }
}

// Subscribe
void OnEnable() {
    Observer<GameEvent>.AddListener<PlayerHpChangedData>(
        GameEvent.PlayerHpChanged, OnHpChanged);
}
void OnDisable() {
    Observer<GameEvent>.RemoveListener<PlayerHpChangedData>(
        GameEvent.PlayerHpChanged, OnHpChanged);
}
void OnHpChanged(PlayerHpChangedData d) { bar.fillAmount = (float)d.current / d.max; }

// Emit
Observer<GameEvent>.Emit(GameEvent.PlayerHpChanged, new PlayerHpChangedData(80, 100));

// Variant không payload
Observer<GameEvent>.AddListener(GameEvent.PlayerDied, OnDied);
Observer<GameEvent>.Emit(GameEvent.PlayerDied);
```

### Đặc điểm
- Type-safe: payload sai type → log error, không crash.
- `GameEventBootstrap` reset event table khi enter Play (tránh stale listener khi tắt Domain Reload).
- Dùng cho **event** (đã xảy ra). Không dùng cho **state** (đang ở đâu) — đó là việc của FSM.

---

## 3. Pooling — `UyiCore.Pooling`

Pool generic cho component bất kỳ. `PoolDatabase<TEntry>` ScriptableObject làm config,
`PooledManager` singleton sở hữu nhiều pool theo id.

### Files
- `IPoolEntry.cs` — interface cho entry (Id, Prefab, PrewarmCount, MaxConcurrent)
- `IPoolable.cs` — optional callback `OnSpawned` / `OnDespawned`
- `GenericPool.cs` — 1 pool đơn (idle queue + active list, oldest-eviction)
- `PoolDatabase.cs` — ScriptableObject chứa list entry
- `PooledManager.cs` — base singleton quản nhiều pool

### Pattern dùng

```csharp
// 1. Define entry
[Serializable]
public class BulletEntry : IPoolEntry {
    public string id;
    public Bullet prefab;
    public int prewarmCount;
    public int maxConcurrent;
    public string Id => id;
    public GameObject Prefab => prefab.gameObject;
    public int PrewarmCount => prewarmCount;
    public int MaxConcurrent => maxConcurrent;
}

// 2. Define database SO
[CreateAssetMenu] public class BulletDatabase : PoolDatabase<BulletEntry> { }

// 3. Define manager singleton
public class BulletManager : PooledManager<BulletManager, BulletEntry, Bullet>
{
    [SerializeField] private BulletDatabase _db;
    protected override IReadOnlyList<BulletEntry> GetEntries() => _db.Entries;
    protected override void OnInstanceCreated(Bullet b, BulletEntry e) {
        b.Configure(e); // set damage, speed từ entry
    }
}

// 4. Use
var bullet = BulletManager.Instance.Get("fireball");
bullet.transform.position = ...;
// Return tự động khi bullet despawn (caller tự gọi):
BulletManager.Instance.Return("fireball", bullet);
```

### Đặc điểm
- **Oldest-eviction**: pool đầy → bullet cũ nhất bị recycle (game shooter cần).
- **IPoolable hook**: bullet implement `OnSpawned/OnDespawned` để reset state.
- **Prewarm**: instantiate sẵn N item ở Awake — tránh spike lúc gameplay.

---

## 4. Character — `UyiCore.Character`

Pattern composition cho character: 1 `CharacterRoot` chứa nhiều `CharacterModule`.
Mỗi module là 1 MonoBehaviour gắn cùng GameObject (hoặc child), tự logic riêng.

### Files
- `CharacterRoot.cs` — cache reference (Rigidbody2D, Animator, Collider, SpriteRenderer) + collect/tick modules
- `CharacterModule.cs` — base abstract: `Initialize`, `OnTick`, `OnFixedTick`

### Pattern

```csharp
// Define module
public class PlayerMovement : CharacterModule {
    public override void OnTick(float dt) {
        var input = Input.GetAxis("Horizontal");
        Owner.Rigidbody.velocity = new Vector2(input * 5, Owner.Rigidbody.velocity.y);
    }
}

public class PlayerHealth : CharacterModule {
    private int _hp = 100;
    public void TakeDamage(int dmg) {
        _hp -= dmg;
        Observer<GameEvent>.Emit(GameEvent.PlayerHpChanged, new PlayerHpChangedData(_hp, 100));
    }
}

// Setup: gắn CharacterRoot + n module vào GameObject. Root tự discover + Init module ở Awake.

// Cross-module access:
var hp = root.GetModule<PlayerHealth>();
hp.TakeDamage(10);
```

### Đặc điểm
- Module decoupled — không depend nhau qua reference cứng, chỉ qua `Owner.GetModule<T>()`.
- Root forward Tick/FixedTick → module không tự `Update` (centralized loop, dễ disable nhóm).
- `ReinitializeModules()` để reset hết sau respawn.

---

## 5. Audio — `UyiCore.Audio`

`AudioManager` singleton. SFX pool nhiều AudioSource, BGM 2-source crossfade.
Config qua `AudioDatabase` ScriptableObject (clip + volume + pitch range + loop).

### Files
- `AudioDatabase.cs` — SO chứa SFX list + BGM list
- `AudioManager.cs` — singleton

### API

```csharp
// SFX
AudioManager.Instance.PlaySfx("shoot");
AudioManager.Instance.PlaySfxAt("explosion", explosionPos); // 3D spatial

// BGM
AudioManager.Instance.PlayBgm("menu");     // crossfade từ BGM cũ
AudioManager.Instance.StopBgm(fade: 1f);

// Volume (0..1)
AudioManager.Instance.SetMasterVolume(0.8f);
AudioManager.Instance.SetSfxVolume(0.5f);
AudioManager.Instance.SetBgmVolume(0.7f);
```

### Đặc điểm
- **Pool SFX `_sfxSourceCount` AudioSource** — chọn source không playing, fallback source 0.
- **BGM 2-source crossfade**: A → B fade chéo trong `_bgmCrossfade` giây.
- **Pitch random**: entry có `pitchMin/pitchMax` → randomize → variation tự nhiên.

---

## 6. UI — `UyiCore.UI`

`PopupManager` quản popup theo id, instance cache để show/hide nhanh.

### Files
- `PopupDatabase.cs` — SO map id → prefab
- `PopupManager.cs` — singleton
- `UIButton.cs` — wrapper cho Button (optional, có tween/scale on click)

### API

```csharp
PopupManager.Instance.Show("pause");
PopupManager.Instance.Hide("pause");
PopupManager.Instance.HideAll();
bool open = PopupManager.Instance.IsOpen("pause");
```

### Đặc điểm
- Show 2 lần cùng id → bring-to-front (SetAsLastSibling), không spawn duplicate.
- Hide = Destroy GameObject. Nếu cần pool, refactor sau.

---

## 7. GameFlow — `UyiCore.GameFlow`

Finite State Machine generic, non-Mono. Dùng cho game state, AI, character action.

### Files
- `IState.cs` — `IState<TOwner>` + base `State<TOwner>`
- `StateMachine.cs` — `StateMachine<TOwner>` core + Get/ChangeState/Tick/...

### Pattern

```csharp
// Define state
public class PlayingState : State<GameStateMachine> {
    public override void OnEnter(GameStateMachine sm) {
        Time.timeScale = 1f;
        AudioManager.Instance.PlayBgm("level");
    }
    public override void OnExit(GameStateMachine sm) { /* cleanup */ }
}

public class PausedState : State<GameStateMachine> {
    public override void OnEnter(GameStateMachine sm) {
        Time.timeScale = 0f;
        PopupManager.Instance.Show("pause");
    }
    public override void OnExit(GameStateMachine sm) {
        Time.timeScale = 1f;
        PopupManager.Instance.Hide("pause");
    }
}

// Owner sở hữu FSM
public class GameStateMachine : SingletonBehaviour<GameStateMachine> {
    public StateMachine<GameStateMachine> Fsm { get; private set; }

    protected override void OnAwake() {
        base.OnAwake();
        Fsm = new StateMachine<GameStateMachine>(this);
        Fsm.OnStateChanged += (prev, next) =>
            Debug.Log($"{prev?.GetType().Name} → {next.GetType().Name}");
        Fsm.ChangeState<PlayingState>();
    }
    void Update() => Fsm.Tick(Time.unscaledDeltaTime);
}

// Transition
GameStateMachine.Instance.Fsm.ChangeState<PausedState>();
GameStateMachine.Instance.Fsm.RevertToPrevious();  // Paused → Playing
if (Fsm.IsIn<PausedState>()) ...
```

### Đặc điểm
- **Generic `<TOwner>`**: state truy cập owner type-safe, no cast.
- **Cached state instance**: `Get<T>()` lazy-create + cache, no alloc khi đổi state.
- **`CanTransition` delegate**: chặn transition lạ (vd đang GameOver không cho Pause).
- **`OnStateChanged` event**: UI/log subscribe, không cần poll.
- Phân biệt với Observer: Observer = "đã xảy ra X", FSM = "đang ở X".

---

## 8. Scenes — `UyiCore.Scenes`

Scene loading async với fade transition + loading screen. Pattern Bootstrap + Additive.

### Files
- `SceneLoader.cs` — singleton chính
- `SceneTransition.cs` — fade overlay (auto-create runtime hoặc prefab override)
- `LoadOptions.cs` — struct config 1 lần load
- `SceneLoadData.cs` — payload Observer (`SceneLoadStartedData`, `ProgressData`, `CompletedData`)
- `CoreBootstrap.cs` — runtime ensure + editor playModeStartScene

### Pattern Bootstrap + Additive

```
[Bootstrap scene]  ← luôn loaded, chứa các manager singleton (Audio, Popup, GameManager, SceneLoader...)
  ↓
[Menu/Game/...] load ADDITIVE chồng lên Bootstrap, scene cũ unload
```

### API

```csharp
// Đơn giản
SceneLoader.Instance.Load("Game");

// Custom
SceneLoader.Instance.Load("Game", new LoadOptions {
    LoadingSceneName = "Loading",
    UseLoadingScene = true,
    FadeDuration = 0.5f,
    FadeColor = Color.black,
    MinLoadingTime = 1f,
});

// Loading UI subscribe progress
Observer<GameEvent>.AddListener<SceneLoadProgressData>(
    GameEvent.SceneLoadProgress, d => bar.fillAmount = d.progress);
```

### Setup Unity
1. Tạo scene `Bootstrap` chứa tất cả manager + `SceneLoader` component.
2. Tạo scene `Loading` chứa UI bar + script subscribe Observer progress event.
3. Build Settings: Bootstrap index 0, các scene khác sau.
4. Editor: `BootstrapEditorPlayMode` tự force Play mode bắt đầu từ Bootstrap.

### Lưu ý
- Bootstrap không bao giờ unload → singleton sống luôn, không cần DontDestroyOnLoad.
- Code `SetActiveScene` sau load để lighting/skybox theo scene gameplay, không phải Bootstrap.

---

## 9. Save — `UyiCore.Save`

JSON file-based save với multi-slot. Static API, không cần GameObject.

### Files
- `SaveSystem.cs` — static API
- `SaveOptions.cs` — config (obfuscation, subdir, extension)
- `SaveMeta.cs` — struct metadata
- `SaveEnvelope.cs` — internal wrapper

### API

```csharp
// User định nghĩa data class
[Serializable]
public class MyGameData {
    public int level;
    public int coins;
    public float playtime;
}

// Save / Load slot
SaveSystem.Save(slot: 0, myData, label: "Wave 12");
var data = SaveSystem.Load<MyGameData>(0);

// Slot management
SaveSystem.Exists(0);
SaveSystem.Delete(0);
SaveSystem.Copy(fromSlot: 0, toSlot: 1);
SaveSystem.DeleteAll();

// Auto-save (file riêng)
SaveSystem.SaveAuto(myData);
var auto = SaveSystem.LoadAuto<MyGameData>();

// Settings (không theo slot)
SaveSystem.SaveSettings(mySettings);
var s = SaveSystem.LoadSettings<MySettings>();

// Slot list cho UI Continue
foreach (var m in SaveSystem.ListSlots()) {
    Debug.Log($"Slot {m.slot}: {m.label} - {m.TimestampLocal}");
}

// Config (gọi 1 lần ở Bootstrap, optional)
SaveSystem.Configure(new SaveOptions {
    Obfuscate = true,
    ObfuscationKey = "your-game-key",
    Subdirectory = "TurtleSaves",
    FileExtension = ".sav",
});
```

### File layout

```
{Application.persistentDataPath}/Saves/
  save_0.json       slot 0 (full data + envelope)
  save_1.json       slot 1
  save_auto.json    auto-save
  settings.json     settings (cross-slot)
```

### Đặc điểm
- **Envelope wrap**: `{version, timestamp, label, data}` — đọc meta nhanh không parse full.
- **MetaProbe trick**: ListSlots parse chỉ field meta, bỏ qua data field.
- **Obfuscation XOR + Base64**: deter casual edit, không phải security.
- **Robust**: file corrupt → return null + log, không crash.

### Hạn chế
- `JsonUtility` không support Dictionary, polymorphism, null array — design save model phẳng.
- Migration thủ công — check `meta.version` rồi tự xử lý.

---

## 10. BT — `UyiCore.BT`

Behavior Tree generic + fluent builder. Cho enemy AI / boss / NPC.

### Files
- `Node.cs` — `NodeStatus`, `INode<T>`, base classes
- `Blackboard.cs` — typed key-value
- `Composites.cs` — Sequence, Selector, Parallel
- `Decorators.cs` — Inverter, Repeater, Cooldown, UntilSuccess, UntilFailure
- `Leaves.cs` — Action, SimpleAction (Do), Condition, Wait, Succeed, Fail
- `BehaviorTree.cs` — `BehaviorTree<T>` wrapper + `BT.Build()` builder

### API

```csharp
public class EnemyController : MonoBehaviour {
    public Transform Player;
    public float DistanceToPlayer => Vector3.Distance(transform.position, Player.position);

    BehaviorTree<EnemyController> _bt;

    void Start() {
        _bt = BT.Build<EnemyController>(this)
            .Selector("Root")
                // Nhánh 1: HP thấp → trốn
                .Sequence("Flee")
                    .Condition(e => e.GetComponent<EnemyHealth>().HpPercent < 0.2f)
                    .Action(e => { e.Flee(); return NodeStatus.Running; })
                .End()

                // Nhánh 2: gần → đánh, có cooldown
                .Sequence("Attack")
                    .Condition(e => e.DistanceToPlayer < 5f)
                    .Cooldown(0.8f)
                        .Do(e => e.Attack())
                    .End()
                .End()

                // Nhánh 3: thấy player → chase
                .Sequence("Chase")
                    .Condition(e => e.CanSeePlayer())
                    .Action(e => {
                        e.MoveTo(e.Player.position);
                        return e.DistanceToPlayer < 4f
                            ? NodeStatus.Success : NodeStatus.Running;
                    })
                .End()

                // Default: tuần tra
                .Do(e => e.Patrol())
            .End()
            .Build();

        _bt.TickInterval = 0.1f; // perf: enemy xa tick 10/s thay vì 60
    }

    void Update() => _bt.Tick(Time.deltaTime);
}
```

### Blackboard

```csharp
_bt.Blackboard.Set("lastKnownPos", playerPos);
var pos = _bt.Blackboard.Get<Vector3>("lastKnownPos");
```

### Đặc điểm
- **`.End()` cho mọi composite + decorator** — predictable, không magic auto-pop.
- **Tree auto-reset** sau Success/Failure → next tick chạy lại từ đầu.
- **`OnTreeCompleted` event** — fire khi root kết thúc.
- **TickInterval** — optimize enemy xa player.
- Skip phase 1: service node, visual debug, random selector.

---

## 11. Timer — `UyiCore.Timing`

Schedule callback theo thời gian. Static API, runner singleton auto-spawn.

### File
- `Timer.cs` — `Timer` static + `TimerHandle` struct + internal `TimerRunner`

### API

```csharp
// One-shot
Timer.After(2f, () => SpawnBoss());

// Repeating
Timer.Every(0.5f, () => Tick());                    // vô hạn
Timer.Every(0.5f, () => Burst(), repeatCount: 5);   // 5 lần

// Cancel
var h = Timer.After(3f, () => DoLater());
h.Cancel();

// Pause-resistant (timeScale = 0 vẫn chạy — cho UI/popup)
Timer.After(1f, () => HideToast(), unscaled: true);

// Auto-cancel khi GameObject bị destroy (tránh NullRef)
Timer.After(2f, () => transform.position = ..., owner: this);

// Tiện ích
Timer.CancelAll();
int n = Timer.ActiveCount;
```

### Đặc điểm
- **Auto-spawn runner** qua `[RuntimeInitializeOnLoadMethod]` — không cần kéo vào scene.
- **Owner check** dùng Unity fake-null → object destroyed = timer tự cancel.
- **Try/catch callback** — 1 timer crash không kéo cả list.
- **Snapshot count khi tick** — timer add trong callback đợi frame sau, tránh infinite loop.

---

## 12. FloatingText — `UyiCore.FloatingText`

Pool damage number + pickup notification. World-space text với motion + fade.

### Files
- `FloatingTextStyle.cs` — preset struct
- `FloatingTextItem.cs` — MonoBehaviour 1 item (motion + alpha curve + scale curve)
- `FloatingTextService.cs` — singleton pool + static API

### Setup Unity (1 lần)
1. Tạo prefab `FloatingTextItem.prefab`:
   - Root: RectTransform + `FloatingTextItem` + `CanvasGroup`
   - Child hoặc cùng: `TextMeshProUGUI`
   - Inspector: gán field `_label` (TMP) + `_group` (CanvasGroup)
2. Thêm `FloatingTextService` component vào Bootstrap (hoặc gameplay scene).
3. Kéo prefab vào field `_prefab`. Tweak `_canvasScale` theo pixel-art scale (~0.01-0.05).

### API

```csharp
// Generic
FloatingTextService.Show("100", hitPos);
FloatingTextService.Show("MISS", hitPos, FloatingTextStyle.Crit);

// Shortcut
FloatingTextService.ShowDamage(100, enemy.transform.position);
FloatingTextService.ShowDamage(250, enemy.transform.position, crit: true);
FloatingTextService.ShowHeal(50, player.transform.position);
FloatingTextService.ShowPickup("+10 Coins", pickupPos);

// Custom style
var style = new FloatingTextStyle {
    color = Color.cyan,
    fontSize = 6f,
    velocity = new Vector2(0, 3f),
    lifetime = 1.5f,
    spawnJitter = new Vector2(0.3f, 0),
};
FloatingTextService.Show("Combo x3", pos, style);
```

### Đặc điểm
- **World-space canvas auto-create** — không cần setup canvas thủ công.
- **Pool prewarm + maxConcurrent** — evict oldest khi quá tải.
- **Animation curve cho alpha + scale** — designer tweak trong inspector item prefab.
- **Spawn jitter** — 2 damage cùng frame không chồng.
- **Preset style sẵn**: Default / Damage / Crit / Heal / Pickup.

---

## Patterns chung

### Singleton vs Static
- **Singleton MonoBehaviour** (Audio, Popup, SceneLoader, FloatingText): cần serialize field trong inspector (database, prefab), sống trong Bootstrap.
- **Static API + auto-spawn runner** (Timer): không cần config, plug-and-play.
- **Static thuần** (Observer, SaveSystem): không stateful runtime, dùng được từ Editor script.

### Observer vs FSM
- Observer = "event đã xảy ra" (PlayerHpChanged, EnemyDied) — broadcast.
- FSM = "đang ở state nào" (Menu/Playing/Paused) — exclusive, có OnEnter/OnExit symmetric.
- Best practice: State.OnEnter Emit Observer event để loose-coupled UI/analytics react.

### Pool vs Instantiate
- Spawn lặp đi lặp lại (bullet, enemy, vfx, floating text) → pool.
- Một lần (boss, level prop) → Instantiate.

### Bootstrap pattern
- Scene `Bootstrap` chứa tất cả manager — không bao giờ unload.
- Các scene khác load Additive chồng lên.
- Editor convenience: `BootstrapEditorPlayMode` force Play start từ Bootstrap.

---

## Reuse sang project khác

1. Copy thư mục `Core/` qua project mới.
2. Module độc lập gần như hoàn toàn — chỉ cần `Singleton/` cho các module có singleton, `Observer/` cho event-emitting module.
3. Tạo scene `Bootstrap` + setup manager prefab (Audio, Popup, SceneLoader, FloatingText) — kéo database SO + prefab vào.
4. Tạo `GameEvent` enum + payload struct riêng cho project mới (xóa cái cũ).
5. Định nghĩa `SaveData` class theo nhu cầu game.
6. Dùng FSM / BT cho gameplay logic.

Modules **không phụ thuộc gì khác**: Save, Timer, Pooling (core), BT.
Modules **phụ thuộc Singleton**: Audio, Popup, SceneLoader, FloatingText, PooledManager.
Modules **phụ thuộc Observer**: SceneLoader (emit progress), GameEvent (project-specific).
