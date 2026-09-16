using UnityEngine;
using UyiCore.Tweening;

namespace UyiCore.UIEffect
{
    /// <summary>
    /// Preset "cảm giác" của 1 hiệu ứng UI (HOW): loại + duration + ease + tham số.
    /// Tách khỏi WHAT (target) và WHEN (trigger) → đổi 1 SO là cả game đổi feel.
    /// Gọi <see cref="Play"/> để chạy lên 1 RectTransform.
    /// </summary>
    [CreateAssetMenu(menuName = "UyiCore/UI Effect Preset", fileName = "NewUIEffect", order = 210)]
    public class UiEffectPreset : ScriptableObject
    {
        public enum Kind { Fade, Scale, Move, Rotate, Punch }

        [Header("Chung")]
        public Kind kind = Kind.Scale;
        public float duration = 0.25f;
        public Ease ease = Ease.OutBack;
        public float delay = 0f;
        [Tooltip("UI thường chạy cả khi timeScale = 0 (pause).")]
        public bool unscaledTime = true;

        [Header("Fade")] public float fromAlpha = 0f; public float toAlpha = 1f;
        [Header("Scale")] public float fromScale = 0.6f; public float toScale = 1f;
        [Header("Move — trượt từ offset về chỗ hiện tại")] public Vector2 fromOffset = new Vector2(0f, -60f);
        [Header("Rotate — euler")] public Vector3 fromEuler = new Vector3(0f, 0f, -12f); public Vector3 toEuler = Vector3.zero;
        [Header("Punch")] public float punchFactor = 1.15f;

        public TweenHandle Play(RectTransform rt)
        {
            if (rt == null) return default;
            switch (kind)
            {
                case Kind.Fade:
                {
                    var cg = GetOrAddCanvasGroup(rt);
                    cg.alpha = fromAlpha;
                    return cg.Fade(toAlpha, duration, ease, delay, unscaledTime);
                }
                case Kind.Scale:
                    rt.localScale = Vector3.one * fromScale;
                    return rt.ScaleTo(Vector3.one * toScale, duration, ease, delay, unscaledTime);

                case Kind.Move:
                {
                    Vector2 rest = rt.anchoredPosition;
                    rt.anchoredPosition = rest + fromOffset;
                    return rt.MoveAnchored(rest, duration, ease, delay, unscaledTime);
                }
                case Kind.Rotate:
                    rt.localEulerAngles = fromEuler;
                    return rt.RotateTo(toEuler, duration, ease, delay, unscaledTime);

                case Kind.Punch:
                default:
                    return rt.Punch(punchFactor, duration, unscaledTime);
            }
        }

        static CanvasGroup GetOrAddCanvasGroup(Component c)
        {
            var cg = c.GetComponent<CanvasGroup>();
            return cg != null ? cg : c.gameObject.AddComponent<CanvasGroup>();
        }
    }
}
