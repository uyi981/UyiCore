using System.Collections.Generic;
using UyiCore.Patterns;
using UnityEngine;

namespace UyiCore.FloatingText
{
    /// <summary>
    /// Singleton spawn floating text qua pool. Static API <see cref="Show"/>
    /// gọi được từ bất cứ đâu (PlayerHit, EnemyDeath, PickupCollect...).
    ///
    /// Setup: kéo prefab có <see cref="FloatingTextItem"/> + TMP_Text + CanvasGroup
    /// vào field <c>_prefab</c>. Canvas world-space tự tạo runtime.
    /// </summary>
    public class FloatingTextService : SingletonBehaviour<FloatingTextService>
    {
        [Header("Prefab")]
        [SerializeField] private FloatingTextItem _prefab;

        [Header("Pool")]
        [SerializeField] private int _prewarm = 8;
        [SerializeField] private int _maxConcurrent = 32;

        [Header("Canvas")]
        [Tooltip("Scale của World-Space Canvas. 2D pixel-art thường 0.01–0.05.")]
        [SerializeField] private float _canvasScale = 0.05f;
        [SerializeField] private int _sortingOrder = 1000;

        private Canvas _canvas;
        private readonly Queue<FloatingTextItem> _idle = new Queue<FloatingTextItem>();
        private readonly List<FloatingTextItem> _active = new List<FloatingTextItem>();

        protected override void OnAwake()
        {
            base.OnAwake();
            BuildCanvas();
            Prewarm();
        }

        void BuildCanvas()
        {
            var go = new GameObject("FloatingTextCanvas");
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.zero;
            go.transform.localScale = Vector3.one * _canvasScale;
            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.WorldSpace;
            _canvas.sortingOrder = _sortingOrder;
        }

        void Prewarm()
        {
            if (_prefab == null) return;
            for (int i = 0; i < _prewarm; i++)
            {
                var item = CreateItem();
                item.gameObject.SetActive(false);
                _idle.Enqueue(item);
            }
        }

        FloatingTextItem CreateItem()
        {
            var item = Instantiate(_prefab, _canvas.transform);
            item.transform.localScale = Vector3.one;
            return item;
        }

        // ---- Public API ----

        public static void Show(string text, Vector3 worldPos)
        {
            Show(text, worldPos, FloatingTextStyle.Default);
        }

        public static void Show(string text, Vector3 worldPos, FloatingTextStyle style)
        {
            if (Instance == null)
            {
                Debug.LogWarning("[FloatingTextService] Instance null — đã add service vào Bootstrap chưa?");
                return;
            }
            Instance.SpawnInternal(text, worldPos, style);
        }

        public static void ShowDamage(int amount, Vector3 worldPos, bool crit = false)
        {
            Show(amount.ToString(), worldPos, crit ? FloatingTextStyle.Crit : FloatingTextStyle.Damage);
        }

        public static void ShowHeal(int amount, Vector3 worldPos)
        {
            Show("+" + amount, worldPos, FloatingTextStyle.Heal);
        }

        public static void ShowPickup(string label, Vector3 worldPos)
        {
            Show(label, worldPos, FloatingTextStyle.Pickup);
        }

        // ---- Internal ----

        void SpawnInternal(string text, Vector3 worldPos, FloatingTextStyle style)
        {
            if (_prefab == null)
            {
                Debug.LogError("[FloatingTextService] Prefab chưa gán.");
                return;
            }

            FloatingTextItem item;
            if (_idle.Count > 0)
            {
                item = _idle.Dequeue();
            }
            else if (_active.Count < _maxConcurrent)
            {
                item = CreateItem();
            }
            else
            {
                // Evict oldest active
                item = _active[0];
                _active.RemoveAt(0);
                item.gameObject.SetActive(false);
            }

            _active.Add(item);
            item.Spawn(text, style, worldPos, OnItemDespawn);
        }

        void OnItemDespawn(FloatingTextItem item)
        {
            _active.Remove(item);
            _idle.Enqueue(item);
        }
    }
}
