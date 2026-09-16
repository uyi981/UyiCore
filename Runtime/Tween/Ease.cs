using UnityEngine;

namespace UyiCore.Tweening
{
    public enum Ease
    {
        Linear,
        InQuad, OutQuad, InOutQuad,
        InCubic, OutCubic, InOutCubic,
        InBack, OutBack, InOutBack,
        OutBounce,
        InOutSine,
        OutElastic,
    }

    /// <summary>Hàm easing thuần: nhận t (0..1), trả giá trị eased (một số có overshoot >1, dùng LerpUnclamped).</summary>
    public static class Easing
    {
        public static float Evaluate(Ease e, float t)
        {
            t = Mathf.Clamp01(t);
            switch (e)
            {
                case Ease.Linear: return t;
                case Ease.InQuad: return t * t;
                case Ease.OutQuad: return t * (2f - t);
                case Ease.InOutQuad: return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
                case Ease.InCubic: return t * t * t;
                case Ease.OutCubic: return 1f - Mathf.Pow(1f - t, 3f);
                case Ease.InOutCubic: return t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
                case Ease.InBack: { const float s = 1.70158f; return t * t * ((s + 1f) * t - s); }
                case Ease.OutBack: { const float s = 1.70158f; float u = t - 1f; return 1f + u * u * ((s + 1f) * u + s); }
                case Ease.InOutBack:
                {
                    const float c2 = 1.70158f * 1.525f;
                    return t < 0.5f
                        ? (Mathf.Pow(2f * t, 2f) * ((c2 + 1f) * 2f * t - c2)) / 2f
                        : (Mathf.Pow(2f * t - 2f, 2f) * ((c2 + 1f) * (t * 2f - 2f) + c2) + 2f) / 2f;
                }
                case Ease.OutBounce: return OutBounce(t);
                case Ease.InOutSine: return -(Mathf.Cos(Mathf.PI * t) - 1f) / 2f;
                case Ease.OutElastic:
                {
                    if (t <= 0f) return 0f;
                    if (t >= 1f) return 1f;
                    const float c4 = (2f * Mathf.PI) / 3f;
                    return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c4) + 1f;
                }
                default: return t;
            }
        }

        static float OutBounce(float t)
        {
            const float n1 = 7.5625f, d1 = 2.75f;
            if (t < 1f / d1) return n1 * t * t;
            if (t < 2f / d1) { t -= 1.5f / d1; return n1 * t * t + 0.75f; }
            if (t < 2.5f / d1) { t -= 2.25f / d1; return n1 * t * t + 0.9375f; }
            t -= 2.625f / d1; return n1 * t * t + 0.984375f;
        }
    }
}
