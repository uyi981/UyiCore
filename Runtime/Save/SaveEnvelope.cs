using System;

namespace UyiCore.Save
{
    /// <summary>
    /// Container bao quanh user data — tách meta khỏi payload để load meta nhanh
    /// và versioning rõ ràng. Internal vì user không cần biết.
    /// </summary>
    [Serializable]
    internal class SaveEnvelope<T>
    {
        public int version = 1;
        public long timestamp;
        public string label;
        public T data;
    }
}
