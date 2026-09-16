using System.IO;
using UnityEditor;
using UnityEngine;

namespace UyiCore.EditorTools
{
    /// <summary>
    /// Menu mở các tool HTML (nằm trong Tools~/ — Unity không import nên phải tự dò path).
    /// Hoạt động cả khi package cài qua UPM (PackageCache) lẫn khi mở project trực tiếp.
    /// </summary>
    static class OpenToolsMenu
    {
        [MenuItem("Tools/UyiCore/Open Behavior Tree Editor", priority = 100)]
        static void OpenBT() => OpenTool("Tools~/BehaviorTreeEditor/index.html", "Behavior Tree Editor");

        [MenuItem("Tools/UyiCore/Open UI Layout Editor", priority = 101)]
        static void OpenUI() => OpenTool("Tools~/UiLayoutEditor/index.html", "UI Layout Editor");

        static void OpenTool(string relPath, string label)
        {
            var root = PackageRoot();
            if (string.IsNullOrEmpty(root))
            {
                Debug.LogError("[UyiCore] Không xác định được thư mục gốc package.");
                return;
            }

            var file = Path.Combine(root, relPath);
            if (!File.Exists(file))
            {
                Debug.LogError($"[UyiCore] Không thấy {label}: {file}");
                return;
            }

            // Mở bằng browser mặc định. file:/// + forward slashes cho Windows.
            Application.OpenURL("file:///" + Path.GetFullPath(file).Replace("\\", "/"));
        }

        /// <summary>
        /// Thư mục gốc package (absolute). Dò theo asset path của chính script này.
        /// Path.GetFullPath resolve được cả path ảo "Packages/..." (kể cả PackageCache) trong Editor.
        /// </summary>
        static string PackageRoot()
        {
            foreach (var guid in AssetDatabase.FindAssets($"t:MonoScript {nameof(OpenToolsMenu)}"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(nameof(OpenToolsMenu) + ".cs")) continue;
                // <root>/Editor/OpenToolsMenu.cs → lùi 2 cấp về <root>
                var full = Path.GetFullPath(path);
                return Directory.GetParent(Path.GetDirectoryName(full))?.FullName;
            }
            return null;
        }
    }
}
