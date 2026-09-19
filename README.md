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
│   ├── Audio/          UyiCore.Audio          SFX pool + BGM crossfade
│   ├── UI/             UyiCore.UI             Popup manager
│   ├── FSM/            UyiCore.FSM            Generic Finite State Machine
│   ├── Scene/          UyiCore.Scenes         Bootstrap + Additive scene loader
│   ├── Save/           UyiCore.Save           JSON file save với slot
│   ├── BT/             UyiCore.BT             Behavior Tree fluent builder + data-driven (SO/JSON)
│   ├── Timer/          UyiCore.Timing         Schedule callbacks (static API)
│   ├── Tween/          UyiCore.Tweening       Tween engine + UI tween helpers + easing
│   ├── UIEffect/       UyiCore.UIEffect       UI effect preset (SO) + component kéo-thả
│   └── Input/          UyiCore.Input          Facade cho Unity Input System (asmdef riêng, optional)
└── Editor/
    └── UyiCore.Editor.asmdef         UyiCore.EditorTools (BT/UI importer + Tools menu)
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

## 4. Audio — `UyiCore.Audio`

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

## 5. UI — `UyiCore.UI`

`PopupManager` quản popup theo id, instance cache để show/hide nhanh.

### Files
- `PopupDatabase.cs` — SO map id → prefab
- `PopupManager.cs` — singleton

### API

```csharp
PopupManager.Instance.Show("pause");
PopupManager.Instance.Hide("pause");
PopupManager.Instance.HideAll();
bool open = PopupManager.Instance.IsOpen("pause");

// Giải phóng bộ nhớ thật sự (mất state, Show sau tạo mới)
PopupManager.Instance.Free("pause");
PopupManager.Instance.FreeAll();
```

### Đặc điểm
- **Pooled**: `Show`/`Hide` = SetActive true/false, instance cache lại → **giữ nguyên state** (input, scroll...) + không GC churn.
- Show 2 lần cùng id → bring-to-front (SetAsLastSibling), không spawn duplicate.
- `Free` / `FreeAll` để Destroy thật khi cần giải phóng bộ nhớ.

---

## 6. FSM — `UyiCore.FSM`

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

## 7. Scenes — `UyiCore.Scenes`

Scene loading async với fade transition + loading screen. Pattern Bootstrap + Additive.

### Files
- `SceneLoader.cs` — singleton chính
- `SceneTransition.cs` — fade overlay (auto-create runtime hoặc prefab override)
- `LoadOptions.cs` — struct config 1 lần load
- `SceneLoadData.cs` — payload event (`SceneLoadStartedData`, `ProgressData`, `CompletedData`)
- `LoadingScreen.cs` — helper gắn trong scene Loading: tự cập nhật bar theo progress
- `CoreBootstrap.cs` — runtime ensure: tự load Bootstrap additive nếu scene khởi động chưa có nó

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
2. Tạo scene `Loading` chứa UI bar → gắn `LoadingScreen`, kéo Image (Type = Filled) vào `_bar` (tự cập nhật, khỏi viết code). Muốn hiện % thì wire event `_onProgress`.
3. Build Settings: Bootstrap index 0, các scene khác sau.
4. Bấm Play ở scene nào cũng được — `CoreBootstrap` (runtime) tự load Bootstrap additive nếu scene hiện tại chưa có nó (cần Bootstrap nằm trong Build Settings). Lưu ý: manager Awake trễ 1 frame so với scene gameplay.

### Lưu ý
- Bootstrap không bao giờ unload → singleton sống luôn, không cần DontDestroyOnLoad.
- Code `SetActiveScene` sau load để lighting/skybox theo scene gameplay, không phải Bootstrap.

---

## 8. Save — `UyiCore.Save`

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

## 9. BT — `UyiCore.BT`

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

Kho state chia sẻ giữa các node + code ngoài (mỗi owner 1 blackboard riêng).
```csharp
// code ngoài
_bt.Blackboard.Set("lastKnownPos", playerPos);
var pos = _bt.Blackboard.Get<Vector3>("lastKnownPos");

// trong registry (data-driven) — lambda nhận thêm Blackboard:
reg.Condition("HasTarget", (e, bb, p) => bb.Has("target"))
   .Do("PickTarget",       (e, bb, p) => bb.Set("target", e.FindTarget()));
```

### Đặc điểm
- **`.End()` cho mọi composite + decorator** — predictable, không magic auto-pop.
- **Loop / one-shot** — mặc định xong Success/Failure là Reset chạy lại; `bt.Loop = false` + `bt.Stop()` để dừng hẳn.
- **Reactive / abort** — `ReactiveSelector` / `ReactiveSequence`: nhánh ưu tiên cao chạy được sẽ cắt ngang nhánh đang chạy (builder `.ReactiveSelector()` / `.ReactiveSequence()`).
- **`OnTreeCompleted` event** — fire khi root kết thúc.
- **TickInterval** — optimize enemy xa player.
- **Debugger** editor (xem cuối mục Data-driven).

### Data-driven (SO + JSON) — `UyiCore.BT.Data`

Ngoài fluent builder (code-first), BT còn dựng được từ **ScriptableObject** — thiết kế cây bằng tool ngoài (HTML) → export JSON → import ra SO. Engine runtime giữ nguyên; đây chỉ là tầng data build lên trên.

**Files** (`Runtime/BT/Data/`)
- `BTNodeData.cs` — node dạng data (`id/type/ref/children/params`) + `BTParams` (đọc param có kiểu, parse InvariantCulture) + `BTGraphJson` (DTO khớp JSON)
- `BehaviorTreeAsset.cs` — SO chứa cây + `FromJson()` + `Validate()`
- `BTRegistry.cs` — map `ref` (string) → hành vi C# thật (vì lambda không serialize được)
- `BehaviorTreeCompiler.cs` — dịch asset → cây `BehaviorTree<TOwner>` runtime
- `Editor/BehaviorTreeJsonImporter.cs` — `ScriptedImporter` cho file `*.btjson`

**Schema JSON** (JsonUtility-friendly: `children`/`params` để dạng mảng)

```json
{ "name":"GruntAI", "root":"root",
  "nodes":[
    {"id":"root","type":"Selector","children":["flee","patrol"]},
    {"id":"flee","type":"Sequence","children":["c","d"]},
    {"id":"c","type":"Condition","ref":"HpBelow","params":[{"key":"threshold","value":"0.2"}]},
    {"id":"d","type":"Do","ref":"Flee"},
    {"id":"patrol","type":"Do","ref":"Patrol"}
  ]}
```

`type` cấu trúc: `Selector/Sequence/ReactiveSelector/ReactiveSequence/Parallel/Inverter/Repeater/Cooldown/UntilSuccess/UntilFailure/Wait/Succeed/Fail`.
`type` leaf bind registry: `Action` (trả NodeStatus) · `Condition` (bool) · `Do` (chạy rồi Success) — dùng `ref` trỏ id đăng ký.

**Dùng**

```csharp
// 1. Registry map ref → code (khai 1 lần cho mỗi loại owner)
var reg = new BTRegistry<Enemy>()
    .Condition("HpBelow", (e, p) => e.HpPercent < p.GetFloat("threshold", 0.2f))
    .Do("Flee",   e => e.Flee())
    .Do("Patrol", e => e.Patrol())
    .Action("Chase", e => e.MoveTo(e.Player.position) ? NodeStatus.Success : NodeStatus.Running);

// 2. Compile asset → cây runtime (mỗi enemy 1 cây state riêng)
_bt = BehaviorTreeCompiler.Compile(_treeAsset, this, reg);
void Update() => _bt.Tick(Time.deltaTime);
```

**Workflow**: thiết kế bằng tool HTML → export `EnemyAI.btjson` → thả vào `Assets/` (auto ra SO) → kéo SO vào field owner. Ref chưa đăng ký → node fail + log rõ. Sample: `Samples~/DataDrivenBT/`.

> Fluent builder và SO **cùng biên dịch ra 1 cây runtime** — dùng song song thoải mái.

### Debugger (editor)

Compile với `debug: true` → vào **Play** → **Tools ▸ UyiCore ▸ Behavior Tree Debugger**, chọn cây trong dropdown → canvas tô màu node theo status **live**: 🟡 Running · 🟢 Success · 🔴 Failure · mờ = không tick frame này. Tắt debug (mặc định) = zero cost.

```csharp
_bt = BehaviorTreeCompiler.Compile(asset, this, reg, blackboard: null, debug: true);
```

---

## 10. Timer — `UyiCore.Timing`

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

## 11. Tween — `UyiCore.Tweening`

Tween engine tối giản (generic) + helper cho UI, có easing. Static API kiểu Timer.

### Files
- `Ease.cs` — enum `Ease` + `Easing.Evaluate` (Quad/Cubic/Back/Bounce/Elastic/Sine...)
- `Tween.cs` — `Tween.To(dur, onUpdate, ease...)` + `TweenHandle`, runner auto-spawn, tự cancel khi owner destroy
- `UITween.cs` — extension: `Fade` / `ScaleTo` / `MoveAnchored` / `RotateTo` / `ColorTo` / `FillTo` / `Punch`

### API
```csharp
rect.ScaleTo(1f, .2f, Ease.OutBack);       // pop
group.Fade(0f, .3f);                        // fade CanvasGroup
hpBar.FillTo(hp01, .25f, Ease.OutCubic);    // thanh máu chạy mượt
rect.Punch(1.15f, .25f);                    // nảy 1 phát (click/hit)

// tổng quát — dùng cho cả gameplay
Tween.To(1f, t => enemy.localScale = Vector3.LerpUnclamped(a, b, t), Ease.OutBack, owner: enemy);
```

### Đặc điểm
- **`unscaled = true` mặc định** cho UI helper (chạy cả khi timeScale = 0).
- **Handle** để `Cancel()` / `Complete()`; **tự huỷ** khi target GameObject destroy (fake-null).
- **One-shot**, không loop (giữ core nhỏ). Ease overshoot (OutBack/Elastic) → dùng `LerpUnclamped`.
- Generic — không khoá vào UI.

---

## 12. UIEffect — `UyiCore.UIEffect`

Hiệu ứng UI theo pattern **tách WHAT / HOW / WHEN**: kéo component + chọn preset SO, khỏi sửa code UI.

### Files
- `UiEffectPreset.cs` — SO "cảm giác" (kind + duration + ease + tham số). Đổi 1 SO → cả game đổi feel
- `UiEffectPlayer.cs` — component kéo lên UI → chơi preset khi trigger (OnEnable/OnStart/Manual), target auto = chính nó
- `ButtonFeedback.cs` — component kéo lên nút → nhấn thu nhỏ / thả bung nảy

### Dùng
1. Tạo `UiEffectPreset` (Create ▸ UyiCore ▸ UI Effect Preset): kind = Scale, ease = OutBack → "PopupIn"
2. Kéo `UiEffectPlayer` lên panel, gán preset → tự chơi khi bật (khỏi code)
3. Kéo `ButtonFeedback` lên các nút → có feedback bấm ngay

```csharp
GetComponent<UiEffectPlayer>().Play();   // hoặc chơi bằng code
```

### Đặc điểm
- **Non-invasive**: gắn bằng component, không đụng logic UI.
- **Preset SO** = feel đồng bộ + tune 1 chỗ.
- Dựa hết trên **Tween** (tầng nền) — cancel/auto-huỷ theo.

---

## 13. Input — `UyiCore.Input`

Facade tĩnh **bọc Unity Input System** cho gọn (giống `Timer`/`SaveSystem`). Nguồn dữ liệu vẫn là 1 `InputActionAsset` (.inputactions) — bạn vẫn vẽ map/gán phím trong Unity. Wrapper chỉ đỡ boilerplate + gom lifecycle + nối rebind vào Save.

> **Optional dependency:** module nằm ở asmdef riêng `UyiCore.Input` với `defineConstraints: ENABLE_INPUT_SYSTEM`. Cần cài gói **Input System** và set *Active Input Handling = Both / Input System Package* (Project Settings ▸ Player). Không có gói → module tự ẩn, phần còn lại của UyiCore vẫn build.
>
> Hợp **single-player / online** (mỗi máy 1 người local). **Couch co-op** (nhiều người chung máy) phải dùng `PlayerInput` riêng — facade tĩnh này không hợp.

### Files
- `UyiInput.cs` — facade tĩnh: poll / event / context / rebind / device scheme
- `InputScheme.cs` — enum `KeyboardMouse / Gamepad / Touch`

### Setup Unity
1. Package Manager → cài **Input System**; Active Input Handling → **Both** hoặc **Input System Package**.
2. **Create ▸ Input Actions** → thêm map `Gameplay`/`UI` + action (`Move` = Value/Vector2, `Jump`, `Fire`…).
3. Ở **Bootstrap** gán asset và init:
```csharp
[SerializeField] InputActionAsset inputActions;
void Awake() => UyiInput.Init(inputActions, defaultMap: "Gameplay");
```

### API
```csharp
// Poll (đọc trong Update)
Vector2 move = UyiInput.Axis2D("Move");
if (UyiInput.Pressed("Jump"))  Jump();
if (UyiInput.Held("Fire"))     Fire();
if (UyiInput.Released("Aim"))  StopAim();

// Event (UI / one-shot)
UyiInput.OnPerformed("Pause", _ => TogglePause());
UyiInput.Off("Pause", handler);          // gỡ khi không cần

// Context (action map) — mở menu thì tắt input gameplay
UyiInput.SwitchMap("UI");                // bật 1 map, tắt các map khác
UyiInput.EnableMap("Gameplay");

// Thiết bị → đổi icon phím gợi ý
if (UyiInput.Scheme == InputScheme.Gamepad) ShowPadIcons();
UyiInput.OnSchemeChanged += RefreshPrompts;

// Rebind + lưu (tự nối SaveSystem)
UyiInput.StartRebind("Jump", onComplete: RefreshLabel);
UyiInput.ResetBindings();                // về mặc định
string key = UyiInput.BindingDisplay("Jump");   // "Space"
```

### Netcode
Đọc input chỉ ở object mình sở hữu:
```csharp
void Update() {
    if (!IsOwner) return;                 // NGO / Netcode
    var v = UyiInput.Axis2D("Move");
    if (UyiInput.Pressed("Jump")) Jump(); // client-authoritative hoặc gửi ServerRpc
}
```

### Đặc điểm
- **Cache `InputAction` theo id** — lookup 1 lần rồi tái dùng.
- **Không leak listener**: `OnPerformed/Off` + tự dọn khi `Teardown`/vào Play (reset static giống Observer).
- **Rebind lưu qua SaveSystem** (`Save("input_rebinds", …)` — file riêng, không đè settings).
- Thò xuống thô được qua `UyiInput.Asset`.

### Hạn chế
- **String id gõ sai không báo compile** → nên gom hằng số: `public static class InputIds { public const string Move="Move", Jump="Jump"; }`.
- 1 input toàn cục → không dùng cho couch co-op chia màn hình.

---

## Patterns chung

### Singleton vs Static
- **Singleton MonoBehaviour** (Audio, Popup, SceneLoader): cần serialize field trong inspector (database, prefab), sống trong Bootstrap.
- **Static API + auto-spawn runner** (Timer): không cần config, plug-and-play.
- **Static thuần** (Observer, SaveSystem): không stateful runtime, dùng được từ Editor script.

### Observer vs FSM
- Observer = "event đã xảy ra" (PlayerHpChanged, EnemyDied) — broadcast.
- FSM = "đang ở state nào" (Menu/Playing/Paused) — exclusive, có OnEnter/OnExit symmetric.
- Best practice: State.OnEnter Emit Observer event để loose-coupled UI/analytics react.

### Pool vs Instantiate
- Spawn lặp đi lặp lại (bullet, enemy, vfx) → pool.
- Một lần (boss, level prop) → Instantiate.

### Bootstrap pattern
- Scene `Bootstrap` chứa tất cả manager — không bao giờ unload.
- Các scene khác load Additive chồng lên.
- Bấm Play ở scene nào cũng được: `CoreBootstrap` (runtime) tự load Bootstrap additive nếu thiếu.

---

## Reuse sang project khác

1. Copy thư mục `Core/` qua project mới.
2. Module độc lập gần như hoàn toàn — chỉ cần `Singleton/` cho các module có singleton, `Observer/` cho event-emitting module.
3. Tạo scene `Bootstrap` + setup manager prefab (Audio, Popup, SceneLoader) — kéo database SO + prefab vào.
4. Tạo `GameEvent` enum + payload struct riêng cho project mới (xóa cái cũ).
5. Định nghĩa `SaveData` class theo nhu cầu game.
6. Dùng FSM / BT cho gameplay logic.

Modules **không phụ thuộc gì khác**: Save, Timer, Pooling (core), BT.
Modules **phụ thuộc Singleton**: Audio, Popup, SceneLoader, PooledManager.
Modules **phụ thuộc Observer**: SceneLoader (emit progress), GameEvent (project-specific).
