using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Patch47.Dialogue
{
    /// 打字机:后端整段返回,本地逐字播放——网络只一次请求,体验仍是"流式"(AGENTS.md 4.1)。
    public class Typewriter : MonoBehaviour
    {
        [Range(10, 60)] public int charsPerSecond = 30;
        public Text targetText;
        public event Action Finished;

        private Coroutine playing;

        public bool IsPlaying => playing != null;

        public void Play(string fullText, float speedMultiplier = 1f)
        {
            if (targetText == null) return;
            if (playing != null) StopCoroutine(playing);
            if (string.IsNullOrEmpty(fullText))
            {
                targetText.text = string.Empty;
                Finished?.Invoke();
                return;
            }
            playing = StartCoroutine(PlayRoutine(fullText, speedMultiplier));
        }

        private IEnumerator PlayRoutine(string fullText, float speedMultiplier)
        {
            var builder = new StringBuilder();
            var delay = 1f / Mathf.Max(5f, charsPerSecond * Mathf.Max(0.2f, speedMultiplier));
            targetText.text = string.Empty;
            foreach (var ch in fullText)
            {
                builder.Append(ch);
                targetText.text = builder.ToString();
                yield return new WaitForSeconds(PauseFor(ch) * delay);
            }
            playing = null;
            Finished?.Invoke();
        }

        // 标点稍作停顿,读起来像"人在说话"
        private static float PauseFor(char ch)
        {
            return ch == '。' || ch == '!' || ch == '?' || ch == '…' || ch == ',' || ch == '、' ? 3f : 1f;
        }
    }
}
