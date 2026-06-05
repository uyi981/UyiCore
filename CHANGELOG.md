# Changelog

Tất cả thay đổi đáng kể của UyiCore.

## [1.0.0] - 2026-06-06

### Added
- **Singleton** — `SingletonBehaviour<T>` self-type base class.
- **Observer** — `Observer<TEvent>` static event bus typed payload.
- **Pooling** — `GenericPool`, `PooledManager`, `PoolDatabase` SO config.
- **Character** — `CharacterRoot` + `CharacterModule` composition pattern.
- **Audio** — SFX pool + BGM 2-source crossfade, qua `AudioDatabase` SO.
- **UI** — `PopupManager` qua id, `UIButton` wrapper.
- **GameFlow** — `StateMachine<TOwner>` generic FSM.
- **Scenes** — `SceneLoader` Bootstrap + Additive pattern, fade transition.
- **Save** — JSON file save với slot, auto-save, settings tách riêng.
- **BT** — Behavior Tree fluent builder, composite/decorator/leaf nodes.
- **Timer** — `Timer` static API, auto-spawn runner.
- **FloatingText** — Damage number + notification pool, world-space canvas.
- UPM package structure với Runtime/Editor asmdef.
- Editor convenience: `BootstrapEditorPlayMode` force Play start từ Bootstrap scene.
