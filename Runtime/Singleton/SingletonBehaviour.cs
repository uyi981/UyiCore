using UnityEngine;

namespace UyiCore.Patterns
{
    /// <summary>
    /// Generic singleton base cho MonoBehaviour. Subclass khai báo dạng:
    ///     public class FooManager : SingletonBehaviour&lt;FooManager&gt; { ... }
    /// Subclass dùng <see cref="OnAwake"/> / <see cref="OnSingletonDestroy"/> để khởi tạo / cleanup
    /// (không override Awake / OnDestroy trực tiếp để tránh quên gọi base).
    /// </summary>
    public abstract class SingletonBehaviour<T> : MonoBehaviour where T : SingletonBehaviour<T>
    {
        private static T _instance;

        public static T Instance => _instance;
        public static bool HasInstance => _instance != null;

        [Header("Singleton")]
        [SerializeField] private bool _dontDestroyOnLoad = false;

        protected virtual void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning($"[{typeof(T).Name}] Duplicate instance detected. Destroying {gameObject.name}.");
                Destroy(gameObject);
                return;
            }
            _instance = (T)this;
            if (_dontDestroyOnLoad) DontDestroyOnLoad(gameObject);
            OnAwake();
        }

        protected virtual void OnDestroy()
        {
            if (_instance == this)
            {
                OnSingletonDestroy();
                _instance = null;
            }
        }

        /// <summary>Hook khởi tạo sau khi đã gán Instance. Override thay vì override Awake.</summary>
        protected virtual void OnAwake() { }

        /// <summary>Hook cleanup chạy ngay trước khi clear Instance.</summary>
        protected virtual void OnSingletonDestroy() { }
    }
}
