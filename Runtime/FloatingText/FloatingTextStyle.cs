using System;
using UnityEngine;

namespace UyiCore.FloatingText
{
    /// <summary>
    /// Preset style cho 1 floating text. Dùng các static (Damage/Crit/Heal/Pickup) hoặc tự tạo.
    /// </summary>
    [Serializable]
    public struct FloatingTextStyle
    {
        public Color color;
        public float fontSize;
        [Tooltip("Vận tốc world-space (unit/sec). Y dương = lên.")]
        public Vector2 velocity;
        public float lifetime;
        [Tooltip("Random offset world-space khi spawn (tránh chồng số).")]
        public Vector2 spawnJitter;

        public static FloatingTextStyle Default => new FloatingTextStyle
        {
            color = Color.white,
            fontSize = 4f,
            velocity = new Vector2(0f, 2f),
            lifetime = 1f,
            spawnJitter = new Vector2(0.3f, 0.1f),
        };

        public static FloatingTextStyle Damage => new FloatingTextStyle
        {
            color = new Color(1f, 0.85f, 0.3f),
            fontSize = 5f,
            velocity = new Vector2(0f, 2.2f),
            lifetime = 1.1f,
            spawnJitter = new Vector2(0.3f, 0.1f),
        };

        public static FloatingTextStyle Crit => new FloatingTextStyle
        {
            color = new Color(1f, 0.35f, 0.2f),
            fontSize = 7f,
            velocity = new Vector2(0f, 3f),
            lifetime = 1.3f,
            spawnJitter = new Vector2(0.4f, 0.1f),
        };

        public static FloatingTextStyle Heal => new FloatingTextStyle
        {
            color = new Color(0.4f, 1f, 0.5f),
            fontSize = 5f,
            velocity = new Vector2(0f, 2f),
            lifetime = 1.2f,
            spawnJitter = new Vector2(0.2f, 0.1f),
        };

        public static FloatingTextStyle Pickup => new FloatingTextStyle
        {
            color = new Color(0.8f, 0.9f, 1f),
            fontSize = 4f,
            velocity = new Vector2(0f, 1.5f),
            lifetime = 1.5f,
            spawnJitter = Vector2.zero,
        };
    }
}
