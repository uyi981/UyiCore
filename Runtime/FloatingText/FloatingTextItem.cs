using System;
using TMPro;
using UnityEngine;

namespace UyiCore.FloatingText
{
    /// <summary>
    /// 1 item floating text. Prefab cần:
    ///   - RectTransform (vì nằm dưới World-Space Canvas)
    ///   - TMP_Text + CanvasGroup ở cùng GameObject hoặc child
    ///   - Component này gắn ở root prefab
    /// Service set Velocity/Lifetime/Color/Text khi spawn.
    /// </summary>
    [DisallowMultipleComponent]
    public class FloatingTextItem : MonoBehaviour
    {
        [SerializeField] private TMP_Text _label;
        [SerializeField] private CanvasGroup _group;
        [SerializeField] private AnimationCurve _alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
        [SerializeField] private AnimationCurve _scaleCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);

        private Vector2 _velocity;
        private float _lifetime;
        private float _elapsed;
        private Vector3 _baseScale;
        private Action<FloatingTextItem> _onDespawn;
        private bool _running;

        void Reset()
        {
            _label = GetComponentInChildren<TMP_Text>();
            _group = GetComponent<CanvasGroup>();
            if (_group == null) _group = gameObject.AddComponent<CanvasGroup>();
        }

        public void Spawn(string text, FloatingTextStyle style, Vector3 worldPos, Action<FloatingTextItem> onDespawn)
        {
            _onDespawn = onDespawn;
            _velocity = style.velocity;
            _lifetime = Mathf.Max(0.01f, style.lifetime);
            _elapsed = 0f;

            if (_label != null)
            {
                _label.text = text;
                _label.color = style.color;
                _label.fontSize = style.fontSize;
            }

            var jitter = style.spawnJitter;
            if (jitter.sqrMagnitude > 0f)
            {
                worldPos.x += UnityEngine.Random.Range(-jitter.x, jitter.x);
                worldPos.y += UnityEngine.Random.Range(-jitter.y, jitter.y);
            }
            transform.position = worldPos;
            _baseScale = transform.localScale == Vector3.zero ? Vector3.one : transform.localScale;
            transform.localScale = _baseScale;

            if (_group != null) _group.alpha = 1f;

            gameObject.SetActive(true);
            _running = true;
        }

        void Update()
        {
            if (!_running) return;
            _elapsed += Time.deltaTime;
            if (_elapsed >= _lifetime)
            {
                Despawn();
                return;
            }

            transform.position += (Vector3)_velocity * Time.deltaTime;
            float u = _elapsed / _lifetime;
            if (_group != null) _group.alpha = _alphaCurve.Evaluate(u);
            transform.localScale = _baseScale * _scaleCurve.Evaluate(u);
        }

        void Despawn()
        {
            _running = false;
            gameObject.SetActive(false);
            _onDespawn?.Invoke(this);
        }
    }
}
