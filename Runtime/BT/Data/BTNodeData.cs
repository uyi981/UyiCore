using System;
using System.Collections.Generic;
using System.Globalization;

namespace UyiCore.BT
{
    /// <summary>
    /// 1 node ở dạng DATA (không state runtime). Dùng chung cho:
    ///  - DTO parse JSON (mảng trong <see cref="BTGraphJson"/>)
    ///  - lưu trong <see cref="BehaviorTreeAsset"/> (SO)
    /// Compiler dịch data này → node runtime (INode&lt;TOwner&gt;) khi spawn.
    /// </summary>
    [Serializable]
    public class BTNodeData
    {
        /// <summary>Id duy nhất trong cây (editor tự sinh, vd "n0", "n1a").</summary>
        public string id;

        /// <summary>Loại node cấu trúc: Selector/Sequence/Parallel/Inverter/Repeater/
        /// Cooldown/UntilSuccess/UntilFailure/Wait/Succeed/Fail — hoặc leaf bind registry: Action/Condition/Do.</summary>
        public string type;

        /// <summary>Chỉ leaf (Action/Condition/Do) dùng: id đăng ký trong <see cref="BTRegistry{TOwner}"/>.</summary>
        public string @ref;

        /// <summary>Nhãn hiển thị (optional, editor gán). Không ảnh hưởng logic.</summary>
        public string name;

        /// <summary>Id các child. Composite: N child; Decorator: dùng child[0]; Leaf: rỗng.</summary>
        public List<string> children = new List<string>();

        /// <summary>Tham số key-value (value luôn là string, parse theo kiểu khi build).</summary>
        public List<BTParam> @params = new List<BTParam>();

        /// <summary>Toạ độ node trong graph editor (chỉ dùng cho editor GraphView).
        /// Runtime/compiler BỎ QUA hoàn toàn. Xuất .btjson cũng không kèm 2 field này
        /// (BTGraphIO ghi JSON thủ công) để giữ schema sạch, tương thích tool HTML cũ.</summary>
        public float x;
        public float y;
    }

    /// <summary>1 cặp key-value. Value giữ dạng string để round-trip JSON ↔ SO đơn giản.</summary>
    [Serializable]
    public class BTParam
    {
        public string key;
        public string value;
    }

    /// <summary>DTO khớp schema JSON của tool HTML. JsonUtility parse thẳng vào đây.</summary>
    [Serializable]
    public class BTGraphJson
    {
        public string name;
        public string root;
        public BTNodeData[] nodes;
    }

    /// <summary>
    /// Wrapper đọc param có kiểu từ <see cref="BTNodeData.@params"/>.
    /// Parse dùng InvariantCulture (JSON số dùng '.') — tránh lỗi locale máy VN dùng ','.
    /// </summary>
    public readonly struct BTParams
    {
        private readonly List<BTParam> _list;
        public BTParams(List<BTParam> list) { _list = list; }

        private string Raw(string key)
        {
            if (_list == null) return null;
            for (int i = 0; i < _list.Count; i++)
                if (_list[i] != null && _list[i].key == key) return _list[i].value;
            return null;
        }

        public bool Has(string key) => Raw(key) != null;

        public string GetString(string key, string fallback = null)
        {
            var v = Raw(key);
            return v ?? fallback;
        }

        public float GetFloat(string key, float fallback = 0f)
        {
            var v = Raw(key);
            return float.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out var r) ? r : fallback;
        }

        public int GetInt(string key, int fallback = 0)
        {
            var v = Raw(key);
            return int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out var r) ? r : fallback;
        }

        public bool GetBool(string key, bool fallback = false)
        {
            var v = Raw(key);
            if (string.IsNullOrEmpty(v)) return fallback;
            v = v.Trim().ToLowerInvariant();
            return v == "true" || v == "1" || v == "yes";
        }
    }
}
