using UnityEditor;
using UnityEditor.SceneManagement;
using UyiCore.Scenes;

namespace UyiCore.EditorTools
{
    /// <summary>
    /// Editor-only: force Play mode luôn bắt đầu từ Bootstrap scene
    /// bất kể dev đang mở scene nào trong Hierarchy.
    /// </summary>
    [InitializeOnLoad]
    static class BootstrapEditorPlayMode
    {
        static BootstrapEditorPlayMode()
        {
            var guids = AssetDatabase.FindAssets($"t:SceneAsset {CoreBootstrap.BootstrapSceneName}");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
                if (asset != null && asset.name == CoreBootstrap.BootstrapSceneName)
                {
                    EditorSceneManager.playModeStartScene = asset;
                    return;
                }
            }
        }
    }
}
