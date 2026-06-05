using UnityEngine;
using UnityEngine.SceneManagement;

namespace UyiCore.Scenes
{
    /// <summary>
    /// Đảm bảo Bootstrap scene luôn được load trước khi gameplay scene chạy.
    /// - Production: Bootstrap = scene index 0 trong Build Settings, load tự nhiên.
    /// - Editor: nếu dev bấm Play ở scene khác → tự load Bootstrap additive ở frame 1.
    /// Để tránh thiếu manager hoàn toàn, dùng kèm BootstrapEditorPlayMode (Editor assembly)
    /// để Editor force scene start là Bootstrap.
    /// </summary>
    public static class CoreBootstrap
    {
        public const string BootstrapSceneName = "Bootstrap";

        private static bool _checked;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Hook()
        {
            _checked = false;
            SceneManager.sceneLoaded += OnAnySceneLoaded;
        }

        static void OnAnySceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (_checked) return;
            _checked = true;
            SceneManager.sceneLoaded -= OnAnySceneLoaded;

            if (scene.name == BootstrapSceneName) return;

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                if (SceneManager.GetSceneAt(i).name == BootstrapSceneName) return;
            }

            try
            {
                SceneManager.LoadScene(BootstrapSceneName, LoadSceneMode.Additive);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[CoreBootstrap] Không load được '{BootstrapSceneName}': {e.Message}. Thêm Bootstrap vào Build Settings.");
            }
        }
    }
}
