namespace UyiCore.Scenes
{
    public readonly struct SceneLoadStartedData
    {
        public readonly string from;
        public readonly string target;
        public SceneLoadStartedData(string from, string target) { this.from = from; this.target = target; }
    }

    public readonly struct SceneLoadProgressData
    {
        public readonly string target;
        public readonly float progress; // 0..1
        public SceneLoadProgressData(string target, float progress) { this.target = target; this.progress = progress; }
    }

    public readonly struct SceneLoadCompletedData
    {
        public readonly string target;
        public SceneLoadCompletedData(string target) { this.target = target; }
    }
}
