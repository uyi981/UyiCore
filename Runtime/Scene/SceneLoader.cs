using System.Collections;
using UyiCore.Observer;
using UyiCore.Patterns;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UyiCore.Scenes
{
    /// <summary>
    /// Quản lý chuyển scene theo Bootstrap + Additive pattern.
    /// Bootstrap scene luôn nằm dưới (chứa các manager singleton);
    /// Menu/Game/... load Additive chồng lên, scene cũ unload.
    /// Sống trong Bootstrap scene — không cần DontDestroyOnLoad.
    /// </summary>
    public class SceneLoader : SingletonBehaviour<SceneLoader>
    {
        [Header("Refs (optional)")]
        [Tooltip("Override fade overlay prefab. Để trống = auto-create runtime.")]
        [SerializeField] private SceneTransition _transitionPrefab;

        [Header("Default")]
        [SerializeField] private LoadOptions _defaultOptions = LoadOptions.Default;

        private SceneTransition _transition;
        private bool _isLoading;

        public bool IsLoading => _isLoading;

        protected override void OnAwake()
        {
            base.OnAwake();
            if (_defaultOptions.FadeDuration <= 0f && string.IsNullOrEmpty(_defaultOptions.LoadingSceneName))
                _defaultOptions = LoadOptions.Default;

            if (_transitionPrefab != null)
            {
                _transition = Instantiate(_transitionPrefab);
                _transition.name = "[SceneTransition]";
                DontDestroyOnLoad(_transition.gameObject);
            }
            else
            {
                _transition = SceneTransition.CreateRuntime();
            }
        }

        public void Load(string sceneName)
        {
            Load(sceneName, _defaultOptions);
        }

        public void Load(string sceneName, LoadOptions options)
        {
            if (_isLoading)
            {
                Debug.LogWarning($"[SceneLoader] Đang load, bỏ qua request '{sceneName}'.");
                return;
            }
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError("[SceneLoader] sceneName rỗng.");
                return;
            }
            StartCoroutine(LoadRoutine(sceneName, options));
        }

        IEnumerator LoadRoutine(string targetScene, LoadOptions opts)
        {
            _isLoading = true;

            if (_transition != null) _transition.SetColor(opts.FadeColor);

            string fromScene = SceneManager.GetActiveScene().name;
            Observer<GameEvent>.Emit(GameEvent.SceneLoadStarted, new SceneLoadStartedData(fromScene, targetScene));

            // 1. Fade-out (che màn hình)
            if (_transition != null) yield return _transition.FadeOut(opts.FadeDuration);

            // 2. Unload scene gameplay cũ (nếu khác Bootstrap)
            var active = SceneManager.GetActiveScene();
            if (active.IsValid() && active.name != CoreBootstrap.BootstrapSceneName && active.isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(active);
            }

            bool useLoading = opts.UseLoadingScene && !string.IsNullOrEmpty(opts.LoadingSceneName);

            // 3. Load Loading scene (additive) nếu có
            if (useLoading)
            {
                yield return SceneManager.LoadSceneAsync(opts.LoadingSceneName, LoadSceneMode.Additive);
                var ls = SceneManager.GetSceneByName(opts.LoadingSceneName);
                if (ls.IsValid()) SceneManager.SetActiveScene(ls);
                yield return null; // chờ 1 frame để Loading UI subscribe Observer
                if (_transition != null) yield return _transition.FadeIn(opts.FadeDuration);
            }

            // 4. Async load scene target, hold activation
            var op = SceneManager.LoadSceneAsync(targetScene, LoadSceneMode.Additive);
            op.allowSceneActivation = false;

            float startTime = Time.unscaledTime;

            // Unity report progress 0..0.9 trong khi load, 0.9 = ready để activate.
            while (op.progress < 0.9f)
            {
                float p = op.progress / 0.9f;
                Observer<GameEvent>.Emit(GameEvent.SceneLoadProgress, new SceneLoadProgressData(targetScene, p));
                yield return null;
            }
            Observer<GameEvent>.Emit(GameEvent.SceneLoadProgress, new SceneLoadProgressData(targetScene, 1f));

            // 5. Đợi đủ MinLoadingTime
            float elapsed = Time.unscaledTime - startTime;
            if (elapsed < opts.MinLoadingTime)
                yield return new WaitForSecondsRealtime(opts.MinLoadingTime - elapsed);

            // 6. Fade-out (che để chuyển sang scene target)
            if (_transition != null) yield return _transition.FadeOut(opts.FadeDuration);

            // 7. Activate target
            op.allowSceneActivation = true;
            while (!op.isDone) yield return null;

            var targetSceneObj = SceneManager.GetSceneByName(targetScene);
            if (targetSceneObj.IsValid()) SceneManager.SetActiveScene(targetSceneObj);

            // 8. Unload Loading scene
            if (useLoading)
            {
                var ls = SceneManager.GetSceneByName(opts.LoadingSceneName);
                if (ls.IsValid() && ls.isLoaded)
                    yield return SceneManager.UnloadSceneAsync(ls);
            }

            // 9. Fade-in (lộ scene target)
            if (_transition != null) yield return _transition.FadeIn(opts.FadeDuration);

            Observer<GameEvent>.Emit(GameEvent.SceneLoadCompleted, new SceneLoadCompletedData(targetScene));
            _isLoading = false;
        }
    }
}
