using UnityEngine;

namespace Patch47.Patch
{
    /// 帕奇灰盒形象:发光的"未保存代码块"(方块)+ 闪烁光标(眼睛)。
    /// 情绪(neutral/happy/sad/scared/excited)→ 光标颜色与闪烁频率(AGENTS.md 第 3 节)。
    public class PatchAvatar : MonoBehaviour
    {
        public Renderer blockRenderer;
        public Renderer cursorRenderer;
        public Transform cursor;
        [Range(0f, 0.3f)] public float floatAmount = 0.1f; // 光标轻微悬浮

        private string emotion = "neutral";
        private float phase;
        private float baseY;

        private void Awake()
        {
            if (cursor != null) baseY = cursor.localPosition.y;
        }

        public void SetEmotion(string value)
        {
            if (!string.IsNullOrEmpty(value)) emotion = value;
        }

        private void Update()
        {
            Color color;
            float frequency;
            float peak = 1f;
            switch (emotion)
            {
                case "happy":   color = FromRgb(0xB9, 0xFF, 0xE3); frequency = 6f;  break; // 轻快亮青
                case "sad":     color = FromRgb(0x6E, 0x9E, 0xC8); frequency = 1.1f; peak = 0.6f; break; // 迟缓暗淡
                case "scared":  color = FromRgb(0xFF, 0x6B, 0x57); frequency = 11f; peak = 0.8f; break; // 高频警示红
                case "excited": color = FromRgb(0xE8, 0xFF, 0xFF); frequency = 13f; break; // 很快很亮
                default:        color = FromRgb(0x9A, 0xE6, 0xFF); frequency = 3f;  break; // 帕奇本体 #9AE6FF
            }

            phase += Time.deltaTime * frequency;
            var blink = 0.5f + 0.5f * Mathf.Sin(phase); // 0~1

            if (cursorRenderer != null)
            {
                cursorRenderer.material.color = color * Mathf.Lerp(0.35f, 1f, blink);
            }

            if (cursor != null)
            {
                var scale = cursor.localScale;
                scale.y = Mathf.Lerp(0.25f, 1f, blink) * peak;
                cursor.localScale = scale;

                var pos = cursor.localPosition;
                pos.y = baseY + floatAmount * blink;
                pos.x = emotion == "scared" ? Mathf.Sin(phase * 3.7f) * 0.03f : 0f; // 害怕时轻微发抖
                cursor.localPosition = pos;
            }
        }

        private static Color FromRgb(byte r, byte g, byte b)
        {
            return new Color32(r, g, b, 255);
        }
    }
}
