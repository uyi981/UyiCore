using UyiCore.Observer;

namespace UyiCore.Scenes
{
    public readonly struct SceneLoadStartedData : IEventData
    {
        public readonly string from;
        public readonly string target;
        public SceneLoadStartedData(string from, string target) { this.from = from; this.target = target; }
    }

    public readonly struct SceneLoadProgressData : IEventData
    {
        public readonly string target;
        public readonly float progress; // 0..1
        public SceneLoadProgressData(string target, float progress) { this.target = target; this.progress = progress; }
    }

    public readonly struct SceneLoadCompletedData : IEventData
    {
        public readonly string target;
        public SceneLoadCompletedData(string target) { this.target = target; }
    }
}
