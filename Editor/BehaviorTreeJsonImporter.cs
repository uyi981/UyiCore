using System.IO;
using UnityEditor.AssetImporters;
using UnityEngine;
using UyiCore.BT;

namespace UyiCore.EditorTools
{
    /// <summary>
    /// Import file <c>*.btjson</c> (tool HTML export) → <see cref="BehaviorTreeAsset"/> tự động.
    /// Thả file vào Assets/ là Unity sinh SO ngay; sửa file → reimport tự cập nhật.
    /// Dùng đuôi riêng <c>.btjson</c> để không nuốt mọi file .json khác trong project.
    /// </summary>
    [ScriptedImporter(1, "btjson")]
    public class BehaviorTreeJsonImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            string json = File.ReadAllText(ctx.assetPath);
            var asset = BehaviorTreeAsset.FromJson(json);
            if (asset == null)
            {
                ctx.LogImportError($"[BT] Parse JSON thất bại: {ctx.assetPath}");
                asset = ScriptableObject.CreateInstance<BehaviorTreeAsset>();
            }

            if (string.IsNullOrEmpty(asset.name))
                asset.name = Path.GetFileNameWithoutExtension(ctx.assetPath);

            foreach (var err in asset.Validate())
                ctx.LogImportWarning($"[BT] {asset.name}: {err}");

            ctx.AddObjectToAsset("BehaviorTree", asset);
            ctx.SetMainObject(asset);
        }
    }
}
