using System.Collections.Generic;
using UnityEngine;

namespace UyiCore.BT
{
    /// <summary>
    /// ScriptableObject chứa 1 cây behavior ở dạng data (template dùng chung).
    /// Không có state runtime — mỗi enemy dùng <see cref="BehaviorTreeCompiler"/> để
    /// build ra 1 cây <see cref="BehaviorTree{TOwner}"/> riêng (state độc lập).
    ///
    /// Tạo bằng: Assets ▸ Create ▸ UyiCore ▸ Behavior Tree Asset,
    /// hoặc import từ file *.btjson (tool HTML export).
    /// </summary>
    [CreateAssetMenu(menuName = "UyiCore/Behavior Tree Asset", fileName = "NewBehaviorTree", order = 200)]
    public class BehaviorTreeAsset : ScriptableObject
    {
        public string treeName;
        public string rootId;
        public List<BTNodeData> nodes = new List<BTNodeData>();

        /// <summary>Parse JSON (schema tool HTML) → 1 asset mới (runtime-safe, chạy được trong build).</summary>
        public static BehaviorTreeAsset FromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            BTGraphJson dto;
            try { dto = JsonUtility.FromJson<BTGraphJson>(json); }
            catch (System.Exception e)
            {
                Debug.LogError($"[BehaviorTreeAsset] JSON parse fail: {e.Message}");
                return null;
            }
            if (dto == null) return null;

            var asset = CreateInstance<BehaviorTreeAsset>();
            asset.Populate(dto);
            return asset;
        }

        /// <summary>Nạp dữ liệu từ DTO vào asset (overwrite). Dùng cho importer / reimport.</summary>
        public void Populate(BTGraphJson dto)
        {
            treeName = dto.name;
            rootId = dto.root;
            nodes = dto.nodes != null ? new List<BTNodeData>(dto.nodes) : new List<BTNodeData>();
            if (!string.IsNullOrEmpty(treeName)) name = treeName;
        }

        /// <summary>Tra node theo id (linear — cây nhỏ nên ok). Null nếu không có.</summary>
        public BTNodeData Find(string id)
        {
            if (nodes == null) return null;
            for (int i = 0; i < nodes.Count; i++)
                if (nodes[i] != null && nodes[i].id == id) return nodes[i];
            return null;
        }

        /// <summary>
        /// Kiểm tra cấu trúc (không cần registry): root tồn tại, mọi child resolve được,
        /// leaf có ref, không trùng id. Trả list message rỗng nếu ok.
        /// </summary>
        public List<string> Validate()
        {
            var errors = new List<string>();
            if (nodes == null || nodes.Count == 0) { errors.Add("Cây rỗng (không có node)."); return errors; }

            var ids = new HashSet<string>();
            foreach (var n in nodes)
            {
                if (n == null) { errors.Add("Có node null."); continue; }
                if (string.IsNullOrEmpty(n.id)) { errors.Add("Có node thiếu id."); continue; }
                if (!ids.Add(n.id)) errors.Add($"Id trùng: '{n.id}'.");
            }

            if (string.IsNullOrEmpty(rootId)) errors.Add("Thiếu rootId.");
            else if (Find(rootId) == null) errors.Add($"rootId '{rootId}' không tồn tại trong nodes.");

            foreach (var n in nodes)
            {
                if (n == null || string.IsNullOrEmpty(n.id)) continue;

                if (n.children != null)
                    foreach (var c in n.children)
                        if (string.IsNullOrEmpty(c) || Find(c) == null)
                            errors.Add($"Node '{n.id}' trỏ tới child '{c}' không tồn tại.");

                bool isLeafBind = n.type == "Action" || n.type == "Condition" || n.type == "Do";
                if (isLeafBind && string.IsNullOrEmpty(n.@ref))
                    errors.Add($"Node '{n.id}' ({n.type}) thiếu 'ref' (id đăng ký).");
            }
            return errors;
        }
    }
}
