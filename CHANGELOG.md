# Changelog

Tất cả thay đổi đáng kể của UyiCore.

## [Unreleased]

### Added
- **Tween** (`UyiCore.Tweening`) — engine tween generic (static API + runner tự spawn, easing, handle cancel, tự huỷ khi owner destroy) + extension UI (`Fade/ScaleTo/MoveAnchored/RotateTo/ColorTo/FillTo/Punch`).
- **UIEffect** (`UyiCore.UIEffect`) — `UiEffectPreset` (SO tách feel) + `UiEffectPlayer` (kéo-thả chơi preset) + `ButtonFeedback`. Pattern tách WHAT/HOW/WHEN, non-invasive.
- **Scenes/LoadingScreen** — helper gắn trong scene Loading: tự subscribe progress → cập nhật Image bar, kèm event `_onProgress` (0..1) cho text %/spinner.
- **BT data-driven** — `BehaviorTreeAsset` (SO), `BTRegistry`, `BehaviorTreeCompiler`, importer `*.btjson` (`Runtime/BT/Data/` + Editor).
- **BT visual editor** — tool HTML node-graph (`Tools~/BehaviorTreeEditor`) export `.btjson`, kèm panel giải thích văn bản + hướng dẫn.
- **UI layout data-driven** — `UiPrefabBuilder` dựng prefab UGUI (Image + TMP text) từ `*.uijson`; menu `Tools ▸ UyiCore ▸ Build UI Prefab from JSON`.
- **UI layout visual editor** — tool HTML kéo-thả (`Tools~/UiLayoutEditor`) WYSIWYG: snap/grid/align, undo/redo, nudge, export `.uijson`.
- **Tools menu** — `Tools ▸ UyiCore ▸ Open BT / UI Editor` mở tool HTML.
- **Prefab Brush** (`Tools ▸ UyiCore ▸ Prefab Brush`) — vẽ prefab lên Scene: multi-prefab random, brush size/density/spacing, đặt lên Collider/GroundY/2D, align normal, random xoay/scale, parent, xoá (Ctrl), undo.
- Sample data-driven BT (`Samples~/DataDrivenBT`) + UI layout (`Samples~/UiLayout`).

### Fixed
- **Audio** — BGM volume slider chỉnh nhầm source đang phát (track `_activeBgm` thay vì suy từ cờ toggle).
- **Observer / Scenes** — static event không reset khi vào Play → stale listener khi tắt Reload Domain. Thêm hook reset.
- **Pooling** — `NullReferenceException` khi `MaxConcurrent = 0` (giờ coi `<= 0` là không giới hạn).
- **Save** — ghi atomic (`.tmp` + `File.Replace`), crash giữa chừng không mất save cũ.

### Changed
- Rename **GameFlow → FSM** (namespace `UyiCore.FSM`, folder `Runtime/FSM`).
- **UI/PopupManager** — pooled: `Show`/`Hide` = SetActive thay Instantiate/Destroy (giữ state, hết GC churn); thêm `Free`/`FreeAll` để huỷ hẳn.

### Removed
- Editor force-play (`BootstrapEditorPlayMode`) — bỏ ép Play từ Bootstrap; runtime `CoreBootstrap` vẫn tự load Bootstrap additive khi thiếu.
- **Character** module (`CharacterRoot`/`CharacterModule`) — pattern opinionated, 2D-only.
- **FloatingText** module — thiên feature game + trùng logic pooling.
- **UIButton** — wrapper Button quá mỏng.

## [1.0.0] - 2026-06-06

### Added
- **Singleton** — `SingletonBehaviour<T>` self-type base class.
- **Observer** — `Observer<TEvent>` static event bus typed payload.
- **Pooling** — `GenericPool`, `PooledManager`, `PoolDatabase` SO config.
- **Audio** — SFX pool + BGM 2-source crossfade, qua `AudioDatabase` SO.
- **UI** — `PopupManager` qua id.
- **FSM** — `StateMachine<TOwner>` generic finite state machine.
- **Scenes** — `SceneLoader` Bootstrap + Additive pattern, fade transition.
- **Save** — JSON file save với slot, auto-save, settings tách riêng.
- **BT** — Behavior Tree fluent builder, composite/decorator/leaf nodes.
- **Timer** — `Timer` static API, auto-spawn runner.
- UPM package structure với Runtime/Editor asmdef.
