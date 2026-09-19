using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UyiCore.BT;

namespace UyiCore.EditorTools
{
    /// <summary>
    /// Cửa sổ debug Behavior Tree (chỉ xem). Vào Play + Compile(debug:true) → chọn cây trong
    /// dropdown → canvas tô màu node đang chạy theo thời gian thực.
    /// Mở: Tools ▸ UyiCore ▸ Behavior Tree Debugger.
    /// </summary>
    public sealed class BTDebugWindow : EditorWindow
    {
        BTDebugGraphView _graph;
        ToolbarMenu _picker;
        Label _info;

        BTDebugSession _selSession;
        int _lastCount = -1;
        BehaviorTreeAsset _builtAsset;
        double _nextPoll;

        [MenuItem("Tools/UyiCore/Behavior Tree Debugger", priority = 100)]
        public static void Open()
        {
            var w = GetWindow<BTDebugWindow>();
            w.titleContent = new GUIContent("BT Debug");
            w.minSize = new Vector2(620, 400);
        }

        void OnEnable()
        {
            rootVisualElement.Clear();

            var bar = new Toolbar();
            _picker = new ToolbarMenu { text = "Cây: (chọn)" };
            bar.Add(_picker);
            bar.Add(new ToolbarButton(RebuildPicker) { text = "⟳" });
            _info = new Label { style = { marginLeft = 8, unityTextAlign = TextAnchor.MiddleLeft } };
            bar.Add(_info);
            rootVisualElement.Add(bar);

            _graph = new BTDebugGraphView();
            rootVisualElement.Add(_graph);

            EditorApplication.update += OnUpdate;
            EditorApplication.playModeStateChanged += OnPlayMode;
            RebuildPicker();
        }

        void OnDisable()
        {
            EditorApplication.update -= OnUpdate;
            EditorApplication.playModeStateChanged -= OnPlayMode;
        }

        void OnPlayMode(PlayModeStateChange s)
        {
            if (s == PlayModeStateChange.ExitingPlayMode)
            {
                BTDebug.Clear();
                _selSession = null; _builtAsset = null;
                _graph.Build(null);
                RebuildPicker();
            }
        }

        void RebuildPicker()
        {
            BTDebug.Prune();
            _picker.menu.MenuItems().Clear();
            var sessions = BTDebug.Sessions;
            foreach (var sess in sessions)
            {
                var captured = sess;
                _picker.menu.AppendAction(sess.Label, _ => Select(captured));
            }
            _lastCount = sessions.Count;
            if (sessions.Count == 0)
                _picker.text = "Cây: (chưa có — vào Play + Compile(debug:true))";
        }

        void Select(BTDebugSession s)
        {
            _selSession = s;
            if (s != null)
            {
                _picker.text = "Cây: " + s.Label;
                _builtAsset = s.Asset;
                _graph.Build(s.Asset);
            }
        }

        BTDebugSession Cur()
        {
            return (_selSession != null && BTDebug.Sessions.Contains(_selSession)) ? _selSession : null;
        }

        void OnUpdate()
        {
            if (EditorApplication.timeSinceStartup < _nextPoll) return;
            _nextPoll = EditorApplication.timeSinceStartup + 0.05;

            if (BTDebug.Sessions.Count != _lastCount) RebuildPicker();

            var s = Cur();
            if (s == null) { _info.text = ""; return; }

            if (s.Asset != _builtAsset) { _builtAsset = s.Asset; _graph.Build(s.Asset); }
            _graph.ApplyStatus(s.Trace);
            _info.text = Application.isPlaying ? "▶ live" : "⏸ (ngoài Play)";
            Repaint();
        }
    }
}
