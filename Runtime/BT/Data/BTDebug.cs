using System.Collections.Generic;
using UnityEngine;

namespace UyiCore.BT
{
    /// <summary>
    /// Ghi status từng node theo id để cửa sổ debugger (editor) đọc. Runtime, build-safe.
    /// Chỉ tồn tại khi <see cref="BehaviorTreeCompiler.Compile"/> gọi với debug:true.
    /// </summary>
    public sealed class BTDebugTrace
    {
        public struct Probe { public NodeStatus status; public float time; }

        public readonly Dictionary<string, Probe> Probes = new Dictionary<string, Probe>();
        public float LastUpdate;

        public void Set(string id, NodeStatus s)
        {
            float t = Time.realtimeSinceStartup;
            Probes[id] = new Probe { status = s, time = t };
            LastUpdate = t;
        }
    }

    /// <summary>
    /// Bọc 1 node để ghi lại status (theo id) mỗi lần Tick. Compiler chèn khi debug:true;
    /// tắt debug thì không có wrapper → zero cost.
    /// </summary>
    public sealed class DebugNode<TOwner> : INode<TOwner>
    {
        public string Name { get; set; }

        private readonly string _id;
        private readonly INode<TOwner> _inner;
        private readonly BTDebugTrace _trace;

        public DebugNode(string id, INode<TOwner> inner, BTDebugTrace trace)
        {
            _id = id; _inner = inner; _trace = trace;
            Name = inner.Name;
        }

        public NodeStatus Tick(TOwner owner, float deltaTime)
        {
            var s = _inner.Tick(owner, deltaTime);
            _trace.Set(_id, s);
            return s;
        }

        public void Reset(TOwner owner) => _inner.Reset(owner);
    }

    /// <summary>1 phiên debug: nhãn + asset (lấy cấu trúc/layout) + trace (status live).</summary>
    public sealed class BTDebugSession
    {
        public string Label;
        public BehaviorTreeAsset Asset;
        public BTDebugTrace Trace;
        public System.WeakReference Owner;   // dọn khi owner đã chết
    }

    /// <summary>Sổ đăng ký các cây đang debug — editor window đọc danh sách này.</summary>
    public static class BTDebug
    {
        public static readonly List<BTDebugSession> Sessions = new List<BTDebugSession>();

        public static void Register(string label, BehaviorTreeAsset asset, BTDebugTrace trace, object owner)
        {
            Prune();
            Sessions.Add(new BTDebugSession
            {
                Label = label,
                Asset = asset,
                Trace = trace,
                Owner = new System.WeakReference(owner)
            });
        }

        public static void Prune()
        {
            for (int i = Sessions.Count - 1; i >= 0; i--)
            {
                var o = Sessions[i].Owner;
                if (o != null && !o.IsAlive) Sessions.RemoveAt(i);
            }
        }

        public static void Clear() => Sessions.Clear();
    }
}
