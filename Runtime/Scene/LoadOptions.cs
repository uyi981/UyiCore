using UnityEngine;

namespace UyiCore.Scenes
{
    /// <summary>
    /// Cấu hình 1 lần gọi <see cref="SceneLoader.Load"/>.
    /// Dùng <see cref="Default"/> rồi override field cần đổi.
    /// </summary>
    [System.Serializable]
    public struct LoadOptions
    {
        [Tooltip("Tên scene Loading hiện giữa hai scene. Để rỗng + UseLoadingScene=false để skip.")]
        public string LoadingSceneName;

        [Tooltip("Có dùng scene Loading trung gian không. False = fade thẳng từ scene cũ sang mới.")]
        public bool UseLoadingScene;

        [Tooltip("Thời gian fade (giây realtime, không bị timeScale ảnh hưởng).")]
        public float FadeDuration;

        [Tooltip("Màu fade overlay.")]
        public Color FadeColor;

        [Tooltip("Thời gian tối thiểu hiện Loading screen (UX — tránh nhấp nháy khi load nhanh).")]
        public float MinLoadingTime;

        public static LoadOptions Default => new LoadOptions
        {
            LoadingSceneName = "Loading",
            UseLoadingScene = true,
            FadeDuration = 0.3f,
            FadeColor = Color.black,
            MinLoadingTime = 0.5f,
        };
    }
}
