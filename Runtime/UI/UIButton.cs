using DG.Tweening;
using UyiCore.Audio;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UyiCore.UI
{
    [RequireComponent(typeof(Button))]
    public class UIButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [Header("Scale Effect")]
        [SerializeField] private Transform _target;
        [SerializeField] private Vector3 _normalScale = Vector3.one;
        [SerializeField] private Vector3 _pressedScale = new Vector3(0.9f, 0.9f, 0.9f);
        [SerializeField] private float _tweenDuration = 0.08f;
        [SerializeField] private Ease _ease = Ease.OutQuad;

        [Header("Sound")]
        [SerializeField] private string _clickSfxId = "button_click_08";
        [SerializeField] private bool _playSfx = true;

        private Button _button;
        private Tween _scaleTween;
        private bool _isPressed;

        void Awake()
        {
            _button = GetComponent<Button>();
            if (_target == null) _target = transform;
            _target.localScale = _normalScale;

            _button.onClick.AddListener(OnClick);
        }

        void OnDestroy()
        {
            if (_button != null) _button.onClick.RemoveListener(OnClick);
            _scaleTween?.Kill();
        }

        void OnDisable()
        {
            _scaleTween?.Kill();
            if (_target != null) _target.localScale = _normalScale;
            _isPressed = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!_button.interactable) return;
            _isPressed = true;
            TweenTo(_pressedScale);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_isPressed) return;
            _isPressed = false;
            TweenTo(_normalScale);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!_isPressed) return;
            _isPressed = false;
            TweenTo(_normalScale);
        }

        void OnClick()
        {
            if (_playSfx && AudioManager.Instance != null && !string.IsNullOrEmpty(_clickSfxId))
            {
                AudioManager.Instance.PlaySfx(_clickSfxId);
            }
        }

        void TweenTo(Vector3 target)
        {
            _scaleTween?.Kill();
            _scaleTween = _target.DOScale(target, _tweenDuration)
                .SetEase(_ease)
                .SetUpdate(true);
        }
    }
}
