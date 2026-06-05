using System.Collections.Generic;
using UnityEngine;

namespace UyiCore.Character
{
    [DisallowMultipleComponent]
    public class CharacterRoot : MonoBehaviour
    {
        [Header("Cached References")]
        [SerializeField] private Rigidbody2D _rigidbody;
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Animator _animator;
        [SerializeField] private Collider2D _collider;

        public Rigidbody2D Rigidbody => _rigidbody;
        public SpriteRenderer SpriteRenderer => _spriteRenderer;
        public Animator Animator => _animator;
        public Collider2D Collider => _collider;

        private readonly List<CharacterModule> _modules = new List<CharacterModule>();

        void Reset()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            _animator = GetComponentInChildren<Animator>();
            _collider = GetComponent<Collider2D>();
        }

        void Awake()
        {
            GetComponentsInChildren(true, _modules);
            for (int i = 0; i < _modules.Count; i++)
            {
                _modules[i].Initialize(this);
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;
            for (int i = 0; i < _modules.Count; i++)
            {
                if (_modules[i].isActiveAndEnabled)
                    _modules[i].OnTick(dt);
            }
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            for (int i = 0; i < _modules.Count; i++)
            {
                if (_modules[i].isActiveAndEnabled)
                    _modules[i].OnFixedTick(dt);
            }
        }

        public void ReinitializeModules()
        {
            for (int i = 0; i < _modules.Count; i++)
            {
                _modules[i].Initialize(this);
            }
        }

        public T GetModule<T>() where T : CharacterModule
        {
            for (int i = 0; i < _modules.Count; i++)
            {
                if (_modules[i] is T typed) return typed;
            }
            return null;
        }
    }
}
