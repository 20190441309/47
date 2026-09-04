using Patch47.GameFramework;
using Patch47.Patch;
using Patch47.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Patch47.Dialogue
{
    /// 对话编排:建立会话 → 发送 → 打字机播放;失败/超时自动切离线兜底(AGENTS.md 4.1)。
    /// 情绪字段驱动帕奇光标动画与打字速度。
    public class DialogueManager : MonoBehaviour
    {
        [Header("UI 引用(灰盒场景构建器注入)")]
        public InputField inputField;
        public Button sendButton;
        public QuickReplyRow quickReplies;
        public Typewriter typewriter;
        public PatchAvatar patch;
        public Text stageLabel;
        public GameObject offlineIndicator;

        private string sessionId;
        private string stage = "ch1_arrival";
        private int trust = 50;
        private bool offline;
        private bool busy;

        private IEnumerator Start()
        {
            SetInteractable(false);
            if (offlineIndicator != null) offlineIndicator.SetActive(false);
            UpdateStageLabel();
            yield return ApiClient.CreateSession(OnSessionCreated, OnSessionFailed);
        }

        private void OnSessionCreated(SessionResponseDto dto)
        {
            sessionId = dto.sessionId;
            if (dto.state != null)
            {
                if (!string.IsNullOrEmpty(dto.state.stage)) stage = dto.state.stage;
                trust = dto.state.trust;
            }
            RefreshQuickReplies();
            SetInteractable(true);
            ShowReply(FallbackDialogue.NextReply(stage), "neutral"); // 开场白走兜底,保证进入就有戏
        }

        private void OnSessionFailed(string error)
        {
            EnterOffline($"session: {error}");
        }

        public void OnSend()
        {
            var text = inputField != null ? inputField.text : string.Empty;
            text = text == null ? string.Empty : text.Trim();
            if (text.Length == 0 || busy) return;
            if (text.Length > GameConfig.MaxPlayerInputLength) text = text.Substring(0, GameConfig.MaxPlayerInputLength);
            inputField.text = string.Empty;
            StartCoroutine(SendRoutine(text));
        }

        public void OnQuickReply(int index)
        {
            if (busy) return;
            var labels = FallbackDialogue.QuickReplies(stage);
            if (index < 0 || index >= labels.Count) return;
            StartCoroutine(SendRoutine(labels[index]));
        }

        private IEnumerator SendRoutine(string text)
        {
            busy = true;
            SetInteractable(false);
            if (typewriter != null && typewriter.targetText != null) typewriter.targetText.text = "……";
            if (offline)
            {
                yield return new WaitForSeconds(0.25f);
                ShowReply(FallbackDialogue.NextReply(stage), "neutral");
            }
            else
            {
                yield return ApiClient.SendChat(sessionId, text, OnChatSuccess, OnChatFailed);
            }
            busy = false;
            SetInteractable(true);
        }

        private void OnChatSuccess(ChatResponseDto dto)
        {
            if (!string.IsNullOrEmpty(dto.stage) && dto.stage != stage)
            {
                stage = dto.stage;
                RefreshQuickReplies();
            }
            trust = dto.trust;
            ShowReply(dto.reply, dto.emotion);
        }

        private void OnChatFailed(string error)
        {
            // 一次失败即切离线,后续不再请求;重连入口 M2 再说(设计备忘)
            EnterOffline($"chat: {error}");
            ShowReply(FallbackDialogue.NextReply(stage), "neutral");
        }

        private void ShowReply(string reply, string emotion)
        {
            if (patch != null) patch.SetEmotion(emotion);
            if (typewriter != null) typewriter.Play(reply, SpeedFor(emotion));
            UpdateStageLabel();
        }

        private static float SpeedFor(string emotion)
        {
            switch (emotion)
            {
                case "happy":
                case "excited": return 1.25f;
                case "sad": return 0.7f;
                default: return 1f;
            }
        }

        private void EnterOffline(string reason)
        {
            if (offline) return;
            offline = true;
            if (offlineIndicator != null) offlineIndicator.SetActive(true);
            Debug.LogWarning($"[DialogueManager] 进入离线兜底:{reason}");
        }

        private void RefreshQuickReplies()
        {
            if (quickReplies != null) quickReplies.SetLabels(FallbackDialogue.QuickReplies(stage));
        }

        private void UpdateStageLabel()
        {
            if (stageLabel != null) stageLabel.text = $"{stage}  trust {trust}";
        }

        private void SetInteractable(bool value)
        {
            if (sendButton != null) sendButton.interactable = value;
            if (inputField != null) inputField.interactable = value;
        }
    }
}
