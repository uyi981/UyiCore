using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace UyiCore.Scenes
{
    /// <summary>
    /// CanvasGroup overlay fade in/out cho scene transition. DontDestroyOnLoad.
    /// Tạo runtime qua <see cref="CreateRuntime"/> hoặc gán prefab vào SceneLoader.
    /// </summary>
    public class SceneTransition : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _group;
        [SerializeField] private Image _image;

        public float Alpha => _group != null ? _group.alpha : 0f;

        public static SceneTransition CreateRuntime()
        {
            var go = new GameObject("[SceneTransition]");
            DontDestroyOnLoad(go);

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999;
            go.AddComponent<CanvasScaler>();
            go.AddComponent<GraphicRaycaster>();

            var group = go.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;

            var imgGo = new GameObject("Fade");
            imgGo.transform.SetParent(go.transform, false);
            var img = imgGo.AddComponent<Image>();
            img.color = Color.black;
            img.raycastTarget = true;
            var rt = img.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var t = go.AddComponent<SceneTransition>();
            t._group = group;
            t._image = img;
            return t;
        }

        public void SetColor(Color c)
        {
            if (_image != null) _image.color = c;
        }

        public void SetAlphaImmediate(float a)
        {
            if (_group == null) return;
            _group.alpha = Mathf.Clamp01(a);
            _group.blocksRaycasts = _group.alpha > 0.01f;
        }

        public IEnumerator FadeOut(float duration)
        {
            if (_group == null) yield break;
            _group.blocksRaycasts = true;
            yield return FadeRoutine(_group.alpha, 1f, duration);
        }

        public IEnumerator FadeIn(float duration)
        {
            if (_group == null) yield break;
            yield return FadeRoutine(_group.alpha, 0f, duration);
            _group.blocksRaycasts = false;
        }

        IEnumerator FadeRoutine(float from, float to, float duration)
        {
            if (duration <= 0f)
            {
                _group.alpha = to;
                yield break;
            }
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                _group.alpha = Mathf.Lerp(from, to, t / duration);
                yield return null;
            }
            _group.alpha = to;
        }
    }
}
