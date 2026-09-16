using System;

namespace UyiCore.EditorTools
{
    /// <summary>
    /// DTO khớp schema JSON của tool UI layout (HTML export *.uijson).
    /// Node dạng danh sách phẳng (id + children ids) — JsonUtility parse thẳng được.
    /// Toạ độ tuyệt đối theo px, gốc trên-trái, trong hệ quy chiếu canvas (canvasW × canvasH).
    /// </summary>
    [Serializable]
    public class UiLayoutJson
    {
        public string name;
        public float canvasW = 1080f;
        public float canvasH = 1920f;
        public string root;
        public UiNodeJson[] nodes;
    }

    [Serializable]
    public class UiNodeJson
    {
        public string id;
        public string name;      // tên GameObject (optional; trống → dùng id)
        public float x, y;       // offset từ góc trên-trái của cha (px)
        public float w, h;       // kích thước (px)
        public string color;     // hex #RRGGBB / #RRGGBBAA — Image tint hoặc màu chữ (text)
        public string sprite;    // tên hoặc path sprite (optional) — resolve qua AssetDatabase
        public string kind;      // "box" (mặc định) → Image · "text" → TextMeshProUGUI
        public string text;      // nội dung (khi kind="text")
        public float fontSize;   // cỡ chữ (khi kind="text")
        public string align;     // left/center/right (căn chữ ngang)
        public string valign;    // top/middle/bottom (căn chữ dọc)
        public string[] children;
    }
}
