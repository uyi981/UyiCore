using UnityEngine;
using UnityEngine.UI;

namespace UyiCore.Tweening
{
    /// <summary>
    /// Extension tween cho prop UI thường dùng — 1 dòng là chạy. Mặc định <c>unscaled = true</c>
    /// (UI thường chạy cả khi timeScale = 0). Trả <see cref="TweenHandle"/> để cancel.
    /// Vd: <c>rect.ScaleTo(1f, .2f, Ease.OutBack); group.Fade(0f, .3f); img.FillTo(hp, .25f);</c>
    /// </summary>
    public static class UITween
    {
        public static TweenHandle Fade(this CanvasGroup cg, float to, float dur, Ease ease = Ease.OutQuad, float delay = 0f, bool unscaled = true)
        {
            if (cg == null) return default;
            float from = cg.alpha;
            return Tween.To(dur, t => { if (cg != null) cg.alpha = Mathf.LerpUnclamped(from, to, t); }, ease, delay, unscaled, cg);
        }

        public static TweenHandle ScaleTo(this Transform tr, Vector3 to, float dur, Ease ease = Ease.OutBack, float delay = 0f, bool unscaled = true)
        {
            if (tr == null) return default;
            Vector3 from = tr.localScale;
            return Tween.To(dur, t => { if (tr != null) tr.localScale = Vector3.LerpUnclamped(from, to, t); }, ease, delay, unscaled, tr);
        }

        public static TweenHandle ScaleTo(this Transform tr, float uniform, float dur, Ease ease = Ease.OutBack, float delay = 0f, bool unscaled = true)
            => tr.ScaleTo(Vector3.one * uniform, dur, ease, delay, unscaled);

        public static TweenHandle MoveAnchored(this RectTransform rt, Vector2 to, float dur, Ease ease = Ease.OutCubic, float delay = 0f, bool unscaled = true)
        {
            if (rt == null) return default;
            Vector2 from = rt.anchoredPosition;
            return Tween.To(dur, t => { if (rt != null) rt.anchoredPosition = Vector2.LerpUnclamped(from, to, t); }, ease, delay, unscaled, rt);
        }

        public static TweenHandle RotateTo(this Transform tr, Vector3 toEuler, float dur, Ease ease = Ease.OutCubic, float delay = 0f, bool unscaled = true)
        {
            if (tr == null) return default;
            Vector3 from = tr.localEulerAngles;
            return Tween.To(dur, t => { if (tr != null) tr.localEulerAngles = Vector3.LerpUnclamped(from, toEuler, t); }, ease, delay, unscaled, tr);
        }

        public static TweenHandle ColorTo(this Graphic g, Color to, float dur, Ease ease = Ease.OutQuad, float delay = 0f, bool unscaled = true)
        {
            if (g == null) return default;
            Color from = g.color;
            return Tween.To(dur, t => { if (g != null) g.color = Color.LerpUnclamped(from, to, t); }, ease, delay, unscaled, g);
        }

        public static TweenHandle FillTo(this Image img, float to, float dur, Ease ease = Ease.OutQuad, float delay = 0f, bool unscaled = true)
        {
            if (img == null) return default;
            float from = img.fillAmount;
            return Tween.To(dur, t => { if (img != null) img.fillAmount = Mathf.LerpUnclamped(from, to, t); }, ease, delay, unscaled, img);
        }

        /// <summary>Nảy scale: phồng lên <c>factor</c> lần rồi về base (click / hit feedback).</summary>
        public static TweenHandle Punch(this Transform tr, float factor = 1.15f, float dur = 0.25f, bool unscaled = true)
        {
            if (tr == null) return default;
            Vector3 baseScale = tr.localScale;
            float amp = factor - 1f;
            return Tween.To(dur, t =>
            {
                if (tr == null) return;
                float p = Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI);   // 0 → 1 → 0
                tr.localScale = baseScale * (1f + amp * p);
            }, Ease.Linear, 0f, unscaled, tr);
        }
    }
}
