using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UyiCore.BT;

namespace UyiCore.EditorTools
{
    /// <summary>
    /// Inspector gọn cho <see cref="BehaviorTreeAsset"/>: hiện tên/root/số node,
    /// lỗi validate, và outline cây dạng text để soi nhanh sau khi import.
    /// </summary>
    [CustomEditor(typeof(BehaviorTreeAsset))]
    public class BehaviorTreeAssetEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var asset = (BehaviorTreeAsset)target;

            EditorGUILayout.LabelField("Tree", string.IsNullOrEmpty(asset.treeName) ? "(chưa đặt tên)" : asset.treeName, EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Root id", string.IsNullOrEmpty(asset.rootId) ? "(trống)" : asset.rootId);
            EditorGUILayout.LabelField("Số node", (asset.nodes != null ? asset.nodes.Count : 0).ToString());

            var errors = asset.Validate();
            if (errors.Count > 0)
            {
                EditorGUILayout.Space();
                foreach (var e in errors) EditorGUILayout.HelpBox(e, MessageType.Error);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Outline", EditorStyles.boldLabel);

            var sb = new StringBuilder();
            Print(asset, asset.rootId, 0, sb, new HashSet<string>());
            EditorGUILayout.TextArea(sb.Length > 0 ? sb.ToString() : "(cây rỗng)", GUILayout.MinHeight(90));
        }

        static void Print(BehaviorTreeAsset a, string id, int depth, StringBuilder sb, HashSet<string> path)
        {
            if (string.IsNullOrEmpty(id)) return;
            string pad = new string(' ', depth * 2);

            var n = a.Find(id);
            if (n == null) { sb.AppendLine($"{pad}?? thiếu '{id}'"); return; }
            if (!path.Add(id)) { sb.AppendLine($"{pad}↻ vòng lặp '{id}'"); return; }

            string label = n.type;
            if (!string.IsNullOrEmpty(n.@ref)) label += $" \"{n.@ref}\"";
            sb.AppendLine($"{pad}• {label}");

            if (n.children != null)
                foreach (var c in n.children) Print(a, c, depth + 1, sb, path);

            path.Remove(id);
        }
    }
}
