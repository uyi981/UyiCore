using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace UyiCore.EditorTools
{
    /// <summary>
    /// Dựng prefab UGUI từ file *.uijson (tool UI layout export).
    /// Mỗi node → GameObject + RectTransform (+ Image nếu có color/sprite).
    /// Định vị TUYỆT ĐỐI: neo góc trên-trái của cha, anchoredPosition = (x, -y), sizeDelta = (w, h).
    /// Root neo stretch-fill cha → thả prefab dưới Canvas là phủ full.
    ///
    /// Lưu ý: set Canvas Scaler = "Scale With Screen Size", Reference Resolution = (canvasW, canvasH)
    /// để layout khớp đúng như thiết kế trong tool.
    /// </summary>
    public static class UiPrefabBuilder
    {
        [MenuItem("Tools/UyiCore/Build UI Prefab from JSON…")]
        public static void BuildFromFile()
        {
            string src = EditorUtility.OpenFilePanel("Chọn UI layout (.uijson)", Application.dataPath, "uijson,json");
            if (string.IsNullOrEmpty(src)) return;

            UiLayoutJson layout;
            try { layout = JsonUtility.FromJson<UiLayoutJson>(File.ReadAllText(src)); }
            catch (System.Exception e) { EditorUtility.DisplayDialog("UyiCore UI", "Parse JSON lỗi:\n" + e.Message, "OK"); return; }

            if (layout == null || layout.nodes == null || layout.nodes.Length == 0)
            { EditorUtility.DisplayDialog("UyiCore UI", "JSON rỗng hoặc không có nodes.", "OK"); return; }

            string savePath = EditorUtility.SaveFilePanelInProject(
                "Lưu prefab UI", SafeName(layout.name), "prefab", "Chọn nơi lưu prefab");
            if (string.IsNullOrEmpty(savePath)) return;

            var root = Build(layout);
            if (root == null) { EditorUtility.DisplayDialog("UyiCore UI", "Không dựng được — thiếu/không thấy root.", "OK"); return; }

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, savePath);
            Object.DestroyImmediate(root);

            if (prefab != null)
            {
                Selection.activeObject = prefab;
                EditorGUIUtility.PingObject(prefab);
                Debug.Log($"[UyiCore UI] Tạo prefab: {savePath}");
            }
        }

        static GameObject Build(UiLayoutJson layout)
        {
            var map = new Dictionary<string, UiNodeJson>();
            foreach (var n in layout.nodes)
                if (n != null && !string.IsNullOrEmpty(n.id)) map[n.id] = n;

            string rootId = !string.IsNullOrEmpty(layout.root) ? layout.root : layout.nodes[0].id;
            return map.TryGetValue(rootId, out var rn) ? BuildNode(rn, map, null, true, new HashSet<string>()) : null;
        }

        static GameObject BuildNode(UiNodeJson n, Dictionary<string, UiNodeJson> map, RectTransform parent, bool isRoot, HashSet<string> path)
        {
            if (!path.Add(n.id)) { Debug.LogWarning($"[UyiCore UI] Vòng lặp tại node '{n.id}'."); return null; }

            bool isText = n.kind == "text";
            bool hasVisual = !string.IsNullOrEmpty(n.color) || !string.IsNullOrEmpty(n.sprite);
            string goName = !string.IsNullOrEmpty(n.name) ? n.name : n.id;

            GameObject go;
            if (isText) go = new GameObject(goName, typeof(RectTransform), typeof(TextMeshProUGUI));
            else if (hasVisual) go = new GameObject(goName, typeof(RectTransform), typeof(Image));
            else go = new GameObject(goName, typeof(RectTransform));

            var rt = (RectTransform)go.transform;
            if (parent != null) rt.SetParent(parent, false);

            if (isText)
            {
                var tmp = go.GetComponent<TextMeshProUGUI>();
                tmp.text = n.text ?? "";
                tmp.fontSize = n.fontSize > 0f ? n.fontSize : 32f;
                tmp.color = ParseColor(n.color);
                tmp.alignment = TmpAlign(n.align, n.valign);
            }
            else if (hasVisual)
            {
                var img = go.GetComponent<Image>();
                img.color = ParseColor(n.color);
                if (!string.IsNullOrEmpty(n.sprite))
                {
                    var sp = FindSprite(n.sprite);
                    if (sp != null) img.sprite = sp;
                    else Debug.LogWarning($"[UyiCore UI] Không thấy sprite '{n.sprite}' (node '{n.id}').");
                }
            }

            if (isRoot)
            {
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            }
            else
            {
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.anchoredPosition = new Vector2(n.x, -n.y);
                rt.sizeDelta = new Vector2(n.w, n.h);
            }

            if (n.children != null)
                foreach (var cid in n.children)
                    if (map.TryGetValue(cid, out var cn)) BuildNode(cn, map, rt, false, path);

            path.Remove(n.id);
            return go;
        }

        static Color ParseColor(string hex)
        {
            if (string.IsNullOrEmpty(hex)) return Color.white;
            if (hex[0] != '#') hex = "#" + hex;
            return ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.white;
        }

        static TextAlignmentOptions TmpAlign(string h, string v)
        {
            h = string.IsNullOrEmpty(h) ? "center" : h;
            v = string.IsNullOrEmpty(v) ? "middle" : v;
            if (v == "top")    return h == "left" ? TextAlignmentOptions.TopLeft    : h == "right" ? TextAlignmentOptions.TopRight    : TextAlignmentOptions.Top;
            if (v == "bottom") return h == "left" ? TextAlignmentOptions.BottomLeft : h == "right" ? TextAlignmentOptions.BottomRight : TextAlignmentOptions.Bottom;
            return                    h == "left" ? TextAlignmentOptions.Left       : h == "right" ? TextAlignmentOptions.Right       : TextAlignmentOptions.Center;
        }

        static Sprite FindSprite(string nameOrPath)
        {
            if (nameOrPath.Contains("/"))
            {
                var sp = AssetDatabase.LoadAssetAtPath<Sprite>(nameOrPath);
                if (sp != null) return sp;
            }
            // ưu tiên khớp tên chính xác
            foreach (var guid in AssetDatabase.FindAssets($"{nameOrPath} t:Sprite"))
            {
                var sp = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guid));
                if (sp != null && sp.name == nameOrPath) return sp;
            }
            // fallback: khớp gần đúng đầu tiên
            foreach (var guid in AssetDatabase.FindAssets($"{nameOrPath} t:Sprite"))
            {
                var sp = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guid));
                if (sp != null) return sp;
            }
            return null;
        }

        static string SafeName(string s) => string.IsNullOrEmpty(s) ? "UILayout" : s.Replace(" ", "");
    }
}
