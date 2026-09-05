using System.Collections;
using System.Collections.Generic;
using Patch47.GameFramework;
using Patch47.Patch;
using Patch47.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Patch47.Dialogue
{
    /// 对话编排:读档 → 恢复/新建会话 → 发送 → 打字机播放;失败/超时自动切离线兜底(AGENTS.md 4.1)。
    /// M2:bug 修复经 POST /api/event 驱动阶段推进(对话说"修好了"不算,见契约);离线时本地镜像推进。
    /// 存档时机:会话建立/恢复后、每次对话后、修复后(AGENTS.md 4.4 自动存)。
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
        private readonly List<string> flagNames = new List<string>();
        private bool offline;
        private bool busy;

        private IEnumerator Start()
        {
            SetInteractable(false);
            if (offlineIndicator != null) offlineIndicator.SetActive(false);
            UpdateStageLabel();

            // 读本地存档:有 sessionId 先恢复;服务端会话丢了(404)再新建
            var save = SaveManager.Load();
            if (save != null)
            {
                sessionId = save.sessionId;
                stage = save.stage;
                trust = save.trust;
                flagNames.Clear();
                if (save.flagNames != null) flagNames.AddRange(save.flagNames);
                UpdateStageLabel();
            }
            if (!string.IsNullOrEmpty(sessionId))
            {
                yield return ApiClient.CreateSession(sessionId, OnSessionResumed, OnResumeFailed);
            }
            else
            {
                yield return ApiClient.CreateSession(null, OnSessionCreated, OnSessionFailed);
            }
        }

        // ---------- 会话生命周期 ----------

        private void OnSessionCreated(SessionResponseDto dto)
        {
            sessionId = dto.sessionId;
            AdoptState(dto.state);
            SaveState();
            EnterOnlineIntro();
        }

        private void OnSessionResumed(SessionResponseDto dto)
        {
            sessionId = dto.sessionId;
            AdoptState(dto.state);
            SaveState();
            EnterOnlineIntro();
        }

        /// 恢复会话失败:404 = 服务端数据丢了 → 新建会话继续(对话记忆重开,进度按服务端);
        /// 其他错误(网络)→ 本地进度进离线,可点重连。
        private void OnResumeFailed(string error)
        {
            if (error == "session_not_found")
            {
                Debug.LogWarning("[DialogueManager] 服务端会话丢失,新建会话");
                sessionId = null;
                StartCoroutine(CreateFreshAfterLost());
                return;
            }
            EnterOffline($"resume: {error}");
            OfflineIntro();
        }

        private IEnumerator CreateFreshAfterLost()
        {
            yield return ApiClient.CreateSession(null, OnSessionCreated, OnSessionFailed);
        }

        private void OnSessionFailed(string error)
        {
            EnterOffline($"session: {error}");
            OfflineIntro();
        }

        private void EnterOnlineIntro()
        {
            offline = false;
            if (offlineIndicator != null) offlineIndicator.SetActive(false);
            RefreshQuickReplies();
            SetInteractable(true);
            ShowReply(FallbackDialogue.NextReply(stage), "neutral"); // 开场白走兜底,保证进入就有戏
        }

        private void OfflineIntro()
        {
            // 离线开局也要能玩:兜底开场白 + 快捷回复 + 解锁输入(AGENTS.md 4.1 离线兜底)
            RefreshQuickReplies();
            SetInteractable(true);
            ShowReply(FallbackDialogue.NextReply(stage), "neutral");
        }

        private void AdoptState(SessionStateDto state)
        {
            if (state == null) return;
            if (!string.IsNullOrEmpty(state.stage)) stage = state.stage;
            trust = state.trust;
            if (state.flags != null)
            {
                flagNames.Clear();
                foreach (var flag in state.flags)
                {
                    if (flag != null && !string.IsNullOrEmpty(flag.name) && !flagNames.Contains(flag.name))
                    {
                        flagNames.Add(flag.name);
                    }
                }
            }
        }

        // ---------- 对话 ----------

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
            MergeFlags(dto.flagsChanged);
            SaveState(); // 每次对话后自动存(AGENTS.md 4.4)
            ShowReply(dto.reply, dto.emotion);
        }

        private void OnChatFailed(string error)
        {
            EnterOffline($"chat: {error}");
            ShowReply(FallbackDialogue.NextReply(stage), "neutral");
        }

        // ---------- bug 修复事件(POST /api/event 驱动阶段) ----------

        /// 由 PatchBug.Fix() 调用:上报真实修复;在线走后端结算,离线本地镜像。
        public void OnBugFixed(string bugId)
        {
            if (busy) { ApplyLocalFix(bugId); return; } // 对话进行中先本地推进,避免打断协程
            StartCoroutine(FixRoutine(bugId));
        }

        private IEnumerator FixRoutine(string bugId)
        {
            if (offline || string.IsNullOrEmpty(sessionId))
            {
                ApplyLocalFix(bugId);
                yield break;
            }
            yield return ApiClient.ReportBugFixed(sessionId, bugId, OnFixSuccess, _ => ApplyLocalFix(bugId));
        }

        private void OnFixSuccess(EventResponseDto dto)
        {
            if (!string.IsNullOrEmpty(dto.stage) && dto.stage != stage)
            {
                stage = dto.stage;
                RefreshQuickReplies();
            }
            trust = dto.trust;
            MergeFlags(dto.flagsChanged);
            SaveState();
            UpdateStageLabel();
            ShowReply(FallbackDialogue.NextReply(stage), "happy"); // 修复后帕奇的庆祝台词
        }

        /// 离线镜像:ch1_arrival/ch1_puzzle → ch1_done + 信任 +5(与后端 applyGameEvent 一致,
        /// 兜住「跳过对话直接修」的路径,离线也不能卡阶段)
        private void ApplyLocalFix(string bugId)
        {
            if (stage == "ch1_puzzle" || stage == "ch1_arrival")
            {
                stage = "ch1_done";
                RefreshQuickReplies();
            }
            trust = Mathf.Clamp(trust + 5, 0, 100);
            SaveState();
            UpdateStageLabel();
            ShowReply(FallbackDialogue.NextReply(stage), "happy");
        }

        // ---------- 手动重连(M2,设计备忘) ----------

        /// 离线指示器点击:有 sessionId 试恢复,没有/丢失则新建。
        public void OnReconnect()
        {
            if (!offline || busy) return;
            StartCoroutine(ReconnectRoutine());
        }

        private IEnumerator ReconnectRoutine()
        {
            busy = true;
            if (!string.IsNullOrEmpty(sessionId))
            {
                yield return ApiClient.CreateSession(sessionId, OnReconnectResumed, OnReconnectResumeFailed);
            }
            else
            {
                yield return ApiClient.CreateSession(null, OnReconnectCreated, OnReconnectFailed);
            }
            busy = false;
        }

        private void OnReconnectResumed(SessionResponseDto dto)
        {
            sessionId = dto.sessionId;
            AdoptState(dto.state);
            offline = false;
            if (offlineIndicator != null) offlineIndicator.SetActive(false);
            SaveState();
            RefreshQuickReplies();
            SetInteractable(true);
            UpdateStageLabel();
            ShowReply(FallbackDialogue.NextReply(stage), "happy");
        }

        private void OnReconnectCreated(SessionResponseDto dto)
        {
            OnReconnectResumed(dto);
        }

        private void OnReconnectResumeFailed(string error)
        {
            if (error == "session_not_found")
            {
                sessionId = null;
                StartCoroutine(RecreateAfterLost());
                return;
            }
            OnReconnectFailed(error);
        }

        private IEnumerator RecreateAfterLost()
        {
            yield return ApiClient.CreateSession(null, OnReconnectResumed, OnReconnectFailed);
            busy = false;
        }

        private void OnReconnectFailed(string error)
        {
            busy = false;
            ShowReply(FallbackDialogue.NextReply(stage), "sad"); // 还是连不上,继续离线
        }

        // ---------- UI 与存档 ----------

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

        private void MergeFlags(List<FlagChangeDto> flags)
        {
            if (flags == null) return;
            foreach (var flag in flags)
            {
                if (flag == null || string.IsNullOrEmpty(flag.name) || !flag.value) continue;
                if (!flagNames.Contains(flag.name)) flagNames.Add(flag.name);
            }
        }

        private void SaveState()
        {
            SaveManager.Save(new SaveManager.SaveData
            {
                sessionId = sessionId ?? string.Empty,
                stage = stage,
                trust = trust,
                flagNames = flagNames.ToArray(),
            });
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
