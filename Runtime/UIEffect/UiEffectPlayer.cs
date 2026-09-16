using UnityEngine;
using UyiCore.Tweening;

namespace UyiCore.UIEffect
{
    /// <summary>
    /// Kéo lên 1 UI element → chơi 1 <see cref="UiEffectPreset"/> khi trigger (WHEN).
    /// Target mặc định = RectTransform của chính nó (auto). Không đụng code logic UI.
    /// </summary>
    public class UiEffectPlayer : MonoBehaviour
    {
        public enum Trigger { OnEnable, OnStart, Manual }

        [SerializeField] private UiEffectPreset _preset;
        [SerializeField] private Trigger _trigger = Trigger.OnEnable;
        [Tooltip("Để trống = dùng RectTransform của chính GameObject này.")]
        [SerializeField] private RectTransform _target;

        private TweenHandle _current;

        void Reset() { _target = transform as RectTransform; }
        void OnEnable() { if (_trigger == Trigger.OnEnable) Play(); }
        void Start() { if (_trigger == Trigger.OnStart) Play(); }

        /// <summary>Chơi preset (cancel lần trước nếu còn chạy).</summary>
        public TweenHandle Play()
        {
            if (_preset == null) { Debug.LogWarning($"[UiEffectPlayer] Chưa gán preset ({name})."); return default; }
            var t = _target != null ? _target : transform as RectTransform;
            if (t == null) return default;
            _current.Cancel();
            _current = _preset.Play(t);
            return _current;
        }
    }
}
