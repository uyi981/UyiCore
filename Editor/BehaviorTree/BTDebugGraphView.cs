using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using UyiCore.BT;

namespace UyiCore.EditorTools
{
    /// <summary>
    /// Canvas CHỈ ĐỌC hiển thị 1 cây từ <see cref="BehaviorTreeAsset"/> và tô màu theo
    /// <see cref="BTDebugTrace"/> live. Pan/zoom để xem; không sửa được (GetCompatiblePorts rỗng).
    /// </summary>
    internal sealed class BTDebugGraphView : GraphView
    {
        readonly Dictionary<string, BTDebugNodeView> _views = new Dictionary<string, BTDebugNodeView>();

        public BTDebugGraphView()
        {
            style.flexGrow = 1;
            Insert(0, new GridBackground());
            this.AddManipulator(new ContentZoomer());
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
        }

        // Read-only: không cho nối dây.
        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter) => new List<Port>();

        public void Build(BehaviorTreeAsset asset)
        {
            DeleteElements(graphElements.ToList());
            _views.Clear();
            if (asset == null || asset.nodes == null) return;

            bool needLayout = asset.nodes.All(n => n == null || (Mathf.Approximately(n.x, 0f) && Mathf.Approximately(n.y, 0f)));

            foreach (var n in asset.nodes)
            {
                if (n == null || string.IsNullOrEmpty(n.id)) continue;
                var v = new BTDebugNodeView(n);
                v.SetPosition(new Rect(n.x, n.y, 180, 110));
                AddElement(v);
                _views[n.id] = v;
            }

            foreach (var n in asset.nodes)
            {
                if (n?.children == null || !_views.TryGetValue(n.id, out var parent)) continue;
                foreach (var cid in n.children)
                    if (_views.TryGetValue(cid, out var child) && parent.Output != null && child.Input != null)
                        AddElement(parent.Output.ConnectTo(child.Input));
            }

            if (needLayout) AutoLayout(asset);
        }

        public void ApplyStatus(BTDebugTrace trace)
        {
            if (trace == null) return;
            float now = trace.LastUpdate;
            foreach (var kv in _views)
            {
                bool has = trace.Probes.TryGetValue(kv.Key, out var pr);
                bool fresh = has && (now - pr.time) < 0.2f;
                kv.Value.SetStatus(has, has ? pr.status : NodeStatus.Failure, fresh);
            }
        }

        // Auto-layout đơn giản (khi asset không có toạ độ x/y).
        void AutoLayout(BehaviorTreeAsset asset)
        {
            var placed = new HashSet<string>();
            float cursor = 0f; const float dx = 210f, dy = 140f, w = 180f;

            float Layout(string id, int depth)
            {
                var n = asset.Find(id);
                if (n == null || !placed.Add(id)) return cursor;
                var kids = n.children ?? new List<string>();
                if (kids.Count == 0)
                {
                    float x = cursor;
                    if (_views.TryGetValue(id, out var lv)) lv.SetPosition(new Rect(x, depth * dy, w, 110));
                    cursor += dx;
                    return x + w * 0.5f;
                }
                float first = -1f, last = 0f;
                foreach (var c in kids) { float cx = Layout(c, depth + 1); if (first < 0f) first = cx; last = cx; }
                float mid = (first + last) * 0.5f - w * 0.5f;
                if (_views.TryGetValue(id, out var v)) v.SetPosition(new Rect(mid, depth * dy, w, 110));
                return mid + w * 0.5f;
            }

            if (!string.IsNullOrEmpty(asset.rootId)) Layout(asset.rootId, 0);
        }
    }
}
