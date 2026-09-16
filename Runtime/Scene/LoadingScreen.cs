using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace UyiCore.Scenes
{
    /// <summary>
    /// Gắn vào scene "Loading". Tự subscribe progress của <see cref="SceneLoader"/> → cập nhật thanh bar.
    /// Kéo 1 Image (Image Type = Filled) vào <c>_bar</c>. Muốn hiện % / xoay spinner thì wire
    /// event <c>_onProgress</c> (0..1) trong inspector — khỏi phải viết code subscribe tay.
    /// </summary>
    public class LoadingScreen : MonoBehaviour
    {
        [Serializable] public class ProgressEvent : UnityEvent<float> { }

        [Tooltip("Image (Type = Filled) làm thanh tiến trình. Optional.")]
        [SerializeField] private Image _bar;

        [Header("Mượt")]
        [Tooltip("Cho bar chạy mượt tới target thay vì nhảy giật.")]
        [SerializeField] private bool _smooth = true;
        [SerializeField] private float _smoothSpeed = 5f;

        [Header("Event (optional)")]
        [Tooltip("Progress 0..1 — wire để cập nhật text %, xoay spinner, v.v.")]
        [SerializeField] private ProgressEvent _onProgress;

        private float _target;

        void OnEnable()
        {
            SceneLoader.OnLoadStarted += HandleStarted;
            SceneLoader.OnLoadProgress += HandleProgress;
            SceneLoader.OnLoadCompleted += HandleCompleted;
            Apply(0f, instant: true);
        }

        void OnDisable()
        {
            SceneLoader.OnLoadStarted -= HandleStarted;
            SceneLoader.OnLoadProgress -= HandleProgress;
            SceneLoader.OnLoadCompleted -= HandleCompleted;
        }

        void HandleStarted(SceneLoadStartedData _) => Apply(0f, instant: true);
        void HandleProgress(SceneLoadProgressData d) => Apply(d.progress, instant: false);
        void HandleCompleted(SceneLoadCompletedData _) => Apply(1f, instant: true);

        void Apply(float p, bool instant)
        {
            _target = Mathf.Clamp01(p);
            _onProgress?.Invoke(_target);
            if (instant && _bar != null) _bar.fillAmount = _target;
        }

        void Update()
        {
            if (_bar == null) return;
            _bar.fillAmount = _smooth
                ? Mathf.MoveTowards(_bar.fillAmount, _target, _smoothSpeed * Time.unscaledDeltaTime)
                : _target;
        }
    }
}
