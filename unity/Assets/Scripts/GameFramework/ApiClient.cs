using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Patch47.GameFramework
{
    /// 对话后端客户端:POST /api/session、POST /api/chat。任何失败都走 onError,由上层切兜底台词。
    public static class ApiClient
    {
        public static IEnumerator CreateSession(Action<SessionResponseDto> onSuccess, Action<string> onError)
        {
            using (var request = new UnityWebRequest($"{GameConfig.ApiBaseUrl}/api/session", "POST"))
            {
                request.downloadHandler = new DownloadHandlerBuffer();
                request.timeout = GameConfig.RequestTimeoutSeconds;
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success)
                {
                    onError?.Invoke($"HTTP {request.responseCode} {request.error}");
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
    }
}
