using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Patch47.GameFramework
{
    /// 对话后端客户端:POST /api/session(新建/恢复)、POST /api/chat、POST /api/event(游戏事件)。
    /// 任何失败都走 onError,由上层切兜底台词;404 会话丢失时 onError 收到 "session_not_found"。
    public static class ApiClient
    {
        public static IEnumerator CreateSession(string resumeSessionId, Action<SessionResponseDto> onSuccess, Action<string> onError)
        {
            var body = JsonUtility.ToJson(new SessionRequestDto { resumeSessionId = resumeSessionId });
            using (var request = new UnityWebRequest($"{GameConfig.ApiBaseUrl}/api/session", "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = GameConfig.RequestTimeoutSeconds;
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success)
                {
                    onError?.Invoke(request.responseCode == 404 ? "session_not_found" : $"HTTP {request.responseCode} {request.error}");
                    yield break;
                }
                SessionResponseDto dto = null;
                try { dto = JsonUtility.FromJson<SessionResponseDto>(request.downloadHandler.text); }
                catch (Exception e) { onError?.Invoke(e.Message); yield break; }
                if (dto == null || string.IsNullOrEmpty(dto.sessionId))
                {
                    onError?.Invoke("bad session response");
                    yield break;
                }
                onSuccess?.Invoke(dto);
            }
        }

        public static IEnumerator SendChat(string sessionId, string text, Action<ChatResponseDto> onSuccess, Action<string> onError)
        {
            var body = JsonUtility.ToJson(new ChatRequestDto { sessionId = sessionId, text = text });
            using (var request = new UnityWebRequest($"{GameConfig.ApiBaseUrl}/api/chat", "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = GameConfig.RequestTimeoutSeconds;
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success)
                {
                    onError?.Invoke($"HTTP {request.responseCode} {request.error}");
                    yield break;
                }
                ChatResponseDto dto = null;
                try { dto = JsonUtility.FromJson<ChatResponseDto>(request.downloadHandler.text); }
                catch (Exception e) { onError?.Invoke(e.Message); yield break; }
                if (dto == null || string.IsNullOrEmpty(dto.reply))
                {
                    onError?.Invoke("bad chat response");
                    yield break;
                }
                onSuccess?.Invoke(dto);
            }
        }

        /// 游戏事件上报(契约 2026-09-05):bug_fixed 等真实游戏事件驱动阶段推进。
        public static IEnumerator ReportBugFixed(string sessionId, string bugId, Action<EventResponseDto> onSuccess, Action<string> onError)
        {
            var body = JsonUtility.ToJson(new EventRequestDto { sessionId = sessionId, type = "bug_fixed", bugId = bugId });
            using (var request = new UnityWebRequest($"{GameConfig.ApiBaseUrl}/api/event", "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = GameConfig.RequestTimeoutSeconds;
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success)
                {
                    onError?.Invoke($"HTTP {request.responseCode} {request.error}");
                    yield break;
                }
                EventResponseDto dto = null;
                try { dto = JsonUtility.FromJson<EventResponseDto>(request.downloadHandler.text); }
                catch (Exception e) { onError?.Invoke(e.Message); yield break; }
                if (dto == null || string.IsNullOrEmpty(dto.stage))
                {
                    onError?.Invoke("bad event response");
                    yield break;
                }
                onSuccess?.Invoke(dto);
            }
        }
    }
}
