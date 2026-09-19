using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using UyiCore.BT;

namespace UyiCore.EditorTools
{
    /// <summary>
    /// Node CHỈ ĐỌC trong cửa sổ debugger. Dựng từ <see cref="BTNodeData"/> (cấu trúc/nhãn),
    /// tô màu theo status live: Running = vàng, Success = xanh, Failure = đỏ, không tick = mờ.
    /// </summary>
    internal sealed class BTDebugNodeView : Node
    {
        public readonly string Id;
        public Port Input, Output;
        readonly VisualElement _bar;

        public BTDebugNodeView(BTNodeData data)
        {
            Id = data.id;
            title = data.type;
            capabilities &= ~Capabilities.Deletable;   // không cho xoá (view read-only)

            Input = InstantiatePort(Orientation.Vertical, Direction.Input, Port.Capacity.Single, typeof(bool));
            Input.portName = "";
            var top = new VisualElement { style = { alignItems = Align.Center } };
            top.Add(Input); mainContainer.Insert(0, top);

            Output = InstantiatePort(Orientation.Vertical, Direction.Output, Port.Capacity.Multi, typeof(bool));
            Output.portName = "";
            var bot = new VisualElement { style = { alignItems = Align.Center } };
            bot.Add(Output); mainContainer.Add(bot);

            titleContainer.style.backgroundColor = Cat(data.type);

            string sub = !string.IsNullOrEmpty(data.name) ? data.name : data.@ref;
            if (!string.IsNullOrEmpty(sub))
            {
                var body = new Label(sub) { style = { marginLeft = 6, marginRight = 6, whiteSpace = WhiteSpace.Normal } };
                extensionContainer.Add(body);
            }
            _bar = new VisualElement { style = { height = 5, marginTop = 2, borderTopLeftRadius = 2, borderTopRightRadius = 2, borderBottomLeftRadius = 2, borderBottomRightRadius = 2 } };
            extensionContainer.Add(_bar);

            RefreshExpandedState();
        }

        public void SetStatus(bool has, NodeStatus s, bool fresh)
        {
            Color c = (!has || !fresh) ? new Color(1f, 1f, 1f, 0.06f) : StatusColor(s);
            _bar.style.backgroundColor = c;

            float w = (has && fresh) ? 2f : 0f;
            Color bc = (has && fresh) ? c : Color.clear;
            style.borderTopWidth = style.borderBottomWidth = style.borderLeftWidth = style.borderRightWidth = w;
            style.borderTopColor = style.borderBottomColor = style.borderLeftColor = style.borderRightColor = bc;
        }

        static Color StatusColor(NodeStatus s)
        {
            switch (s)
            {
                case NodeStatus.Running: return new Color(1f, 0.75f, 0.2f);
                case NodeStatus.Success: return new Color(0.3f, 0.8f, 0.45f);
                default: return new Color(0.85f, 0.32f, 0.27f); // Failure
            }
        }

        static Color Cat(string type)
        {
            switch (type)
            {
                case "Selector": case "Sequence": case "Parallel":
                case "ReactiveSelector": case "ReactiveSequence": return new Color(0.20f, 0.35f, 0.55f);
                case "Inverter": case "Repeater": case "Cooldown":
                case "UntilSuccess": case "UntilFailure": return new Color(0.42f, 0.28f, 0.52f);
                case "Action": return new Color(0.20f, 0.45f, 0.30f);
                case "Condition": return new Color(0.55f, 0.42f, 0.15f);
                case "Do": return new Color(0.18f, 0.42f, 0.45f);
                default: return new Color(0.35f, 0.35f, 0.38f);
            }
        }
    }
}
