using UnityEngine;
using UnityEngine.EventSystems;
using UyiCore.Tweening;

namespace UyiCore.UIEffect
{
    /// <summary>
    /// Feedback bấm nút: nhấn → thu nhỏ, thả → bung về (OutBack nảy nhẹ). Kéo lên bất kỳ UI
    /// có raycast target (Button, Image...) là xong — không cần sửa logic nút.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class ButtonFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [Range(0.5f, 1f)] [SerializeField] private float _pressScale = 0.92f;
        [SerializeField] private float _duration = 0.12f;

        private Vector3 _base = Vector3.one;
        private bool _captured;
        private TweenHandle _tw;

        void Awake() { Capture(); }
        void OnDisable() { _tw.Cancel(); if (_captured) transform.localScale = _base; }

        void Capture()
        {
            if (_captured) return;
            _base = transform.localScale;
            _captured = true;
        }

        public void OnPointerDown(PointerEventData e)
        {
            Capture();
            _tw.Cancel();
            _tw = transform.ScaleTo(_base * _pressScale, _duration, Ease.OutQuad);
        }

        public void OnPointerUp(PointerEventData e)
        {
            _tw.Cancel();
            _tw = transform.ScaleTo(_base, _duration, Ease.OutBack);
        }
    }
}
