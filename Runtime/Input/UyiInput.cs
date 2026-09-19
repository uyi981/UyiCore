#if ENABLE_INPUT_SYSTEM
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UyiCore.Save;

namespace UyiCore.Input
{
    /// <summary>
    /// Facade tĩnh bọc Unity Input System cho gọn (giống <c>Timer</c>/<c>SaveSystem</c>).
    /// Nguồn dữ liệu vẫn là 1 <see cref="InputActionAsset"/> (.inputactions) — gán ở Bootstrap
    /// qua <see cref="Init"/>. Hợp single-player / online (mỗi máy 1 người local); KHÔNG hợp
    /// couch co-op (nhiều người 1 máy → dùng PlayerInput riêng).
    ///
    /// <code>
    /// // Bootstrap:
    /// UyiInput.Init(inputActionsAsset, defaultMap: "Gameplay");
    ///
    /// // Gameplay (chặn IsOwner nếu netcode):
    /// var move = UyiInput.Axis2D("Move");
    /// if (UyiInput.Pressed("Jump")) Jump();
    ///
    /// // UI:
    /// UyiInput.OnPerformed("Pause", _ => TogglePause());
    /// UyiInput.SwitchMap("UI");
    ///
    /// // Rebind + lưu (qua SaveSystem):
    /// UyiInput.StartRebind("Jump", onComplete: RefreshBindingLabel);
    /// </code>
    /// Mẹo: gõ id kiểu string dễ sai — nên gom vào 1 class hằng số:
    /// <c>public static class InputIds { public const string Move="Move", Jump="Jump"; }</c>
    /// </summary>
    public static class UyiInput
    {
        const string RebindFile = "input_rebinds";

        static InputActionAsset _asset;
        static readonly Dictionary<string, InputAction> _cache = new Dictionary<string, InputAction>();
        static readonly List<Sub> _subs = new List<Sub>();
        static InputActionRebindingExtensions.RebindingOperation _rebind;
        static bool _warned;

        struct Sub { public InputAction action; public Action<InputAction.CallbackContext> handler; public bool performed; }

        /// <summary>Asset gốc — cho ai cần thò xuống API thô của Input System.</summary>
        public static InputActionAsset Asset => _asset;
        public static bool Ready => _asset != null;

        /// <summary>Thiết bị dùng gần nhất (đổi icon phím). Bắn <see cref="OnSchemeChanged"/> khi đổi.</summary>
        public static InputScheme Scheme { get; private set; } = InputScheme.KeyboardMouse;
        public static event Action<InputScheme> OnSchemeChanged;

        // ---- Khởi tạo / dọn ----

        /// <summary>Gọi 1 lần ở Bootstrap. <paramref name="defaultMap"/> null → bật tất cả map.</summary>
        public static void Init(InputActionAsset asset, string defaultMap = null)
        {
            if (asset == null) { Debug.LogError("[UyiInput] Init: asset null."); return; }

            Teardown();
            _asset = asset;
            _cache.Clear();
            _warned = false;

            LoadBindings();

            if (!string.IsNullOrEmpty(defaultMap)) SwitchMap(defaultMap);
            else _asset.Enable();

            InputSystem.onActionChange -= OnActionChange;
            InputSystem.onActionChange += OnActionChange;
        }

        /// <summary>Gỡ toàn bộ listener + bỏ tham chiếu asset.</summary>
        public static void Teardown()
        {
            CancelRebind();
            ClearSubs();
            InputSystem.onActionChange -= OnActionChange;
            _asset?.Disable();
            _asset = null;
            _cache.Clear();
        }

        // ---- Poll (đọc trong Update) ----

        public static bool Pressed(string id)    => Find(id)?.WasPressedThisFrame()  ?? false;
        public static bool Held(string id)       => Find(id)?.IsPressed()            ?? false;
        public static bool Released(string id)   => Find(id)?.WasReleasedThisFrame() ?? false;
        public static float Axis(string id)      => Find(id)?.ReadValue<float>()     ?? 0f;
        public static Vector2 Axis2D(string id)  => Find(id)?.ReadValue<Vector2>()   ?? Vector2.zero;
        public static T Read<T>(string id) where T : struct
            => Find(id) is InputAction a ? a.ReadValue<T>() : default;

        // ---- Event (UI / one-shot) ----

        public static void OnPerformed(string id, Action<InputAction.CallbackContext> cb) => Add(id, cb, true);
        public static void OnCanceled(string id, Action<InputAction.CallbackContext> cb) => Add(id, cb, false);

        /// <summary>Gỡ 1 callback đã đăng ký (cả performed lẫn canceled).</summary>
        public static void Off(string id, Action<InputAction.CallbackContext> cb)
        {
            var a = Find(id);
            if (a == null || cb == null) return;
            a.performed -= cb; a.canceled -= cb;
            _subs.RemoveAll(s => s.action == a && s.handler == cb);
        }

        // ---- Context (action map) ----

        /// <summary>Bật đúng 1 map (tắt các map khác). VD: SwitchMap("UI") khi mở menu.</summary>
        public static void SwitchMap(string map)
        {
            if (_asset == null) { WarnOnce(); return; }
            foreach (var m in _asset.actionMaps) m.Disable();
            var target = _asset.FindActionMap(map, throwIfNotFound: false);
            if (target != null) target.Enable();
            else Debug.LogWarning($"[UyiInput] không thấy action map '{map}'.");
        }

        public static void EnableMap(string map) => _asset?.FindActionMap(map, false)?.Enable();
        public static void DisableMap(string map) => _asset?.FindActionMap(map, false)?.Disable();

        // ---- Rebind + lưu ----

        /// <summary>Bắt đầu gán lại phím: chờ người chơi bấm phím mới. Xong tự lưu.</summary>
        public static void StartRebind(string id, int bindingIndex = 0, Action onComplete = null, Action onCancel = null)
        {
            var a = Find(id);
            if (a == null) return;

            CancelRebind();
            bool wasEnabled = a.enabled;
            a.Disable();

            _rebind = a.PerformInteractiveRebinding(bindingIndex)
                .OnComplete(op =>
                {
                    op.Dispose(); _rebind = null;
                    if (wasEnabled) a.Enable();
                    SaveBindings();
                    onComplete?.Invoke();
                })
                .OnCancel(op =>
                {
                    op.Dispose(); _rebind = null;
                    if (wasEnabled) a.Enable();
                    onCancel?.Invoke();
                })
                .Start();
        }

        public static void CancelRebind()
        {
            if (_rebind == null) return;
            _rebind.Cancel();
            _rebind.Dispose();
            _rebind = null;
        }

        /// <summary>Xoá mọi override → về phím mặc định, rồi lưu.</summary>
        public static void ResetBindings()
        {
            if (_asset == null) return;
            foreach (var m in _asset.actionMaps) m.RemoveAllBindingOverrides();
            SaveBindings();
        }

        /// <summary>Chuỗi hiển thị phím hiện tại (cho UI settings). VD "Space", "LMB", "A".</summary>
        public static string BindingDisplay(string id, int bindingIndex = 0)
        {
            var a = Find(id);
            return a != null ? a.GetBindingDisplayString(bindingIndex) : "";
        }

        public static void SaveBindings()
        {
            if (_asset == null) return;
            SaveSystem.Save(RebindFile, new RebindBlob { json = _asset.SaveBindingOverridesAsJson() });
        }

        static void LoadBindings()
        {
            var b = SaveSystem.Load<RebindBlob>(RebindFile);
            if (b != null && !string.IsNullOrEmpty(b.json)) _asset.LoadBindingOverridesFromJson(b.json);
        }

        [Serializable] class RebindBlob { public string json; }

        // ---- Internals ----

        static InputAction Find(string id)
        {
            if (_asset == null) { WarnOnce(); return null; }
            if (_cache.TryGetValue(id, out var a)) return a;
            a = _asset.FindAction(id, throwIfNotFound: false);
            if (a == null) Debug.LogWarning($"[UyiInput] không tìm thấy action '{id}'.");
            _cache[id] = a;
            return a;
        }

        static void Add(string id, Action<InputAction.CallbackContext> cb, bool performed)
        {
            var a = Find(id);
            if (a == null || cb == null) return;
            if (performed) a.performed += cb; else a.canceled += cb;
            _subs.Add(new Sub { action = a, handler = cb, performed = performed });
        }

        static void ClearSubs()
        {
            foreach (var s in _subs)
            {
                if (s.action == null) continue;
                if (s.performed) s.action.performed -= s.handler;
                else s.action.canceled -= s.handler;
            }
            _subs.Clear();
        }

        static void OnActionChange(object obj, InputActionChange change)
        {
            if (change != InputActionChange.ActionPerformed) return;
            if (obj is InputAction a && a.activeControl != null)
                UpdateScheme(a.activeControl.device);
        }

        static void UpdateScheme(InputDevice device)
        {
            var s = device is Gamepad ? InputScheme.Gamepad
                  : device is Touchscreen ? InputScheme.Touch
                  : InputScheme.KeyboardMouse;
            if (s == Scheme) return;
            Scheme = s;
            OnSchemeChanged?.Invoke(s);
        }

        static void WarnOnce()
        {
            if (_warned) return;
            _warned = true;
            Debug.LogWarning("[UyiInput] chưa Init — gọi UyiInput.Init(asset) ở Bootstrap trước.");
        }

        // Reset state tĩnh khi vào Play (phòng khi tắt Reload Domain — giống Observer).
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            ClearSubs();
            InputSystem.onActionChange -= OnActionChange;
            _asset = null;
            _cache.Clear();
            _rebind = null;
            _warned = false;
            Scheme = InputScheme.KeyboardMouse;
            OnSchemeChanged = null;
        }
    }
}
#endif
