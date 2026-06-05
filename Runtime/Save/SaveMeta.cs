using System;

namespace UyiCore.Save
{
    /// <summary>
    /// Metadata của 1 save slot — đọc nhanh để hiển thị danh sách slot
    /// ("Slot 1 - Level 12 - 2 giờ trước") mà không cần parse full data.
    /// </summary>
    [Serializable]
    public struct SaveMeta
    {
        public int slot;
        public int version;
        public long timestamp; // Unix seconds (UTC)
        public string label;   // Tuỳ user (vd "Level 12 — Wave 8")

        public DateTime TimestampUtc => DateTimeOffset.FromUnixTimeSeconds(timestamp).UtcDateTime;
        public DateTime TimestampLocal => TimestampUtc.ToLocalTime();
    }
}
