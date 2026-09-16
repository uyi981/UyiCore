using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace UyiCore.EditorTools
{
    /// <summary>
    /// Prefab Brush — "vẽ" prefab lên Scene view. Tools ▸ UyiCore ▸ Prefab Brush.
    /// Bật Paint → kéo chuột trái trong Scene để rải, giữ Ctrl để xoá, Alt để xoay camera.
    /// </summary>
    public class PrefabBrush : EditorWindow
    {
        [MenuItem("Tools/UyiCore/Prefab Brush", priority = 120)]
        static void Open() => GetWindow<PrefabBrush>("Prefab Brush").minSize = new Vector2(280, 360);

        enum Surface { Collider, GroundY, TwoD }

        bool _painting;
        readonly List<GameObject> _prefabs = new List<GameObject>();
        float _radius = 2f;
        int _density = 4;
        float _spacing = 1f;
        Surface _surface = Surface.Collider;
        float _groundY = 0f;
        LayerMask _mask = ~0;
        bool _alignNormal = false;
        bool _randomYaw = true;
        Vector2 _scaleRange = new Vector2(1f, 1f);
        Transform _parent;

        Vector3 _hit; Vector3 _normal = Vector3.up; bool _hasHit;
        double _lastPaint;

        void OnEnable() { SceneView.duringSceneGui += OnScene; }
        void OnDisable() { SceneView.duringSceneGui -= OnScene; _painting = false; }

        // ---------------- Window UI ----------------
        void OnGUI()
        {
            var prev = GUI.backgroundColor;
            GUI.backgroundColor = _painting ? new Color(0.5f, 1f, 0.6f) : prev;
            if (GUILayout.Button(_painting ? "■  ĐANG PAINT — bấm để tắt" : "▶  Bật Paint", GUILayout.Height(30)))
                _painting = !_painting;
            GUI.backgroundColor = prev;
            EditorGUILayout.HelpBox("Kéo chuột trái trong Scene = rải · giữ Ctrl = xoá · Alt = xoay camera.", MessageType.None);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Prefabs (rải ngẫu nhiên)", EditorStyles.boldLabel);
            int remove = -1;
            for (int i = 0; i < _prefabs.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                _prefabs[i] = (GameObject)EditorGUILayout.ObjectField(_prefabs[i], typeof(GameObject), false);
                if (GUILayout.Button("✕", GUILayout.Width(24))) remove = i;
                EditorGUILayout.EndHorizontal();
            }
            if (remove >= 0) _prefabs.RemoveAt(remove);
            if (GUILayout.Button("+ Thêm prefab")) _prefabs.Add(null);

            EditorGUILayout.Space();
            _radius = EditorGUILayout.Slider("Brush size", _radius, 0.1f, 30f);
            _density = EditorGUILayout.IntSlider("Density / lần", _density, 1, 30);
            _spacing = EditorGUILayout.Slider("Spacing tối thiểu", _spacing, 0f, 10f);

            EditorGUILayout.Space();
            _surface = (Surface)EditorGUILayout.EnumPopup("Đặt lên", _surface);
            if (_surface == Surface.Collider)
            {
                _mask = LayerMaskField("Layer mask", _mask);
                _alignNormal = EditorGUILayout.Toggle("Xoay theo normal mặt", _alignNormal);
            }
            else if (_surface == Surface.GroundY)
            {
                _groundY = EditorGUILayout.FloatField("Ground Y", _groundY);
            }

            EditorGUILayout.Space();
            _randomYaw = EditorGUILayout.Toggle("Random xoay quanh trục", _randomYaw);
            _scaleRange = EditorGUILayout.Vector2Field("Scale ngẫu nhiên (min/max)", _scaleRange);
            _parent = (Transform)EditorGUILayout.ObjectField("Parent (optional)", _parent, typeof(Transform), true);
        }

        // ---------------- Scene painting ----------------
        void OnScene(SceneView sv)
        {
            if (!_painting) return;
            var e = Event.current;
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

            _hasHit = Sample(HandleUtility.GUIPointToWorldRay(e.mousePosition), out _hit, out _normal);
            if (_hasHit)
            {
                Handles.color = e.control ? new Color(1f, 0.4f, 0.35f) : new Color(0.35f, 1f, 0.55f);
                Vector3 discNormal = _surface == Surface.TwoD ? Vector3.forward : (_alignNormal ? _normal : Vector3.up);
                Handles.DrawWireDisc(_hit, discNormal, _radius);
                sv.Repaint();
            }

            if (e.alt) return;   // nhường Alt cho orbit camera

            if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.button == 0 && _hasHit)
            {
                if (EditorApplication.timeSinceStartup - _lastPaint > 0.03)
                {
                    if (e.control) Erase(_hit);
                    else Paint(_hit, _normal);
                    _lastPaint = EditorApplication.timeSinceStartup;
                }
                e.Use();
            }
            else if (e.type == EventType.MouseUp && e.button == 0)
            {
                e.Use();
            }
        }

        bool Sample(Ray ray, out Vector3 point, out Vector3 normal)
        {
            normal = Vector3.up; point = Vector3.zero;
            switch (_surface)
            {
                case Surface.Collider:
                    if (Physics.Raycast(ray, out var hit, 100000f, _mask))
                    { point = hit.point; normal = hit.normal; return true; }
                    return false;
                case Surface.GroundY:
                {
                    var p = new Plane(Vector3.up, new Vector3(0f, _groundY, 0f));
                    if (p.Raycast(ray, out float d)) { point = ray.GetPoint(d); return true; }
                    return false;
                }
                case Surface.TwoD:
                {
                    var p = new Plane(Vector3.forward, Vector3.zero);
                    if (p.Raycast(ray, out float d)) { point = ray.GetPoint(d); normal = Vector3.back; return true; }
                    return false;
                }
            }
            return false;
        }

        void Paint(Vector3 center, Vector3 centerNormal)
        {
            _prefabs.RemoveAll(p => p == null);
            if (_prefabs.Count == 0) { Debug.LogWarning("[PrefabBrush] Chưa gán prefab."); return; }

            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Prefab Brush");
            var placed = new List<Vector3>();

            for (int i = 0; i < _density; i++)
            {
                Vector2 rnd = Random.insideUnitCircle * _radius;
                Vector3 pos = center + (_surface == Surface.TwoD ? new Vector3(rnd.x, rnd.y, 0f) : new Vector3(rnd.x, 0f, rnd.y));
                Vector3 nrm = centerNormal;

                if (_surface == Surface.Collider)
                {
                    var down = new Ray(pos + Vector3.up * 50f, Vector3.down);
                    if (Physics.Raycast(down, out var h, 100000f, _mask)) { pos = h.point; nrm = h.normal; }
                }
                else if (_surface == Surface.GroundY) pos.y = _groundY;

                if (_spacing > 0f)
                {
                    bool tooClose = false;
                    float s2 = _spacing * _spacing;
                    for (int k = 0; k < placed.Count; k++)
                        if ((placed[k] - pos).sqrMagnitude < s2) { tooClose = true; break; }
                    if (tooClose) continue;
                }
                placed.Add(pos);

                var prefab = _prefabs[Random.Range(0, _prefabs.Count)];
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                if (go == null) continue;
                Undo.RegisterCreatedObjectUndo(go, "Prefab Brush");
                var t = go.transform;
                if (_parent != null) t.SetParent(_parent, true);
                t.position = pos;
                if (_alignNormal && _surface != Surface.TwoD) t.up = nrm;
                if (_randomYaw) t.Rotate(t.up, Random.Range(0f, 360f), Space.World);
                float sc = Random.Range(Mathf.Min(_scaleRange.x, _scaleRange.y), Mathf.Max(_scaleRange.x, _scaleRange.y));
                if (!Mathf.Approximately(sc, 1f)) t.localScale *= sc;
            }

            Undo.CollapseUndoOperations(group);
        }

        void Erase(Vector3 center)
        {
            var toDelete = new HashSet<GameObject>();
            float r2 = _radius * _radius;

            if (_parent != null)
                foreach (Transform c in _parent)
                    if ((c.position - center).sqrMagnitude <= r2) AddPrefabRoot(toDelete, c.gameObject);

            if (_surface == Surface.TwoD)
            {
                foreach (var col in Physics2D.OverlapCircleAll(center, _radius))
                    AddPrefabRoot(toDelete, col.gameObject);
            }
            else
            {
                foreach (var col in Physics.OverlapSphere(center, _radius, _mask))
                    AddPrefabRoot(toDelete, col.gameObject);
            }

            if (toDelete.Count == 0) return;
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Prefab Brush Erase");
            foreach (var go in toDelete) Undo.DestroyObjectImmediate(go);
            Undo.CollapseUndoOperations(group);
        }

        static void AddPrefabRoot(HashSet<GameObject> set, GameObject go)
        {
            var root = PrefabUtility.GetNearestPrefabInstanceRoot(go);
            if (root != null) set.Add(root);   // chỉ xoá prefab instance, không đụng object scene khác
        }

        static LayerMask LayerMaskField(string label, LayerMask layerMask)
        {
            var layers = InternalEditorUtility.layers;
            int mask = 0;
            for (int i = 0; i < layers.Length; i++)
                if ((layerMask.value & (1 << LayerMask.NameToLayer(layers[i]))) != 0) mask |= 1 << i;
            mask = EditorGUILayout.MaskField(label, mask, layers);
            int result = 0;
            for (int i = 0; i < layers.Length; i++)
                if ((mask & (1 << i)) != 0) result |= 1 << LayerMask.NameToLayer(layers[i]);
            return result;
        }
    }
}
